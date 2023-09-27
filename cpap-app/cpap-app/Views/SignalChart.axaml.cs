using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using cpap_app.Events;
using cpap_app.Styling;
using cpap_app.Helpers;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

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

	public string? Title               { get; set; }
	public string  SignalName          { get; set; } = "";
	public string  SecondarySignalName { get; set; } = "";
	public Color   PlotColor           { get; set; }
	public double? BaselineHigh        { get; set; }
	public double? BaselineLow         { get; set; }
	public bool?   FillBelow           { get; set; }
	public double? AxisMinValue        { get; set; } = null;
	public double? AxisMaxValue        { get; set; } = null;

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
	
	private CustomChartStyle? _chartStyle         = null;
	private Tooltip?          _tooltip            = null;
	private VLine?            _mouseTrackLine     = null;
	private MarkerPlot?       _currentValueMarker = null;
	private DailyReport?      _day                = null;
	private bool              _hasDataAvailable   = false;
	private bool              _chartInitialized   = false;
	
	private List<ReportedEvent> _events  = new();
	private List<Signal>        _signals = new();
	
	#endregion 
	
	#region Constructor 
	
	public SignalChart()
	{
		InitializeComponent();

		PointerWheelChanged += OnPointerWheelChanged;
		Chart.RightClicked  -= Chart.DefaultRightClickEvent;
	}

	#endregion 
	
	#region Event Handlers

	private void ChartOnPointerMoved( object? sender, PointerEventArgs e )
	{
		throw new NotImplementedException();
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		ChartLabel.Text = Title ?? SignalName;

		InitializeChartProperties( Chart );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );
	
		if( change.Property.Name == nameof( DataContext ) )
		{
			_signals.Clear();
			
			if( change.NewValue is DailyReport day )
			{
				if( string.IsNullOrEmpty( SignalName ) )
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
		// held down while scrolling the mouse wheel in order to zoom. If the shift key is held down, the chart will zooom in
		// and out and the event will be marked Handled so that it doesn't cause scrolling in the parent container. 
		if( (args.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift )
		{
			(double x, double y) = Chart.GetMouseCoordinates();

			var amount = args.Delta.Y * 0.15 + 1.0;
			Chart.Plot.AxisZoom( amount, 1.0, x, y );

			args.Handled = true;
			
			Chart.Refresh();
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
			
			Chart.RenderRequest( RenderType.LowQualityThenHighQuality );
		}
		else if( args.Key is Key.Up or Key.Down )
		{
			double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.15 : 0.05;
			double amount    = (args.Key == Key.Up ? 1.0 : -1.0) * increment + 1.0;
			
			Chart.Plot.AxisZoom( amount, 1.0 );

			args.Handled = true;

			Chart.RenderRequest( RenderType.LowQualityThenHighQuality );
		}
	}

	#endregion 
	
	#region Private functions 
	
	internal void UpdateSelectedTime( double time )
	{
		if( !_hasDataAvailable )
		{
			return;
		}
		
		_tooltip!.IsVisible    = false;
		_mouseTrackLine!.X     = time;
		_currentValueMarker!.X = time;
		CurrentValue.Text      = "";
		
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

				CurrentValue.Text     = $"{asDateTime:T}        {Title}: {value:N2} {signal.UnitOfMeasurement}";
				_currentValueMarker.Y = value;

				break;
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
					_tooltip.Label = $"{flag.Type.ToName()} ({flag.Duration:g})";
				else
					_tooltip.Label = $"{flag.Type.ToName()}";
				
				_tooltip.X         = endTime;
				_tooltip.Y         = Chart.Plot.GetAxisLimits().YMax * 0.75;
				_tooltip.IsVisible = true;
				
				break;
			}
		}
		
		Chart.RenderRequest( RenderType.LowQualityThenHighQualityDelayed );
	}

	private void LoadData( DailyReport day )
	{
		_day = day;
		_events.Clear();

		CurrentValue.Text = "";

		try
		{
			Chart.Configuration.AxesChangedEventEnabled = false;
			Chart.Plot.Clear();

			// Check to see if there are any sessions with the named Signal. If not, display the "No Data Available" message and eject.
			_hasDataAvailable = day.Sessions.FirstOrDefault( x => x.GetSignalByName( SignalName ) != null ) != null;
			if( !_hasDataAvailable )
			{
				IndicateNoDataAvailable();

				return;
			}
			else
			{
				ChartLabel.Text        = SignalName;
				NoDataLabel.IsVisible  = false;
				CurrentValue.IsVisible = true;
				Chart.IsEnabled        = true;
				this.IsEnabled         = true;
			}

			// If a RedLine position is specified, we want to add it before any signal data, as items are rendered in the 
			// order in which they are added, and we want the redline rendered behind the signal data.
			if( BaselineHigh.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.6f );
				Chart.Plot.AddHorizontalLine( BaselineHigh.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dash );
			}

			if( BaselineLow.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.6f );
				Chart.Plot.AddHorizontalLine( BaselineLow.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dash );
			}

			if( AxisMinValue != null )
			{
				Debug.Write( $"Axis Min Value: {AxisMinValue}" );
			}

			ChartSignal( Chart, day, SignalName, 1f, AxisMinValue, AxisMaxValue );
			// CreateEventMarkers( day );

			var lineColor = ((SolidColorBrush)ChartGridLineColor).Color.ToDrawingColor();

			_tooltip                                = Chart.Plot.AddTooltip( "", 0, 0 );
			_mouseTrackLine                         = Chart.Plot.AddVerticalLine( 0, lineColor, 1.25f, LineStyle.Dot );
			_mouseTrackLine.PositionLabel           = false;
			_mouseTrackLine.PositionLabelAxis       = Chart.Plot.XAxis;
			_mouseTrackLine.DragEnabled             = false;
			_mouseTrackLine.PositionLabel           = false;
			_mouseTrackLine.PositionLabelBackground = Color.FromArgb( 255, 32, 32, 32 ).ToDrawingColor();
			_mouseTrackLine.PositionFormatter       = x => DateTime.FromFileTime( (long)x ).ToString( "hh:mm:ss tt" );

			_tooltip.FillColor   = _chartStyle.DataBackgroundColor;
			_tooltip.BorderColor = _chartStyle.TickLabelColor;
			_tooltip.BorderWidth = 0;
			_tooltip.Font.Color  = _chartStyle.TickLabelColor;
			_tooltip.Font.Bold   = true;
			_tooltip.IsVisible   = false;

			// TODO: The "Current Value" marker dot is currently not visible. 
			_currentValueMarker           = Chart.Plot.AddMarker( -1, -1, MarkerShape.filledCircle, 8, Colors.White.ToDrawingColor(), null );
			_currentValueMarker.IsVisible = false;
		}
		finally
		{
			Chart.Configuration.AxesChangedEventEnabled = true;
		}

		Chart.RenderRequest( RenderType.LowQualityThenHighQuality );
	}
	
	private void ChartSignal( AvaPlot chart, DailyReport day, string signalName, float signalScale = 1f, double? axisMinValue = null, double? axisMaxValue = null, double[]? manualLabels = null )
	{
		var minValue = axisMinValue ?? double.MaxValue;
		var maxValue = axisMaxValue ?? double.MinValue;

		double offset  = 0;
		double endTime = 0;

		// Need to keep track of the first session added to the chart so that we can set that 
		// section's Label (for the chart legend). Otherwise, it will be duplicated for each 
		// session. 
		bool firstSessionAdded = true;

		// Keeping track of the chart's index in the group is an easy way to assign automatic
		// chart colors. 
		// var chartIndex = GetGroupList().IndexOf( this );

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

			offset  = (signal.StartTime - day.RecordingStartTime).TotalSeconds;
			endTime = (signal.EndTime - day.RecordingStartTime).TotalSeconds;

			var chartColor = PlotColor.ToDrawingColor();

			SignalPlotBase<double> graph = default;
			
			if( signal.Samples.Count > 100000 )
				graph = chart.Plot.AddSignalConst( signal.Samples.ToArray(), signal.FrequencyInHz, chartColor, firstSessionAdded ? signal.Name : null );
			else
				graph = chart.Plot.AddSignal( signal.Samples.ToArray(), signal.FrequencyInHz, chartColor, firstSessionAdded ? signal.Name : null );

			if( !string.IsNullOrEmpty( SecondarySignalName ) )
			{
				var secondarySignal = session.GetSignalByName( SecondarySignalName );
				if( secondarySignal != null )
				{
					var secondaryGraph = chart.Plot.AddSignal( secondarySignal.Samples.ToArray(), secondarySignal.FrequencyInHz, Colors.Yellow.ToDrawingColor(), null );
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

			// "Fill Below" is only available on signals that do not cross a zero line, do not have a secondary 
			// signal, and is explicitly set in the configuration table. 
			if( signal is { MinValue: >= 0, MaxValue: > 0 } && string.IsNullOrEmpty( SecondarySignalName ) && FillBelow.HasValue && FillBelow.Value )
			{
				graph.FillBelow( chartColor, Colors.Transparent.ToDrawingColor(), 0.18 );
			}

			firstSessionAdded = false;
		}

		chart.Plot.XAxis.TickLabelFormat( x => $"{day.RecordingStartTime.AddSeconds( x ):hh:mm:ss tt}" );

		// Set zoom and boundary limits
		chart.Plot.YAxis.SetBoundary( axisMinValue ?? minValue, axisMaxValue ?? maxValue );
		chart.Plot.XAxis.SetBoundary( -1, day.TotalTimeSpan.TotalSeconds + 1 );
		chart.Plot.SetAxisLimits( -1, day.TotalTimeSpan.TotalSeconds + 1, minValue, maxValue );

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
			var range           = maxValue - minValue;
			var automaticLabels = new double[] { minValue, minValue + range * 0.25, minValue + range * 0.5, minValue + range * 0.75, maxValue };
			var labels          = new string[ 5 ];
			
			for( int i = 0; i < labels.Length; i++ )
			{
				labels[ i ] = automaticLabels[ i ].ToString( "F1" );
			}

			// Ensure that there is always a center line on signals that have negative and positive values 
			if( minValue < 0 && maxValue > 0 )
			{
				chart.Plot.YAxis.AutomaticTickPositions( new[] { 0.0 }, new[] { "0" } );
			}
			
			chart.Plot.YAxis.ManualTickPositions( automaticLabels, labels );
		}
	}

	private void IndicateNoDataAvailable()
	{
		Chart.Plot.Clear();
		
		NoDataLabel.Text       = $"There is no {SignalName} data available";
		NoDataLabel.IsVisible  = true;
		CurrentValue.IsVisible = false;
		Chart.IsEnabled        = false;
		this.IsEnabled         = false;

		Chart.RenderRequest( RenderType.LowQualityThenHighQuality );
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
		chart.Configuration.QualityConfiguration.BenchmarkToggle         = RenderType.LowQualityThenHighQuality;
		chart.Configuration.QualityConfiguration.AutoAxis                = RenderType.LowQualityThenHighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDragged = RenderType.LowQualityThenHighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDropped = RenderType.LowQualityThenHighQuality;
		chart.Configuration.QualityConfiguration.MouseWheelScrolled      = RenderType.LowQualityThenHighQuality;

		plot.Style( _chartStyle );
		//plot.LeftAxis.Label( label );
		plot.Layout( 0, 0, 0, 0 );
		
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
		
		//chart.Refresh();
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

