using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Styling;
using cpap_app.Helpers;

using cpaplib;

using FluentAvalonia.UI.Controls;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

using Brush = Avalonia.Media.Brush;
using Brushes = Avalonia.Media.Brushes;
using Point = Avalonia.Point;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace cpap_app.Views;

public partial class SignalChart : UserControl
{
	#region Dependency Properties

	public static readonly StyledProperty<Brush> ChartBackgroundProperty    = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<Brush> ChartGridLineColorProperty = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<Brush> ChartForegroundProperty    = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<Brush> ChartBorderColorProperty   = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartBorderColor ) );

	#endregion 
	
	#region Public properties

	public SignalChartConfiguration? Configuration          { get; set; }
	public SignalChartConfiguration? SecondaryConfiguration { get; set; }

	public Brush ChartForeground
	{
		get => GetValue( ChartForegroundProperty );
		set => SetValue( ChartForegroundProperty, value );
	}

	public Brush ChartBackground
	{
		get => GetValue( ChartBackgroundProperty );
		set => SetValue( ChartBackgroundProperty, value );
	}

	public Brush ChartGridLineColor
	{
		get => GetValue( ChartGridLineColorProperty );
		set => SetValue( ChartGridLineColorProperty, value );
	}

	public Brush ChartBorderColor
	{
		get => GetValue( ChartBorderColorProperty );
		set => SetValue( ChartBorderColorProperty, value );
	}
	
	#endregion
	
	#region Private fields

	private CustomChartStyle _chartStyle;
	private MarkerPlot?      _currentValueMarker = null;
	private DailyReport?     _day                = null;
	private bool             _hasDataAvailable   = false;
	private bool             _chartInitialized   = false;
	
	private List<ReportedEvent> _events           = new();
	private List<Signal>        _signals          = new();
	private List<Signal>        _secondarySignals = new();
	
	#endregion 
	
	#region Constructor 
	
	public SignalChart()
	{
		InitializeComponent();

		PointerWheelChanged += OnPointerWheelChanged;

		Chart.ContextMenu = null;
	}

	#endregion 
	
	#region Event Handlers

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		if( Configuration is { IsPinned: true } )
		{
			btnChartPinUnpin.GetVisualDescendants().OfType<SymbolIcon>().First().Symbol = Symbol.Pin;
		}
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		if( Configuration != null )
		{
			ChartLabel.Text = Configuration.Title;
		}

		EventTooltip.IsVisible = false;
		MouseLine.IsVisible    = false;

		InitializeChartProperties( Chart );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );
	
		if( change.Property.Name == nameof( DataContext ) )
		{
			_signals.Clear();
			_secondarySignals.Clear();
			
			if( change.NewValue is DailyReport day )
			{
				if( Configuration == null || string.IsNullOrEmpty( Configuration.SignalName ) )
				{
					throw new NullReferenceException( "No Signal name was provided" );
				}

				if( !_chartInitialized )
				{
					InitializeChartProperties( Chart );
				}

				LoadData( day );
			}
			else if( change.NewValue == null )
			{
				IndicateNoDataAvailable();
			}
		}
	}

	private void OnPointerWheelChanged( object? sender, PointerWheelEventArgs args )
	{
		// Because the charts are likely going to be used within a scrolling container, I've disabled the built-in mouse wheel 
		// handling which performs zooming, and re-implemented it here with the additional requirement that the shift key be
		// held down while scrolling the mouse wheel in order to zoom. If the shift key is held down, the chart will zoom in
		// and out and the event will be marked Handled so that it doesn't cause scrolling in the parent container. 
		if( (args.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift )
		{
			(double x, double y) = Chart.GetMouseCoordinates();

			var amount = args.Delta.Y * 0.15 + 1.0;
			Chart.Plot.AxisZoom( amount, 1.0, x, y );

			args.Handled = true;

			Chart.RefreshRequest( RenderType.HighQuality );
		}
	}

	protected override void OnKeyDown( KeyEventArgs args )
	{
		if( args.Key is Key.Left or Key.Right )
		{
			bool isShiftDown = (args.KeyModifiers & KeyModifiers.Shift) != 0;
			var  direction   = (args.Key == Key.Left) ? -1.0 : 1.0;
			var  amount      = isShiftDown ? 50 : 25;

			var plot = Chart.Plot;

			// var axisLimits = plot.GetAxisLimits();
			// var bounds     = _day.TotalTimeSpan.TotalSeconds;
			// var axisRange  = axisLimits.XMax - axisLimits.XMin;
			var scale      = 0.5;

			if( scale < 0.99 )
			{
				plot.AxisPan( (direction * amount) / (1.0 - scale), 0 );
			}

			args.Handled = true;
			
			Chart.RefreshRequest( RenderType.HighQuality );
		}
		else if( args.Key is Key.Up or Key.Down )
		{
			double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.15 : 0.05;
			double amount    = (args.Key == Key.Up ? 1.0 : -1.0) * increment + 1.0;
			
			Chart.Plot.AxisZoom( amount, 1.0 );

			args.Handled = true;

			Chart.RefreshRequest( RenderType.HighQuality );
		}
	}

	#endregion 
	
	#region Public functions

	public Rectangle GetDataBounds()
	{
		var chartBounds = Chart.Bounds;
		var xDims       = Chart.Plot.XAxis.Dims;
		var yDims       = Chart.Plot.YAxis.Dims;
		
		var rect = new Rectangle(
			(int)(chartBounds.X + xDims.DataOffsetPx),
			(int)(chartBounds.Y + yDims.DataOffsetPx),
			(int)xDims.DataSizePx, 
			(int)yDims.DataSizePx
		);

		return rect;
	}
	
	#endregion 
	
	#region Private functions 
	
	internal void UpdateTrackedTime( double time, PointerEventArgs? eventArgs )
	{
		if( !_hasDataAvailable || _day == null )
		{
			return;
		}
		
		if( Configuration == null )
		{
			throw new Exception( $"The {nameof( Configuration )} property has not been assigned" );
		}

		var mousePosition = eventArgs?.GetPosition( this ) ?? new Point();
		var dataRect      = GetDataBounds();

		EventTooltip.IsVisible = false;
		_currentValueMarker!.X = time;
		CurrentValue.Text      = "";

		// Double.NaN will be sent when the mouse cursor leaves the control, and is an opportunity to hide
		// the mouse track line and other mouse-related visuals. 
		if( double.IsNaN( time ) || dataRect.Left > mousePosition.X || dataRect.Right < mousePosition.X )
		{
			MouseLine.IsVisible    = false;
			EventTooltip.IsVisible = false;
			
			return;
		}

		MouseLine.StartPoint = new Point( mousePosition.X, dataRect.Top );
		MouseLine.EndPoint   = new Point( mousePosition.X, dataRect.Bottom );
		MouseLine.IsVisible  = true;

		// Converting the "Number of seconds offset from the start of the chart" back to a DateTime makes it 
		// much easier to locate which Session this time refers to, and to then calculate an offset into that
		// session.
		var asDateTime = _day.RecordingStartTime.AddSeconds( time );

		foreach( var signal in _signals )
		{
			// Signal start times may be slightly different than session start times, so need to check 
			// the signal itself also 
			if( signal.StartTime <= asDateTime && signal.EndTime >= asDateTime )
			{
				var value = signal.GetValueAtTime( asDateTime );

				CurrentValue.Text     = $"{asDateTime:T}        {Configuration.Title}: {value:N2} {signal.UnitOfMeasurement}";
				_currentValueMarker.Y = value;

				break;
			}
		}

		if( SecondaryConfiguration != null )
		{
			foreach( var signal in _secondarySignals )
			{
				if( signal.StartTime <= asDateTime && signal.EndTime >= asDateTime )
				{
					var value = signal.GetValueAtTime( asDateTime );

					CurrentValue.Text     += $"        {SecondaryConfiguration.Title}: {value:N2} {signal.UnitOfMeasurement}";
					_currentValueMarker.Y = value;

					break;
				}
			}
		}

		double highlightDistance = 5.0 / Chart.Plot.XAxis.Dims.PxPerUnit;
		
		// Find any events the mouse might be hovering over
		foreach( var flag in _events )
		{
			var bounds    = flag.GetTimeBounds();
			var startTime = (bounds.StartTime - _day.RecordingStartTime).TotalSeconds;
			var endTime   = (bounds.EndTime - _day.RecordingStartTime).TotalSeconds;
			
			if( time >= startTime - highlightDistance && time <= endTime + highlightDistance )
			{
				if( flag.Duration.TotalSeconds > 0 )
					EventTooltip.Tag = $"{flag.Type.ToName()} ({FormattedTimespanConverter.FormatTimeSpan( flag.Duration, TimespanFormatType.Short, false )})";
				else
					EventTooltip.Tag = $"{flag.Type.ToName()}";
				
				EventTooltip.IsVisible = true;
				
				break;
			}
		}
		
	}

	private void LoadData( DailyReport day )
	{
		_day = day;
		_events.Clear();

		CurrentValue.Text = "";

		if( Configuration == null )
		{
			throw new Exception( $"No chart configuration has been provided" );
		}

		try
		{
			Chart.Configuration.AxesChangedEventEnabled = false;
			Chart.Plot.Clear();

			// Check to see if there are any sessions with the named Signal. If not, display the "No Data Available" message and eject.
			_hasDataAvailable = day.Sessions.FirstOrDefault( x => x.GetSignalByName( Configuration.SignalName ) != null ) != null;
			if( !_hasDataAvailable )
			{
				IndicateNoDataAvailable();

				return;
			}
			else
			{
				ChartLabel.Text        = Configuration.SignalName;
				NoDataLabel.IsVisible  = false;
				CurrentValue.IsVisible = true;
				Chart.IsEnabled        = true;
				this.IsEnabled         = true;
			}

			// If a RedLine position is specified, we want to add it before any signal data, as items are rendered in the 
			// order in which they are added, and we want the redline rendered behind the signal data.
			if( Configuration.BaselineHigh.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.6f );
				Chart.Plot.AddHorizontalLine( Configuration.BaselineHigh.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dash );
			}

			if( Configuration.BaselineLow.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.6f );
				Chart.Plot.AddHorizontalLine( Configuration.BaselineLow.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dash );
			}

			ChartSignal( Chart, day, Configuration.SignalName, 1f, Configuration.AxisMinValue, Configuration.AxisMaxValue );
			CreateEventMarkers( day );

			// TODO: The "Current Value" marker dot is currently not visible. 
			_currentValueMarker           = Chart.Plot.AddMarker( -1, -1, MarkerShape.filledCircle, 8, Colors.White.ToDrawingColor(), null );
			_currentValueMarker.IsVisible = false;
		}
		finally
		{
			Chart.Configuration.AxesChangedEventEnabled = true;
		}

		Chart.RefreshRequest( RenderType.HighQuality );
	}
	
	private void ChartSignal( AvaPlot chart, DailyReport day, string signalName, float signalScale = 1f, double? axisMinValue = null, double? axisMaxValue = null, double[]? manualLabels = null )
	{
		if( Configuration == null )
		{
			throw new Exception( $"The {nameof( Configuration )} property has not been assigned" );
		}
		
		var minValue = axisMinValue ?? double.MaxValue;
		var maxValue = axisMaxValue ?? double.MinValue;

		// Need to keep track of the first session added to the chart so that we can set that 
		// section's Label (for the chart legend). Otherwise, it will be duplicated for each 
		// session. 
		bool firstSessionAdded = true;

		foreach( var session in day.Sessions )
		{
			var signal = session.GetSignalByName( signalName );
			
			// Not every Session will contain the signal data for this chart. This is often the case when Sessions
			// have been added after CPAP data was imported, such as when importing pulse oximeter data or sleep
			// stage data, for example. 
			if( signal == null )
			{
				continue;
			}
			
			// Keep track of all of the signals that this graph displays. This is done partially so that we don't 
			// have to search for the signals during time-sensitive operations such as mouse movement, etc. 
			_signals.Add( signal );

			minValue = Math.Min( minValue, signal.MinValue * signalScale );
			maxValue = Math.Max( maxValue, signal.MaxValue * signalScale );

			var offset = (signal.StartTime - day.RecordingStartTime).TotalSeconds;

			var chartColor = Configuration.PlotColor.ToDrawingColor();

			var graph = chart.Plot.AddSignal(
				signal.Samples.ToArray(),
				signal.FrequencyInHz,
				chartColor,
				firstSessionAdded ? Configuration.Title : null
			);

			if( SecondaryConfiguration != null )
			{
				var secondarySignal = session.GetSignalByName( SecondaryConfiguration.SignalName );
				if( secondarySignal != null )
				{
					_secondarySignals.Add( secondarySignal );
					
					var secondaryGraph = chart.Plot.AddSignal( 
						secondarySignal.Samples.ToArray(), 
						secondarySignal.FrequencyInHz, 
						SecondaryConfiguration.PlotColor.ToDrawingColor(), 
						firstSessionAdded ? SecondaryConfiguration.Title : null );
					
					var secondaryOffset = (secondarySignal.StartTime - day.RecordingStartTime).TotalSeconds;
					
					secondaryGraph.OffsetX    = secondaryOffset;
					secondaryGraph.MarkerSize = 0;
				}
			}
			
			graph.OffsetX     = offset;
			graph.MarkerSize  = 0;
			graph.ScaleY      = signalScale;
			graph.UseParallel = true;
			// graph.StepDisplay = true;

			// "Fill Below" is only available on signals that do not cross a zero line and do not have a secondary 
			// signal. 
			if( signal is { MinValue: >= 0, MaxValue: > 0 } && SecondaryConfiguration == null && (Configuration.FillBelow ?? false) )
			{
				graph.FillBelow( chartColor, Colors.Transparent.ToDrawingColor(), 0.18 );
			}

			firstSessionAdded = false;
		}

		chart.Plot.XAxis.TickLabelFormat( x => $"{day.RecordingStartTime.AddSeconds( x ):hh:mm:ss tt}" );

		// Set zoom and boundary limits
		maxValue = axisMaxValue ?? maxValue;
		minValue = axisMinValue ?? minValue;
		var extents = maxValue - minValue;
		chart.Plot.YAxis.SetBoundary( minValue, maxValue + extents * 0.1 );
		chart.Plot.XAxis.SetBoundary( -1, day.TotalTimeSpan.TotalSeconds + 1 );
		chart.Plot.SetAxisLimits( -1, day.TotalTimeSpan.TotalSeconds + 1, minValue, maxValue + extents * 0.1 );

		// If manual vertical axis tick positions were provided, set up the labels for them and force the chart
		// to show those instead of the automatically-generated tick positions. 
		if( manualLabels != null && manualLabels.Length > 0 )
		{
			var labels = new string[ manualLabels.Length ];
			for( int i = 0; i < labels.Length; i++ )
			{
				labels[ i ] = manualLabels[ i ].ToString( "F0" );
			}
			
			chart.Plot.YAxis.ManualTickPositions( manualLabels, labels );
		}
		else
		{
			// int labelCount     = 5;
			// var range          = maxValue - minValue;
			// var labelPositions = new List<double>();
			// var labels         = new List<string>();
			//
			// if( minValue < 0 && maxValue > 0 )
			// {
			// 	labelPositions.Add( 0 );
			// 	labelCount -= 1;
			// }
			//
			// for( int i = 0; i < labelCount; i++ )
			// {
			// 	labelPositions.Add( minValue + i * range * (1.0 / labelCount) );
			// }
			//
			// labelPositions.Sort();
			//
			// for( int i = 0; i < labelPositions.Count; i++ )
			// {
			// 	labels.Add( $"{labelPositions[i]:F1}" );
			// }
			//
			// chart.Plot.YAxis.ManualTickPositions( labelPositions.ToArray(), labels.ToArray() );
		}
	}
	
	private void CreateEventMarkers( DailyReport day )
	{
		int[] typesSeen = new int[ 256 ];

		var flagTypes = Configuration!.DisplayedEvents;
		if( flagTypes.Count == 0 )
		{
			return;
		}
		
		foreach( var eventFlag in day.Events )
		{
			if( flagTypes.Contains( eventFlag.Type ) )
			{
				bool    seenBefore = typesSeen[ (int)eventFlag.Type ] != 0;
				var     color      = DataColors.GetMarkerColor( (int)eventFlag.Type ).ToDrawingColor();
				double  offset     = (eventFlag.StartTime - day.RecordingStartTime).TotalSeconds;
				string? label      = seenBefore ? null : eventFlag.Type.ToName();
				
				// TODO: Currently not showing marker types in graph legend. Should we?
				label = null;

				var markerLine = Chart.Plot.AddVerticalLine( offset, color, 1f, LineStyle.Solid, label );
				markerLine.IgnoreAxisAuto = true;
				
				if( eventFlag.Duration.TotalSeconds > 0 )
				{
					// Determine whether to consider the Duration as occurring before or after the marker flag
					var bounds    = eventFlag.GetTimeBounds();
					var startTime = (bounds.StartTime - day.RecordingStartTime).TotalSeconds;
					var endTime   = (bounds.EndTime - day.RecordingStartTime).TotalSeconds;
					
					// This seems backwards, but it appears that ResMed CPAP machines will set Duration to include
					// the period *before* the event, presumably because that period of time is a defining characteristic
					// of said event (such as "a decrease in airflow lasting at least 10 seconds", etc).
					Chart.Plot.AddHorizontalSpan( startTime, endTime, color.MultiplyAlpha( 0.2f ) );
				}

				_events.Add( eventFlag );

				typesSeen[ (int)eventFlag.Type ] = 1;
			}
		}
	}

	private void IndicateNoDataAvailable()
	{
		Chart.Plot.Clear();

		var signalName = Configuration != null ? Configuration.Title : "signal";
		
		NoDataLabel.Text       = $"There is no {signalName} data available";
		NoDataLabel.IsVisible  = true;
		CurrentValue.IsVisible = false;
		Chart.IsEnabled        = false;
		this.IsEnabled         = false;

		Chart.RefreshRequest( RenderType.HighQuality );
	}

	private void InitializeChartProperties( AvaPlot chart )
	{
		_chartInitialized = true;
		_chartStyle       = new CustomChartStyle( this, ChartForeground, ChartBackground, ChartBorderColor, ChartGridLineColor );
		
		var plot = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "88888.8", _chartStyle.TickLabelFontName, 12 );

		chart.Configuration.ScrollWheelZoom                              = false;
		chart.Configuration.Quality                                      = ScottPlot.Control.QualityMode.LowWhileDragging;
		chart.Configuration.QualityConfiguration.BenchmarkToggle         = RenderType.LowQualityThenHighQualityDelayed;
		chart.Configuration.QualityConfiguration.AutoAxis                = RenderType.LowQualityThenHighQualityDelayed;
		chart.Configuration.QualityConfiguration.MouseInteractiveDragged = RenderType.LowQualityThenHighQualityDelayed;
		chart.Configuration.QualityConfiguration.MouseInteractiveDropped = RenderType.LowQualityThenHighQualityDelayed;
		chart.Configuration.QualityConfiguration.MouseWheelScrolled      = RenderType.LowQualityThenHighQualityDelayed;

		plot.Style( _chartStyle );
		//plot.LeftAxis.Label( label );
		plot.Layout( 0, 0, 0, 0 );
		plot.Margins( 0.0, 0.1 );
		
		plot.XAxis.MinimumTickSpacing( 1f );
		plot.XAxis.SetZoomInLimit( 60 ); // Make smallest zoom window possible be 1 minute 
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.MajorGrid( false );
		plot.XAxis.AxisTicks.MajorTickLength = 15;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis2.Layout( 8, 1, 1 );

		plot.YAxis.TickDensity( 1f );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );

		var legend = plot.Legend();
		legend.Location     = Alignment.UpperRight;
		legend.Orientation  = ScottPlot.Orientation.Horizontal;
		legend.OutlineColor = _chartStyle.TickMajorColor;
		legend.FillColor    = _chartStyle.DataBackgroundColor;
		legend.FontColor    = _chartStyle.TitleFontColor;

		chart.Configuration.LockVerticalAxis = true;
		
		chart.RefreshRequest( RenderType.HighQuality );
	}
	
	private float MeasureText( string text, string fontFamily, float emSize )
	{
		FormattedText formatted = new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			new Typeface( fontFamily ),
			emSize,
			Brushes.Black
		);

		return (float)Math.Ceiling( formatted.Width );
	}

	#endregion 
}

