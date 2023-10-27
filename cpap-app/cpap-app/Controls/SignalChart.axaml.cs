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
using Avalonia.Styling;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Events;
using cpap_app.Styling;
using cpap_app.Helpers;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

using Brushes = Avalonia.Media.Brushes;
using Color = System.Drawing.Color;
using Point = Avalonia.Point;
using Cursor = Avalonia.Input.Cursor;

namespace cpap_app.Views;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public partial class SignalChart : UserControl
{
	#region Styled Properties

	public static readonly StyledProperty<IBrush> ChartBackgroundProperty    = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<IBrush> ChartGridLineColorProperty = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<IBrush> ChartForegroundProperty    = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<IBrush> ChartBorderColorProperty   = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBorderColor ) );

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
	
	public static readonly RoutedEvent<ChartConfigurationChangedEventArgs> ChartConfigurationChangedEvent = RoutedEvent.Register<SignalChart, ChartConfigurationChangedEventArgs>( nameof( ChartConfigurationChanged ), RoutingStrategies.Bubble );

	public static void AddChartConfigurationChangedHandler( IInputElement element, EventHandler<ChartConfigurationChangedEventArgs> handler )
	{
		element.AddHandler( ChartConfigurationChangedEvent, handler );
	}

	public event EventHandler<ChartConfigurationChangedEventArgs> ChartConfigurationChanged
	{
		add => AddHandler( ChartConfigurationChangedEvent, value );
		remove => RemoveHandler( ChartConfigurationChangedEvent, value );
	}

	public class ChartDragEventArgs : RoutedEventArgs
	{
		public int Direction { get; init; }
	}
	
	public static readonly RoutedEvent<ChartDragEventArgs> ChartDraggedEvent = RoutedEvent.Register<SignalChart, ChartDragEventArgs>( nameof( ChartDragged ), RoutingStrategies.Bubble );

	public event EventHandler<ChartDragEventArgs> ChartDragged
	{
		add => AddHandler( ChartDraggedEvent, value );
		remove => RemoveHandler( ChartDraggedEvent, value );
	}

	#endregion 
	
	#region Public properties

	public static readonly DirectProperty<SignalChart, SignalChartConfiguration?> ChartConfigurationProperty =
		AvaloniaProperty.RegisterDirect<SignalChart, SignalChartConfiguration?>( nameof( ChartConfiguration ), o => o.ChartConfiguration );

	public SignalChartConfiguration? ChartConfiguration
	{
		get => _chartConfiguration;
		set => SetAndRaise( ChartConfigurationProperty, ref _chartConfiguration, value );
	}
	public SignalChartConfiguration?      SecondaryConfiguration { get; set; }
	public List<EventMarkerConfiguration> MarkerConfiguration    { get; set; }

	public IBrush ChartForeground
	{
		get => GetValue( ChartForegroundProperty );
		set => SetValue( ChartForegroundProperty, value );
	}

	public IBrush ChartBackground
	{
		get => GetValue( ChartBackgroundProperty );
		set => SetValue( ChartBackgroundProperty, value );
	}

	public IBrush ChartGridLineColor
	{
		get => GetValue( ChartGridLineColorProperty );
		set => SetValue( ChartGridLineColorProperty, value );
	}

	public IBrush ChartBorderColor
	{
		get => GetValue( ChartBorderColorProperty );
		set => SetValue( ChartBorderColorProperty, value );
	}
	
	#endregion
	
	#region Private fields

	private const double MINIMUM_TIME_WINDOW = 30;

	private SignalChartConfiguration? _chartConfiguration;
	private CustomChartStyle          _chartStyle;
	private DailyReport?              _day                = null;
	private bool                      _hasDataAvailable   = false;
	private bool                      _chartInitialized   = false;
	private double                    _selectionStartTime = 0;
	private double                    _selectionEndTime   = 0;
	private GraphInteractionMode      _interactionMode    = GraphInteractionMode.None;
	private AxisLimits                _pointerDownAxisLimits;
	private Point                     _pointerDownPosition;

	private HSpan          _selectionSpan;
	private VLine          _hoverMarkerLine;
	private HSpan          _hoverMarkerSpan;
	private ReportedEvent? _hoverEvent = null;

	private List<IPlottable>    _eventMarkers = new();
	private List<ReportedEvent> _events       = new();
	
	private List<Signal> _signals          = new();
	private List<Signal> _secondarySignals = new();
	
	#endregion 
	
	#region Constructor 
	
	public SignalChart()
	{
		InitializeComponent();

		PointerWheelChanged += OnPointerWheelChanged;
		PointerEntered      += OnPointerEntered;
		PointerExited       += OnPointerExited;
		PointerPressed      += OnPointerPressed;
		PointerReleased     += OnPointerReleased;
		PointerMoved        += OnPointerMoved;
		
		Chart.AxesChanged     += OnAxesChanged;
		
		ChartLabel.PointerPressed     += ChartLabelOnPointerPressed;
		ChartLabel.PointerReleased    += ChartLabelOnPointerReleased;
		ChartLabel.PointerCaptureLost += ChartLabelOnPointerCaptureLost;
		ChartLabel.PointerMoved       += ChartLabelOnPointerMoved;

		//Chart.ContextMenu = null;
	}
	
	#endregion 
	
	#region Base class overrides 
	
	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
	
		EventTooltip.IsVisible   = false;
		TimeMarkerLine.IsVisible = false;
	
		InitializeChartProperties( Chart );
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
				EventTooltip.IsVisible   = false;
				
				Chart.RenderRequest();
			}
		}
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		switch( change.Property.Name )
		{
			case nameof( DataContext ):
				_signals.Clear();
				_secondarySignals.Clear();
			
				switch( change.NewValue )
				{
					case DailyReport _ when ChartConfiguration == null || string.IsNullOrEmpty( ChartConfiguration.SignalName ):
						throw new NullReferenceException( "No Signal name was provided" );
					case DailyReport day when !_chartInitialized:
						_day = day;
						break;
					case DailyReport day:
						LoadData( day );
						break;
					case null:
						IndicateNoDataAvailable();
						break;
				}
				break;
			case nameof( ChartConfiguration ):
				btnSettings.ChartConfiguration = change.NewValue as SignalChartConfiguration;
				break;
		}
	}

	#endregion 
	
	#region Event Handlers

	private void OnChartConfigurationChanged( object? sender, ChartConfigurationChangedEventArgs e )
	{
		switch( e.PropertyName )
		{
			case nameof( SignalChartConfiguration.PlotColor ):
				RefreshPlotColor();
				Chart.RenderRequest();
				break;
			case nameof( SignalChartConfiguration.DisplayedEvents ):
				RefreshEventMarkers();
				Chart.RenderRequest();
				break;
			case nameof( SignalChartConfiguration.FillBelow ):
				RefreshPlotFill();
				Chart.RenderRequest();
				break;
			case nameof( SignalChartConfiguration.ScalingMode ):
				// TODO: Refresh the chart's scaling mode
				break;
		}

		RaiseEvent( new ChartConfigurationChangedEventArgs
		{
			RoutedEvent        = ChartConfigurationChangedEvent,
			Source             = this,
			PropertyName       = e.PropertyName,
			ChartConfiguration = e.ChartConfiguration,
		} );
	}
	
	private void ChartLabelOnPointerMoved( object? sender, PointerEventArgs e )
	{
		var position = e.GetPosition( this );

		if( position.Y < 0 || position.Y > this.Bounds.Height )
		{
			RaiseEvent( new ChartDragEventArgs
			{
				RoutedEvent = ChartDraggedEvent,
				Source      = this,
				Direction   = Math.Sign( position.Y ),
			} );
		}

		e.Handled = true;
	}

	private void ChartLabelOnPointerCaptureLost( object? sender, PointerCaptureLostEventArgs e )
	{
		ChartLabel.Cursor = new Cursor( StandardCursorType.Arrow );
	}
	
	private void ChartLabelOnPointerReleased( object? sender, PointerReleasedEventArgs e )
	{
		ChartLabel.Cursor = new Cursor( StandardCursorType.Arrow );
	}
	
	private void ChartLabelOnPointerPressed( object? sender, PointerPressedEventArgs e )
	{
		if( e.Handled || !e.GetCurrentPoint( this ).Properties.IsLeftButtonPressed )
		{
			return;
		}
		
		ChartLabel.Cursor = new Cursor( StandardCursorType.SizeNorthSouth );

		e.Handled = true;
	}

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

		if( _interactionMode == GraphInteractionMode.None && e.Pointer.Captured == null )
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

		var mouseRelativePosition = eventArgs.GetCurrentPoint( Chart ).Position;

		(double timeOffset, _) = Chart.Plot.GetCoordinate( (float)mouseRelativePosition.X, (float)mouseRelativePosition.Y );
		var time = _day.RecordingStartTime.AddSeconds( timeOffset );

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
				if( eventArgs.Pointer.Captured == null )
				{
					UpdateTimeMarker( time );
					RaiseTimeMarkerChanged( time );
				}
				break;
			}
		}
	}

	#endregion 
	
	#region Public functions

	/// <summary>
	/// Intended to be called when moving the control from one parent to another. 
	/// SaveState() and RestoreState() are intended to be called as a pair during the procedure.  
	/// </summary>
	internal void SaveState()
	{
		_pointerDownAxisLimits = Chart.Plot.GetAxisLimits();
	}

	/// <summary>
	/// Intended to be called when moving the control from parent to another.
	/// SaveState() and RestoreState() are intended to be called as a pair during the procedure.  
	/// </summary>
	internal void RestoreState()
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		Chart.Plot.SetAxisLimits( _pointerDownAxisLimits );
		Chart.RenderRequest();
		Chart.Configuration.AxesChangedEventEnabled = true;
	}

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

	private void RefreshPlotFill( bool renderImmediately = false)
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		var fill = ChartConfiguration.FillBelow ?? false;

		var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		
		// TODO: Which alpha values actually work best for each theme?
		double alpha = isDarkTheme ? 0.35 : 0.45;

		var list = Chart.Plot.GetPlottables();
		foreach( var item in list )
		{
			if( item is SignalPlot plot )
			{
				if( !fill )
				{
					plot.FillDisable();
				}
				else
				{
					plot.FillBelow( plot.Color, Colors.Transparent.ToDrawingColor(), alpha );
				}
			}
		}

		if( renderImmediately )
		{
			Chart.RenderRequest();
		}
	}
	
	private void RefreshPlotColor( bool renderImmediately = false )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		var plottables = Chart.Plot.GetPlottables();
		foreach( var plottable in plottables )
		{
			if( plottable is SignalPlot plot )
			{
				plot.Color = ChartConfiguration.PlotColor;
			}
		}

		RefreshPlotFill( false );

		if( renderImmediately )
		{
			Chart.RenderRequest();
		}
	}

	private void RefreshEventMarkers( bool renderImmediately = false)
	{
		foreach( var marker in _eventMarkers )
		{
			Chart.Plot.Remove( marker );
		}
		
		_eventMarkers.Clear();
		_events.Clear();

		if( _day != null )
		{
			CreateEventMarkers( _day );

			if( renderImmediately )
			{
				Chart.RenderRequest();
			}
		}
	}

	private void EndSelectionMode()
	{
		// Sanity check
		if( _day == null )
		{
			return;
		}
		
		_interactionMode = GraphInteractionMode.None;

		if( _selectionStartTime > _selectionEndTime )
		{
			(_selectionStartTime, _selectionEndTime) = (_selectionEndTime, _selectionStartTime);
		}

		// Try to differentiate between a click or simple mousedown and the user intending to select a time range
		var pixelDifference = Chart.Plot.XAxis.Dims.PxPerUnit * ( _selectionEndTime - _selectionStartTime );
		if( pixelDifference <= 2 )
		{
			_selectionSpan.IsVisible = false;
			Chart.RenderRequest();

			return;
		}

		// Enforce maximum zoom
		if( _selectionEndTime < _selectionStartTime + MINIMUM_TIME_WINDOW )
		{
			var center = (_selectionStartTime + _selectionEndTime) / 2.0;
			_selectionStartTime = center - MINIMUM_TIME_WINDOW / 2.0;
			_selectionEndTime   = center + MINIMUM_TIME_WINDOW / 2.0;
		}
		
		ZoomTo( _selectionStartTime, _selectionEndTime );
		OnAxesChanged( this, EventArgs.Empty );

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
		TimeMarkerLine.EndPoint   = new Point( mousePosition, dataRect.Bottom + Chart.Plot.XAxis.AxisTicks.MajorTickLength );
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

			// Always display the name of the Signal, even when there is no data available
			ChartLabel.Text = ChartConfiguration.SignalName;

			// Check to see if there are any sessions with the named Signal. If not, display the "No Data Available" message and eject.
			_hasDataAvailable = day.Sessions.FirstOrDefault( x => x.GetSignalByName( ChartConfiguration.SignalName ) != null ) != null;
			if( !_hasDataAvailable )
			{
				IndicateNoDataAvailable();

				return;
			}
			else
			{
				NoDataLabel.IsVisible  = false;
				CurrentValue.IsVisible = true;
				Chart.IsEnabled        = true;
				this.IsEnabled         = true;
			}
			
			var isDarkTheme  = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
			var redlineAlpha = isDarkTheme ? 0.5f : 0.75f; 

			// If a RedLine position is specified, we want to add it before any signal data, as items are rendered in the 
			// order in which they are added, and we want the redline rendered behind the signal data.
			if( ChartConfiguration.BaselineHigh.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( redlineAlpha );
				Chart.Plot.AddHorizontalLine( ChartConfiguration.BaselineHigh.Value, redlineColor.ToDrawingColor(), 0.5f, LineStyle.Dot );
			}

			if( ChartConfiguration.BaselineLow.HasValue )
			{
				var redlineColor = Colors.Red.MultiplyAlpha( redlineAlpha );
				Chart.Plot.AddHorizontalLine( ChartConfiguration.BaselineLow.Value, redlineColor.ToDrawingColor(), 0.5f, LineStyle.Dot );
			}

			PlotSignal( Chart, day, ChartConfiguration.SignalName, ChartConfiguration.AxisMinValue, ChartConfiguration.AxisMaxValue );
			RefreshPlotFill( false );

			// TODO: This should come *before* ChartSignal(), but relies on the axis limits being finalized first. Fix that.
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
	
	private void PlotSignal( AvaPlot chart, DailyReport day, string signalName, double? axisMinValue = null, double? axisMaxValue = null )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		
		if( ChartConfiguration == null )
		{
			throw new Exception( $"The {nameof( ChartConfiguration )} property has not been assigned" );
		}
		
		var dataMinValue = double.MaxValue;
		var signalMinValue = double.MaxValue;
		var dataMaxValue = double.MinValue;
		var signalMaxValue = double.MinValue;

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
			
			// Keep track of min and max values 
			dataMinValue   = Math.Min( dataMinValue,   signal.Samples.Min() );
			signalMinValue = Math.Min( signalMinValue, signal.MinValue );
			dataMaxValue   = Math.Max( dataMaxValue,   signal.Samples.Max() );
			signalMaxValue = Math.Max( signalMaxValue, signal.MaxValue );
			
			// Keep track of all of the signals that this graph displays. This is done partially so that we don't 
			// have to search for the signals during time-sensitive operations such as mouse movement, etc. 
			_signals.Add( signal );

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
					// Keep track of min and max values 
					dataMinValue = Math.Min( dataMinValue, secondarySignal.Samples.Min() );
					dataMaxValue = Math.Max( dataMaxValue, secondarySignal.Samples.Max() );

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

			graph.LineWidth   = 1.1;
			graph.OffsetX     = offset;
			graph.MarkerSize  = 0;
			graph.UseParallel = true;
			
			firstSessionAdded = false;
		}

		// Set zoom and boundary limits
		{
			var minValue = signalMinValue;
			var maxValue = signalMaxValue;

			switch( ChartConfiguration.ScalingMode )
			{
				case AxisScalingMode.Defaults:
					minValue = signalMinValue;
					maxValue = signalMaxValue;
					break;
				case AxisScalingMode.AutoFit:
					if( Math.Abs( dataMaxValue - dataMinValue ) < float.Epsilon )
					{
						dataMaxValue += 1;
					}
					minValue = dataMinValue;
					maxValue = dataMaxValue;
					break;
				case AxisScalingMode.Override:
					minValue = axisMinValue ?? signalMinValue;
					maxValue = axisMaxValue ?? signalMaxValue;
					break;
				default:
					throw new ArgumentOutOfRangeException( $"Value {ChartConfiguration.ScalingMode} is not handled" );
			}

			// TODO: This is probably not the best place (or way) to be doing this, but the data is conveniently available...
			ChartConfiguration.AxisMinValue ??= signalMinValue;
			ChartConfiguration.AxisMaxValue ??= signalMaxValue;

			var extents = Math.Max( 1.0, maxValue - minValue );
			var padding = ChartConfiguration.ScalingMode == AxisScalingMode.AutoFit ? extents * 0.1 : 0;

			chart.Plot.YAxis.SetBoundary( minValue, maxValue + padding );
			chart.Plot.XAxis.SetBoundary( -1, day.TotalTimeSpan.TotalSeconds + 1 );
			chart.Plot.SetAxisLimits( -1, day.TotalTimeSpan.TotalSeconds + 1, minValue, maxValue + padding );

			double tickSpacing = extents / 4;
			chart.Plot.YAxis.ManualTickSpacing( tickSpacing );
		}
	}
	
	private void CreateEventMarkers( DailyReport day )
	{
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

				IPlottable? marker = null;

				switch( markerConfig.EventMarkerType )
				{
					case EventMarkerType.Flag:
						marker = Chart.Plot.AddVerticalLine( markerOffset, color, 1.5f, LineStyle.Solid, null );
						break;
					case EventMarkerType.TickTop:
						var topLine = Chart.Plot.AddMarker( markerOffset, limits.YMax, MarkerShape.verticalBar, 32, markerConfig.Color, null );
						topLine.MarkerLineWidth = 1.5f;
						marker = topLine;
						break;
					case EventMarkerType.TickBottom:
						var bottomLine = Chart.Plot.AddMarker( markerOffset, limits.YMin, MarkerShape.verticalBar, 32, markerConfig.Color, null );
						bottomLine.MarkerLineWidth = 1.5f;
						marker = bottomLine;
						break;
					case EventMarkerType.ArrowTop:
						marker = Chart.Plot.AddMarker( markerOffset, limits.YMax, MarkerShape.filledTriangleDown, 16, markerConfig.Color, null );
						break;
					case EventMarkerType.ArrowBottom:
						marker = Chart.Plot.AddMarker( markerOffset, limits.YMin, MarkerShape.filledTriangleUp, 16, markerConfig.Color, null );
						break;
					case EventMarkerType.Span:
						marker = Chart.Plot.AddHorizontalSpan( startOffset, endOffset, color.MultiplyAlpha( 0.35f ) );
						break;
					case EventMarkerType.None:
						continue;
					default:
						throw new ArgumentOutOfRangeException( $"Unhandled {nameof( EventMarkerType )} value {markerConfig.EventMarkerType}" );
				}

				if( marker != null )
				{
					_eventMarkers.Add( marker );
					_events.Add( eventFlag );
				}
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

		Chart.Plot.XAxis.ManualTickSpacing( 3600 );
		Chart.Plot.YAxis.ManualTickSpacing( 5 );

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
		plot.Layout( 0, 0, 0, 8 );
		//plot.Margins( 0.0, 0.1 );
		
		plot.XAxis.TickLabelFormat( TickFormatter );
		//plot.XAxis.TickLabelFormat( x => $"{TimeSpan.FromSeconds( x ):c}" );
		plot.XAxis.MinimumTickSpacing( 1f );
		plot.XAxis.SetZoomInLimit( MINIMUM_TIME_WINDOW ); // Make smallest zoom window possible be 1 minute 
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.MajorGrid( false );
		plot.XAxis.AxisTicks.MajorTickLength = 15;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis2.Layout( 8, 1, 1 );

		plot.YAxis.TickDensity( 1f );
		plot.YAxis.TickLabelFormat( x => $"{x:0.##}" );
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
	
	private string TickFormatter( double time )
	{
		return _day == null ? $"00:00:00" : $"{_day.RecordingStartTime.AddSeconds( time ):hh:mm:ss tt}";
	}

	private static float MeasureText( string text, string fontFamily, float emSize )
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
		Selecting,
	}
	
	#endregion
}

