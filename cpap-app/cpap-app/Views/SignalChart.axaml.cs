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
using Avalonia.VisualTree;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Events;
using cpap_app.Styling;
using cpap_app.Helpers;

using cpaplib;

using FluentAvalonia.UI.Controls;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

using Brush = Avalonia.Media.Brush;
using Brushes = Avalonia.Media.Brushes;
using Color = System.Drawing.Color;
using Point = Avalonia.Point;

namespace cpap_app.Views;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public partial class SignalChart : UserControl
{
	#region Dependency Properties

	public static readonly StyledProperty<Brush> ChartBackgroundProperty    = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<Brush> ChartGridLineColorProperty = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<Brush> ChartForegroundProperty    = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<Brush> ChartBorderColorProperty   = AvaloniaProperty.Register<SignalChart, Brush>( nameof( ChartBorderColor ) );

	#endregion
	
	#region Events 
	
	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> DisplayedRangeChangedEvent = RoutedEvent.Register<SignalChart, DateTimeRangeRoutedEventArgs>( nameof( DisplayedRangeChanged ), RoutingStrategies.Bubble );

	public static void AddDisplayedRangeChangedHandler( IInputElement element, EventHandler<DateTimeRangeRoutedEventArgs> handler )
	{
		element.AddHandler( DisplayedRangeChangedEvent, handler );
	}

	public event EventHandler<DateTimeRangeRoutedEventArgs> DisplayedRangeChanged
	{
		add => AddHandler( DisplayedRangeChangedEvent, value );
		remove => RemoveHandler( DisplayedRangeChangedEvent, value );
	}

	public static readonly RoutedEvent<DateTimeRoutedEventArgs> TimeMarkerChangedEvent = RoutedEvent.Register<SignalChart, DateTimeRoutedEventArgs>( nameof( TimeMarkerChanged ), RoutingStrategies.Bubble );

	public static void AddTimeMarkerChangedHandler( IInputElement element, EventHandler<DateTimeRoutedEventArgs> handler )
	{
		element.AddHandler( TimeMarkerChangedEvent, handler );
	}

	public event EventHandler<DateTimeRoutedEventArgs> TimeMarkerChanged
	{
		add => AddHandler( TimeMarkerChangedEvent, value );
		remove => RemoveHandler( TimeMarkerChangedEvent, value );
	}

	#endregion 
	
	#region Public properties

	public SignalChartConfiguration?      ChartConfiguration     { get; set; }
	public SignalChartConfiguration?      SecondaryConfiguration { get; set; }
	public List<EventMarkerConfiguration> MarkerConfiguration    { get; set; }

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

	private CustomChartStyle     _chartStyle;
	private DailyReport?         _day                = null;
	private bool                 _hasDataAvailable   = false;
	private bool                 _chartInitialized   = false;
	private double               _selectionStartTime = 0;
	private double               _selectionEndTime   = 0;
	private GraphInteractionMode _interactionMode    = GraphInteractionMode.None;
	private AxisLimits           _pointerDownAxisLimits;
	private Point                _pointerDownPosition;

	private HSpan          _selectionSpan;
	private VLine          _hoverMarkerLine;
	private HSpan          _hoverMarkerSpan;
	private ReportedEvent? _hoverEvent = null;
	
	private List<ReportedEvent> _events           = new();
	private List<Signal>        _signals          = new();
	private List<Signal>        _secondarySignals = new();
	
	#endregion 
	
	#region Constructor 
	
	public SignalChart()
	{
		InitializeComponent();

		PointerWheelChanged   += OnPointerWheelChanged;
		PointerEntered        += OnPointerEntered;
		PointerExited         += OnPointerExited;
		Chart.PointerPressed  += OnPointerPressed;
		Chart.PointerReleased += OnPointerReleased;
		Chart.PointerMoved    += OnPointerMoved;
		Chart.AxesChanged     += OnAxesChanged;

		Chart.ContextMenu = null;
	}

	#endregion 
	
	#region Base class overrides 
	
	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		if( ChartConfiguration is { IsPinned: true } )
		{
			btnChartPinUnpin.GetVisualDescendants().OfType<SymbolIcon>().First().Symbol = Symbol.Pin;
		}
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		if( ChartConfiguration != null )
		{
			ChartLabel.Text = ChartConfiguration.Title;
		}

		EventTooltip.IsVisible   = false;
		TimeMarkerLine.IsVisible = false;

		InitializeChartProperties( Chart );

		if( _day != null )
		{
			LoadData( _day );
		}
	}

	protected override void OnKeyDown( KeyEventArgs args )
	{
		if( args.Key is Key.Left or Key.Right )
		{
			var  axisLimits  = Chart.Plot.GetAxisLimits();
			var  startTime   = axisLimits.XMin;
			var  endTime     = axisLimits.XMax;
			bool isShiftDown = (args.KeyModifiers & KeyModifiers.Shift) != 0;
			var  amount      = axisLimits.XSpan * (isShiftDown ? 0.25 : 0.10);

			if( args.Key == Key.Left )
			{
				startTime -= amount;
				endTime   =  startTime + axisLimits.XSpan;
			}
			else
			{
				endTime   += amount;
				startTime =  endTime - axisLimits.XSpan;
			}
			
			Chart.Plot.SetAxisLimits( startTime, endTime );
			
			HideTimeMarker();
			Chart.RenderRequest();
			
			args.Handled = true;
		}
		else if( args.Key is Key.Up or Key.Down )
		{
			double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.25 : 0.1;
			double amount    = (args.Key == Key.Up ? 1.0 : -1.0) * increment + 1.0;
			
			Chart.Plot.AxisZoom( amount, 1.0 );

			args.Handled = true;

			HideTimeMarker();
			Chart.RenderRequest();
		}
		else if( args.Key == Key.Escape )
		{
			if( _interactionMode == GraphInteractionMode.Selecting )
			{
				_interactionMode         = GraphInteractionMode.None;
				_selectionStartTime      = 0;
				_selectionEndTime        = 0;
				_selectionSpan.IsVisible = false;
				
				Chart.RenderRequest();
			}
		}
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
				if( ChartConfiguration == null || string.IsNullOrEmpty( ChartConfiguration.SignalName ) )
				{
					throw new NullReferenceException( "No Signal name was provided" );
				}

				if( !_chartInitialized )
				{
					_day = day;
				}
				else
				{
					LoadData( day );
				}
			}
			else if( change.NewValue == null )
			{
				IndicateNoDataAvailable();
			}
		}
	}

	#endregion 
	
	#region Event Handlers

	private void OnAxesChanged( object? sender, EventArgs e )
	{
		if( _day == null || !_hasDataAvailable || !IsEnabled )
		{
			return;
		}
		
		var currentAxisLimits = Chart.Plot.GetAxisLimits();

		var eventArgs = new DateTimeRangeRoutedEventArgs
		{
			RoutedEvent = DisplayedRangeChangedEvent,
			Source      = this,
			StartTime   = _day.RecordingStartTime.AddSeconds( currentAxisLimits.XMin ),
			EndTime     = _day.RecordingStartTime.AddSeconds( currentAxisLimits.XMax )
		};

		RaiseEvent( eventArgs );
	}

	private void OnPointerEntered( object? sender, PointerEventArgs e )
	{
		this.Focus();
		
		if( _day == null || !IsEnabled )
		{
			return;
		}

		if( _interactionMode == GraphInteractionMode.None )
		{
			var currentPoint = e.GetPosition( Chart );
			var timeOffset   = Chart.Plot.XAxis.Dims.GetUnit( (float)currentPoint.X );
			var time         = _day.RecordingStartTime.AddSeconds( timeOffset );

			UpdateTimeMarker( time );
			RaiseTimeMarkerChanged( time );
		}
	}

	private void OnPointerExited( object? sender, PointerEventArgs e )
	{
		if( object.ReferenceEquals( sender, this ) )
		{
			HideTimeMarker();
		}
	}

	private void OnPointerReleased( object? sender, PointerReleasedEventArgs e )
	{
		_selectionSpan.IsVisible = false;
		EventTooltip.IsVisible   = false;
		
		switch( _interactionMode )
		{
			case GraphInteractionMode.Selecting:
				EndSelectionMode();
				break;
			case GraphInteractionMode.Panning:
				// The chart was rendered in low quality while panning, so re-render in high quality now that we're done 
				Chart.Configuration.Quality = ScottPlot.Control.QualityMode.LowWhileDragging;
				Chart.RenderRequest();
				break;
		}

		_interactionMode = GraphInteractionMode.None;
	}

	private void OnPointerPressed( object? sender, PointerPressedEventArgs eventArgs )
	{
		if( eventArgs.Handled || _interactionMode != GraphInteractionMode.None )
		{
			return;
		}

		_selectionStartTime = 0;
		_selectionEndTime   = 0;
		
		HideTimeMarker();

		var point = eventArgs.GetCurrentPoint( this );
		if( point.Properties.IsMiddleButtonPressed )
		{
			return;
		}

		// We will want to do different things depending on where the PointerPressed happens, such 
		// as within the data area of the graph versus on the chart title, etc. 
		var dataRect = GetDataBounds();
		if( !dataRect.Contains( point.Position ) )
		{
			return;
		}
		
		if( eventArgs.KeyModifiers == KeyModifiers.None && !point.Properties.IsRightButtonPressed )
		{
			(_selectionStartTime, _) = Chart.GetMouseCoordinates();
			_selectionEndTime        = _selectionStartTime;
			_selectionSpan.X1        = _selectionStartTime;
			_selectionSpan.X2        = _selectionStartTime;
			_selectionSpan.IsVisible = true;

			_interactionMode = GraphInteractionMode.Selecting;
			
			eventArgs.Handled = true;
		}
		else if( (eventArgs.KeyModifiers & KeyModifiers.Control) != 0 || point.Properties.IsRightButtonPressed )
		{
			Chart.Configuration.Quality = ScottPlot.Control.QualityMode.Low;

			_pointerDownPosition   = point.Position;
			_pointerDownAxisLimits = Chart.Plot.GetAxisLimits();
			_interactionMode       = GraphInteractionMode.Panning;
		}
	}

	private void OnPointerWheelChanged( object? sender, PointerWheelEventArgs args )
	{
		// Because the charts are likely going to be used within a scrolling container, I've disabled the built-in mouse wheel 
		// handling which performs zooming, and re-implemented it here with the additional requirement that the Control key be
		// held down while scrolling the mouse wheel in order to zoom. If the Control key is held down, the chart will zoom in
		// and out and the event will be marked Handled so that it doesn't cause scrolling in the parent container. 
		if( (args.KeyModifiers & KeyModifiers.Control) != 0x00 )
		{
			(double x, double y) = Chart.GetMouseCoordinates();

			var amount = args.Delta.Y * 0.15 + 1.0;
			Chart.Plot.AxisZoom( amount, 1.0, x, y );

			args.Handled = true;

			HideTimeMarker();
			Chart.RenderRequest();
		}
	}

	private void OnPointerMoved( object? sender, PointerEventArgs eventArgs )
	{
		if( _day == null || !IsEnabled )
		{
			return;
		}

		(double timeOffset, _) = Chart.GetMouseCoordinates();

		switch( _interactionMode )
		{
			case GraphInteractionMode.Selecting:
			{
				// TODO: This still allows selecting areas of the Signal that are not in the graph's visible area. Leave it?
				_selectionEndTime = Math.Max( 0, Math.Min( timeOffset, _day.TotalTimeSpan.TotalSeconds ) );

				if( timeOffset < _selectionStartTime )
				{
					_selectionSpan.X1 = _selectionEndTime;
					_selectionSpan.X2 = _selectionStartTime;
				}
				else
				{
					_selectionSpan.X1 = _selectionStartTime;
					_selectionSpan.X2 = _selectionEndTime;
				}

				var timeRangeSelected = TimeSpan.FromSeconds( _selectionSpan.X2 - _selectionSpan.X1 );

				if( timeRangeSelected.Minutes > 10 )
				{
					Debug.WriteLine( "wtf"  );
				}
			
				EventTooltip.Tag       = FormattedTimespanConverter.FormatTimeSpan( timeRangeSelected, TimespanFormatType.Long, true );
				EventTooltip.IsVisible = timeRangeSelected.TotalSeconds > double.Epsilon;
				
				eventArgs.Handled = true;
			
				Chart.RenderRequest();
			
				return;
			}
			case GraphInteractionMode.Panning:
			{
				var position  = eventArgs.GetCurrentPoint( this ).Position;
				var panAmount = (_pointerDownPosition.X - position.X) / Chart.Plot.XAxis.Dims.PxPerUnit;
			
				double start = 0;
				double end   = 0;
			
				if( position.X < _pointerDownPosition.X )
				{
					start = Math.Max( 0, _pointerDownAxisLimits.XMin + panAmount );
					end   = start + _pointerDownAxisLimits.XSpan;
				}
				else
				{
					end   = Math.Min( _day.TotalTimeSpan.TotalSeconds, _pointerDownAxisLimits.XMax + panAmount );
					start = end - _pointerDownAxisLimits.XSpan;
				}
			
				Chart.Plot.SetAxisLimits( start, end );
				Chart.RenderRequest( RenderType.LowQualityThenHighQualityDelayed );

				eventArgs.Handled = true;

				return;
			}
			case GraphInteractionMode.None:
			{
				var time = _day.RecordingStartTime.AddSeconds( timeOffset );

				UpdateTimeMarker( time );
				RaiseTimeMarkerChanged( time );
				break;
			}
		}
	}

	#endregion 
	
	#region Public functions

	public void SetDisplayedRange( DateTime startTime, DateTime endTime )
	{
		if( _day == null )
		{
			return;
		}
		
		var offsetStart = (startTime - _day.RecordingStartTime).TotalSeconds;
		var offsetEnd   = (endTime - _day.RecordingStartTime).TotalSeconds;

		ZoomTo( offsetStart, offsetEnd );
	}

	public Rect GetDataBounds()
	{
		var chartBounds = Chart.Bounds;
		var xDims       = Chart.Plot.XAxis.Dims;
		var yDims       = Chart.Plot.YAxis.Dims;
		
		var rect = new Rect(
			(int)(chartBounds.X + xDims.DataOffsetPx),
			(int)(chartBounds.Y + yDims.DataOffsetPx),
			(int)xDims.DataSizePx, 
			(int)yDims.DataSizePx
		);

		return rect;
	}
	
	#endregion 
	
	#region Private functions

	private void EndSelectionMode()
	{
		_interactionMode = GraphInteractionMode.None;
			
		if( _selectionStartTime > _selectionEndTime )
		{
			(_selectionStartTime, _selectionEndTime) = (_selectionEndTime, _selectionStartTime);
		}
		
		if( _day != null && _selectionEndTime > _selectionStartTime + 30 )
		{
			ZoomTo( _selectionStartTime, _selectionEndTime );
			OnAxesChanged( this, EventArgs.Empty );
		}

		Chart.RenderRequest();
	}

	private void HideTimeMarker()
	{
		if( TimeMarkerLine.IsVisible )
		{
			UpdateTimeMarker( DateTime.MinValue );
			RaiseTimeMarkerChanged( DateTime.MinValue );

			HideEventHoverMarkers();
		}
	}

	private void ShowEventHoverMarkers( ReportedEvent hoverEvent )
	{
		if( _day == null || object.ReferenceEquals( _hoverEvent, hoverEvent ) )
		{
			return;
		}
		
		_hoverEvent = hoverEvent;

		var config = MarkerConfiguration.FirstOrDefault( x => x.EventType == hoverEvent.Type );
		if( config != null )
		{
			if( config.EventMarkerType == EventMarkerType.Span )
			{
				return;
			}

			var    bounds       = hoverEvent.GetTimeBounds();
			double startOffset  = (bounds.StartTime - _day.RecordingStartTime).TotalSeconds;
			double endOffset    = (bounds.EndTime - _day.RecordingStartTime).TotalSeconds;
			double centerOffset = (startOffset + endOffset) / 2.0;

			if( config.EventMarkerType != EventMarkerType.Flag && config.MarkerPosition != EventMarkerPosition.InCenter )
			{
				_hoverMarkerLine.X = config.MarkerPosition switch
				{
					EventMarkerPosition.AtEnd       => endOffset,
					EventMarkerPosition.AtBeginning => startOffset,
					EventMarkerPosition.InCenter    => centerOffset,
					_                               => -1
				};
				
				_hoverMarkerLine.Color     = config.Color;
				_hoverMarkerLine.IsVisible = true;
			}

			if( hoverEvent.Duration.TotalSeconds > 0 )
			{
				_hoverMarkerSpan.X1        = startOffset;
				_hoverMarkerSpan.X2        = endOffset;
				_hoverMarkerSpan.Color     = config.Color.MultiplyAlpha( 0.2f );
				_hoverMarkerSpan.IsVisible = true;
			}
		}
		
		Chart.RenderRequest();
	}
	
	private void HideEventHoverMarkers()
	{
		if( _hoverEvent != null )
		{
			_hoverMarkerSpan.IsVisible = false;
			_hoverMarkerLine.IsVisible = false;
			_hoverEvent                = null;
			
			Chart.RenderRequest();
		}
	}

	private void RaiseTimeMarkerChanged( DateTime time )
	{
		RaiseEvent( new DateTimeRoutedEventArgs()
		{
			RoutedEvent = TimeMarkerChangedEvent,
			Source      = this,
			DateTime        = time
		} );
	}

	private void ZoomTo( double startTime, double endTime )
	{
		if( !IsEnabled || !Chart.IsEnabled )
		{
			return;
		}
		
		// Don't allow zooming in closer than one minute
		if( endTime - startTime < 60 )
		{
			Debug.Assert( _day != null, nameof( _day ) + " != null" );
			
			var center = (endTime + startTime) * 0.5;
			startTime = Math.Max( 0, center - 30 );
			endTime   = Math.Min( _day.TotalTimeSpan.TotalSeconds, center + 30 );
		}

		// disable events briefly to avoid an infinite loop
		Chart.Configuration.AxesChangedEventEnabled = false;
		{
			var currentAxisLimits  = Chart.Plot.GetAxisLimits();
			var modifiedAxisLimits = new AxisLimits( startTime, endTime, currentAxisLimits.YMin, currentAxisLimits.YMax );

			Chart.Plot.SetAxisLimits( modifiedAxisLimits );
			Chart.RenderRequest( RenderType.LowQualityThenHighQualityDelayed );
		}
		Chart.Configuration.AxesChangedEventEnabled = true;
	}
	
	internal void UpdateTimeMarker( DateTime time )
	{
		if( !_hasDataAvailable || _day == null )
		{
			return;
		}
		
		if( ChartConfiguration == null )
		{
			throw new Exception( $"The {nameof( ChartConfiguration )} property has not been assigned" );
		}

		var timeOffset    = (time - _day.RecordingStartTime).TotalSeconds; 
		var dataRect      = GetDataBounds();
		var dims          = Chart.Plot.XAxis.Dims;
		var mousePosition = dims.PxPerUnit * (timeOffset - dims.Min) + dataRect.Left;

		TimeMarkerLine.IsVisible = false;
		EventTooltip.IsVisible   = false;
		CurrentValue.Text        = "";
		
		// If the time isn't valid then hide the marker and exit
		if( time < _day.RecordingStartTime || time > _day.RecordingEndTime )
		{
			return;
		}

		// If the time isn't visible within the displayed range, hide the marker and exit.
		if( dataRect.Left > mousePosition || dataRect.Right < mousePosition )
		{
			return;
		}

		TimeMarkerLine.StartPoint = new Point( mousePosition, dataRect.Top );
		TimeMarkerLine.EndPoint   = new Point( mousePosition, dataRect.Bottom );
		TimeMarkerLine.IsVisible  = true;

		foreach( var signal in _signals )
		{
			// Signal start times may be slightly different than session start times, so need to check 
			// the signal itself also 
			if( signal.StartTime <= time && signal.EndTime >= time )
			{
				var value = signal.GetValueAtTime( time, !ChartConfiguration.ShowStepped );

				CurrentValue.Text = $"{time:T}        {ChartConfiguration.Title}: {value:N2} {signal.UnitOfMeasurement}";

				break;
			}
		}

		if( SecondaryConfiguration != null )
		{
			foreach( var signal in _secondarySignals )
			{
				if( signal.StartTime <= time && signal.EndTime >= time )
				{
					var value = signal.GetValueAtTime( time );

					CurrentValue.Text += $"        {SecondaryConfiguration.Title}: {value:N2} {signal.UnitOfMeasurement}";

					break;
				}
			}
		}

		double highlightDistance = 8.0 / Chart.Plot.XAxis.Dims.PxPerUnit;
		bool   hoveringOverEvent = false;
		
		// Find any events the mouse might be hovering over
		foreach( var flag in _events )
		{
			var bounds    = flag.GetTimeBounds();
			var startTime = (bounds.StartTime - _day.RecordingStartTime).TotalSeconds;
			var endTime   = (bounds.EndTime - _day.RecordingStartTime).TotalSeconds;
			
			if( timeOffset >= startTime - highlightDistance && timeOffset <= endTime + highlightDistance )
			{
				EventTooltip.Tag = $"{flag.Type.ToName()}";
				if( flag.Duration.TotalSeconds > 0 )
				{
					EventTooltip.Tag += $" ({FormattedTimespanConverter.FormatTimeSpan( flag.Duration, TimespanFormatType.Short, false )})";
				}
				
				EventTooltip.IsVisible = true;
				
				ShowEventHoverMarkers( flag );
				hoveringOverEvent = true;
				
				break;
			}
		}

		if( !hoveringOverEvent )
		{
			HideEventHoverMarkers();
		}
	}

	private void LoadData( DailyReport day )
	{
		_day = day;
		_events.Clear();

		CurrentValue.Text = "";

		if( ChartConfiguration == null )
		{
			throw new Exception( $"No chart configuration has been provided" );
		}

		try
		{
			Chart.Configuration.AxesChangedEventEnabled = false;
			Chart.Plot.Clear();

			// Check to see if there are any sessions with the named Signal. If not, display the "No Data Available" message and eject.
			_hasDataAvailable = day.Sessions.FirstOrDefault( x => x.GetSignalByName( ChartConfiguration.SignalName ) != null ) != null;
			if( !_hasDataAvailable )
			{
				IndicateNoDataAvailable();

				return;
			}
			else
			{
				ChartLabel.Text        = ChartConfiguration.SignalName;
				NoDataLabel.IsVisible  = false;
				CurrentValue.IsVisible = true;
				Chart.IsEnabled        = true;
				this.IsEnabled         = true;
			}

			// If a RedLine position is specified, we want to add it before any signal data, as items are rendered in the 
			// order in which they are added, and we want the redline rendered behind the signal data.
			if( ChartConfiguration.BaselineHigh.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.75f );
				Chart.Plot.AddHorizontalLine( ChartConfiguration.BaselineHigh.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dot );
			}

			if( ChartConfiguration.BaselineLow.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( 0.75f );
				Chart.Plot.AddHorizontalLine( ChartConfiguration.BaselineLow.Value, redlineColor.ToDrawingColor(), 1f, LineStyle.Dot );
			}

			ChartSignal( Chart, day, ChartConfiguration.SignalName, 1f, ChartConfiguration.AxisMinValue, ChartConfiguration.AxisMaxValue );
			CreateEventMarkers( day );

			_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
			_selectionSpan.IgnoreAxisAuto = true;
			_selectionSpan.IsVisible      = false;

			_hoverMarkerLine                = Chart.Plot.AddVerticalLine( -1, Color.Transparent, 1.5f, LineStyle.Solid, null );
			_hoverMarkerLine.IgnoreAxisAuto = true;
			_hoverMarkerLine.IsVisible      = false;

			_hoverMarkerSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Transparent, null );
			_hoverMarkerSpan.IgnoreAxisAuto = true;
			_hoverMarkerSpan.IsVisible      = false;
		}
		finally
		{
			Chart.RenderRequest();
			Chart.Configuration.AxesChangedEventEnabled = true;
		}
	}
	
	private void ChartSignal( AvaPlot chart, DailyReport day, string signalName, float signalScale = 1f, double? axisMinValue = null, double? axisMaxValue = null, double[]? manualLabels = null )
	{
		if( ChartConfiguration == null )
		{
			throw new Exception( $"The {nameof( ChartConfiguration )} property has not been assigned" );
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

			var chartColor = ChartConfiguration.PlotColor;

			var graph = chart.Plot.AddSignal(
				signal.Samples.ToArray(),
				signal.FrequencyInHz,
				chartColor,
				firstSessionAdded ? ChartConfiguration.Title : null
			);

			if( ChartConfiguration.ShowStepped )
			{
				graph.StepDisplay      = true;
				graph.StepDisplayRight = true;
			}

			if( SecondaryConfiguration != null )
			{
				var secondarySignal = session.GetSignalByName( SecondaryConfiguration.SignalName );
				if( secondarySignal != null )
				{
					_secondarySignals.Add( secondarySignal );
					
					var secondaryGraph = chart.Plot.AddSignal( 
						secondarySignal.Samples.ToArray(), 
						secondarySignal.FrequencyInHz, 
						SecondaryConfiguration.PlotColor, 
						firstSessionAdded ? SecondaryConfiguration.Title : null );
					
					var secondaryOffset = (secondarySignal.StartTime - day.RecordingStartTime).TotalSeconds;
					
					secondaryGraph.OffsetX    = secondaryOffset;
					secondaryGraph.MarkerSize = 0;
					
					if( ChartConfiguration.ShowStepped )
					{
						secondaryGraph.StepDisplay      = true;
						secondaryGraph.StepDisplayRight = true;
					}
				}
			}
			
			graph.OffsetX     = offset;
			graph.MarkerSize  = 0;
			graph.ScaleY      = signalScale;
			graph.UseParallel = true;
			// graph.StepDisplay = true;

			// "Fill Below" is only available on signals that do not cross a zero line and do not have a secondary 
			// signal. 
			if( signal is { MinValue: >= 0, MaxValue: > 0 } && SecondaryConfiguration == null && (ChartConfiguration.FillBelow ?? false) )
			{
				graph.FillBelow( chartColor, Colors.Transparent.ToDrawingColor(), 0.18 );
			}

			firstSessionAdded = false;
		}

		// Set zoom and boundary limits
		maxValue = axisMaxValue ?? maxValue;
		minValue = axisMinValue ?? minValue;
		var extents = maxValue - minValue;
		chart.Plot.YAxis.SetBoundary( minValue, maxValue + extents * 0.1 );
		chart.Plot.XAxis.SetBoundary( -1, day.TotalTimeSpan.TotalSeconds + 1 );
		
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		chart.Plot.XAxis.TickLabelFormat( x => $"{_day.RecordingStartTime.AddSeconds( x ):hh:mm:ss tt}" );

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
	}
	
	private void CreateEventMarkers( DailyReport day )
	{
		int[] typesSeen = new int[ 256 ];

		var flagTypes = ChartConfiguration!.DisplayedEvents;
		if( flagTypes.Count == 0 )
		{
			return;
		}
		
		foreach( var eventFlag in day.Events )
		{
			if( flagTypes.Contains( eventFlag.Type ) )
			{
				var markerConfig = MarkerConfiguration.FirstOrDefault( x => x.EventType == eventFlag.Type );
				if( markerConfig == null || markerConfig.EventMarkerType == EventMarkerType.None )
				{
					continue;
				}
					
				var color  = markerConfig.Color;
				var limits = Chart.Plot.GetAxisLimits();
				var bounds = eventFlag.GetTimeBounds();

				double startOffset  = (bounds.StartTime - day.RecordingStartTime).TotalSeconds;
				double endOffset    = (bounds.EndTime - day.RecordingStartTime).TotalSeconds;
				double centerOffset = (startOffset + endOffset) / 2.0;

				double markerOffset = markerConfig.MarkerPosition switch
				{
					EventMarkerPosition.AtEnd       => endOffset,
					EventMarkerPosition.AtBeginning => startOffset,
					EventMarkerPosition.InCenter    => centerOffset,
					_                               => throw new ArgumentOutOfRangeException( $"Unhandled {nameof( EventMarkerPosition )} value {markerConfig.MarkerPosition}" )
				};

				switch( markerConfig.EventMarkerType )
				{
					case EventMarkerType.Flag:
						Chart.Plot.AddVerticalLine( markerOffset, color, 1.5f, LineStyle.Solid, null );
						break;
					case EventMarkerType.TickTop:
						var topLine = Chart.Plot.AddMarker( markerOffset, limits.YMax, MarkerShape.verticalBar, 32, markerConfig.Color, null );
						topLine.MarkerLineWidth = 1.5f;
						break;
					case EventMarkerType.TickBottom:
						var bottomLine = Chart.Plot.AddMarker( markerOffset, limits.YMin, MarkerShape.verticalBar, 32, markerConfig.Color, null );
						bottomLine.MarkerLineWidth = 1.5f;
						break;
					case EventMarkerType.ArrowTop:
						Chart.Plot.AddMarker( markerOffset, limits.YMax, MarkerShape.filledTriangleDown, 16, markerConfig.Color, null );
						break;
					case EventMarkerType.ArrowBottom:
						Chart.Plot.AddMarker( markerOffset, limits.YMin, MarkerShape.filledTriangleUp, 16, markerConfig.Color, null );
						break;
					case EventMarkerType.Span:
						Chart.Plot.AddHorizontalSpan( startOffset, endOffset, color.MultiplyAlpha( 0.35f ) );
						break;
					case EventMarkerType.None:
						continue;
					default:
						throw new ArgumentOutOfRangeException( $"Unhandled {nameof( EventMarkerType )} value {markerConfig.EventMarkerType}" );
				}

				_events.Add( eventFlag );

				typesSeen[ (int)eventFlag.Type ] = 1;
			}
		}
	}

	private void IndicateNoDataAvailable()
	{
		Chart.Plot.Clear();

		var signalName = ChartConfiguration != null ? ChartConfiguration.Title : "signal";
		
		NoDataLabel.Text       = $"There is no {signalName} data available";
		NoDataLabel.IsVisible  = true;
		CurrentValue.IsVisible = false;
		Chart.IsEnabled        = false;
		this.IsEnabled         = false;

		Chart.RenderRequest();
	}

	private void InitializeChartProperties( AvaPlot chart )
	{
		_chartInitialized = true;
		_chartStyle       = new CustomChartStyle( ChartForeground, ChartBackground, ChartBorderColor, ChartGridLineColor );
		
		var plot = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "88888.8", _chartStyle.TickLabelFontName, 12 );

		// We will be replacing most of the built-in mouse interactivity with bespoke functionality
		Chart.Configuration.ScrollWheelZoom      = false;
		Chart.Configuration.AltLeftClickDragZoom = false;
		Chart.Configuration.MiddleClickAutoAxis  = false;
		Chart.Configuration.MiddleClickDragZoom  = false;
		Chart.Configuration.LockVerticalAxis     = true;
		Chart.Configuration.LeftClickDragPan     = false;
		Chart.Configuration.RightClickDragZoom   = false;
		Chart.Configuration.Quality              = ScottPlot.Control.QualityMode.LowWhileDragging;

		plot.Style( _chartStyle );
		plot.Layout( 0, 0, 0, 0 );
		plot.Margins( 0.0, 0.1 );
		
		plot.XAxis.TickLabelFormat( x => $"{TimeSpan.FromSeconds( x ):c}" );
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

		if( ChartConfiguration is { AxisMinValue: not null, AxisMaxValue: not null } )
		{
			var extents = ChartConfiguration.AxisMaxValue.Value - ChartConfiguration.AxisMinValue.Value;
			plot.YAxis.SetBoundary( ChartConfiguration.AxisMinValue.Value, ChartConfiguration.AxisMaxValue.Value + extents * 0.1 );
		}

		var legend = plot.Legend();
		legend.Location     = Alignment.UpperRight;
		legend.Orientation  = ScottPlot.Orientation.Horizontal;
		legend.OutlineColor = _chartStyle.TickMajorColor;
		legend.FillColor    = _chartStyle.DataBackgroundColor;
		legend.FontColor    = _chartStyle.TitleFontColor;

		chart.Configuration.LockVerticalAxis = true;
		
		chart.RenderRequest();
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
	
	#region Nested types

	private enum GraphInteractionMode
	{
		None,
		Panning,
		Zooming,
		Selecting,
	}
	
	#endregion 
}

