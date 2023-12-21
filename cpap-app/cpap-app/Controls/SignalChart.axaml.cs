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
using Avalonia.Threading;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Events;
using cpap_app.Styling;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Control;
using ScottPlot.Plottable;

using Annotation = cpaplib.Annotation;
using Application = Avalonia.Application;
using Brushes = Avalonia.Media.Brushes;
using Color = System.Drawing.Color;
using Point = Avalonia.Point;
using Cursor = Avalonia.Input.Cursor;

namespace cpap_app.Controls;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public partial class SignalChart : UserControl
{
	#region Events 
	
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
	
	#region Styled Properties

	public static readonly StyledProperty<IBrush> ChartBackgroundProperty    = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<IBrush> ChartGridLineColorProperty = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<IBrush> ChartForegroundProperty    = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<IBrush> ChartBorderColorProperty   = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBorderColor ) );

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

	private const double MINIMUM_TIME_WINDOW = 15;

	private SignalChartConfiguration? _chartConfiguration;
	private CustomChartStyle          _chartStyle;
	private DailyReport?              _day                = null;
	private bool                      _hasInputFocus      = false;
	private bool                      _hasDataAvailable   = false;
	private bool                      _chartInitialized   = false;
	private GraphInteractionMode      _interactionMode    = GraphInteractionMode.None;
	private AxisLimits                _pointerDownAxisLimits;
	private Point                     _pointerDownPosition;

	private double _selectionStartTime = 0;
	private double _selectionEndTime   = 0;
	private HSpan  _selectionSpan;
	
	private VLine          _hoverMarkerLine;
	private HSpan          _hoverMarkerSpan;
	private ReportedEvent? _hoverEvent = null;

	private List<IPlottable>    _visualizations    = new();
	private List<IPlottable>    _eventMarkers      = new();
	private List<ReportedEvent> _events            = new();
	private List<IPlottable>    _annotationMarkers = new();
	
	private List<Signal>     _signals          = new();
	private List<SignalPlot> _signalPlots      = new();
	private List<Signal>     _secondarySignals = new();
	
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
		GotFocus            += OnGotFocus;
		LostFocus           += OnLostFocus;
		
		Chart.AxesChanged += OnAxesChanged;

		// TODO: Replace the default ScottPlot context menu with a bespoke version 
		//Chart.ContextMenu = null;
		
		ChartLabel.PointerPressed     += ChartLabelOnPointerPressed;
		ChartLabel.PointerReleased    += ChartLabelOnPointerReleased;
		ChartLabel.PointerCaptureLost += ChartLabelOnPointerCaptureLost;
		ChartLabel.PointerMoved       += ChartLabelOnPointerMoved;
	}

	#endregion 
	
	#region Base class overrides 
	
	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
	
		EventTooltip.IsVisible   = false;
		TimeMarkerLine.IsVisible = false;

		if( !_chartInitialized )
		{
			InitializeChartProperties( Chart );
		}
	}

	protected override void OnKeyDown( KeyEventArgs args )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		switch( args.Key )
		{
			case Key.Left or Key.Right:
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
				RenderGraph( false );
				OnAxesChanged( this, EventArgs.Empty ); 
			
				args.Handled = true;
				break;
			}
			case Key.Up or Key.Down:
			{
				double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.35 : 0.2;
				double amount    = (args.Key == Key.Up ? 1.0 : -1.0) * increment + 1.0;
			
				Chart.Plot.AxisZoom( amount, 1.0 );

				args.Handled = true;

				HideTimeMarker();
				RenderGraph( false );
				OnAxesChanged( this, EventArgs.Empty ); 

				break;
			}
			case Key.Escape:
			{
				if( _interactionMode == GraphInteractionMode.Selecting )
				{
					CancelSelectionMode();
				}
				break;
			}
			case Key.A:
			{
				if( args.KeyModifiers == KeyModifiers.None )
				{
					// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
					switch( _interactionMode )
					{
						case GraphInteractionMode.Selecting:
							AddAnnotationForCurrentSelection();
							break;
						case GraphInteractionMode.None when _selectionStartTime > 0:
							_selectionEndTime = _selectionStartTime;
							AddAnnotationForCurrentSelection();
							break;
					}
				}
				break;
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
				_signalPlots.Clear();
				_visualizations.Clear();
			
				switch( change.NewValue )
				{
					case DailyReport _ when ChartConfiguration == null || string.IsNullOrEmpty( ChartConfiguration.SignalName ):
						throw new NullReferenceException( "Missing or incorrect configuration" );
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

				if( btnSettings.Visualizations.Count == 0 )
				{
					InitializeVisualizationsMenu();
				}
				
				// Always display the name of the Signal, even when there is no data available
				ChartLabel.Text  = ChartConfiguration?.Title;
				NoDataLabel.Text = $"There is no {ChartConfiguration?.Title ?? "signal"} data available";
				break;
		}
	}

	#endregion 
	
	#region Event Handlers

	private void OnAnnotationsChanged( object? sender, AnnotationListEventArgs e )
	{
		// If this Graph is not showing any data, then Annotations don't matter 
		if( _day == null )
		{
			return;
		}

		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		if( _annotationMarkers.Count == 0 && !_day.Annotations.Any( x => x.Signal == ChartConfiguration.Title ) )
		{
			return;
		}
		
		foreach( var marker in _annotationMarkers )
		{
			Chart.Plot.Remove( marker );	
		}

		CreateAnnotationMarkers( _day );
		
		Chart.Render();
	}

	private void OnLostFocus( object? sender, RoutedEventArgs e )
	{
		FocusAdornerBorder.Classes.Remove( "FocusAdorner" );
		_hasInputFocus = false;
		
		CancelSelectionMode();
	}
	
	private void OnGotFocus( object? sender, GotFocusEventArgs e )
	{
		FocusAdornerBorder.Classes.Add( "FocusAdorner" );
		_hasInputFocus = true;
	}

	private void OnChartConfigurationChanged( object? sender, ChartConfigurationChangedEventArgs e )
	{
		switch( e.PropertyName )
		{
			case nameof( SignalChartConfiguration.PlotColor ):
				RefreshPlotColor();
				RenderGraph( true );
				break;
			case nameof( SignalChartConfiguration.DisplayedEvents ):
				RefreshEventMarkers();
				RenderGraph( true );
				break;
			case nameof( SignalChartConfiguration.FillBelow ):
				RefreshPlotFill();
				RenderGraph( true );
				break;
			case nameof( SignalChartConfiguration.ScalingMode ):
			case nameof( SignalChartConfiguration.AxisMinValue ):
			case nameof( SignalChartConfiguration.AxisMaxValue ) :
				// TODO: Refresh the chart's scaling mode
				RefreshChartAxisLimits();
				RenderGraph( true );
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

	private void OnPointerEntered( object? sender, PointerEventArgs e )
	{
		if( _day == null || !IsEnabled )
		{
			return;
		}

		if( _interactionMode == GraphInteractionMode.None && e.Pointer.Captured == null )
		{
			var currentPoint = e.GetPosition( Chart );
			var timeOffset   = Chart.Plot.XAxis.Dims.GetUnit( (float)currentPoint.X );

			// Race condition: The chart may not be fully set up yet
			if( double.IsNaN( timeOffset ) )
			{
				return;
			}

			var time = _day.RecordingStartTime.AddSeconds( timeOffset );

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
		// Don't attempt to do anything if some of the necessary objects have not yet been created.
		// This was added mostly to prevent exceptions from being thrown in the previewer in design mode.
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if( ChartConfiguration == null || _selectionSpan == null )
		{
			return;
		}
		
		_selectionSpan.IsVisible = false;
		EventTooltip.IsVisible   = false;
		
		switch( _interactionMode )
		{
			case GraphInteractionMode.Selecting:
				EndSelectionMode();
				break;
			case GraphInteractionMode.Panning:
				// The chart was rendered in low quality while panning, so re-render in high quality now that we're done 
				//RenderGraph( true );
				break;
		}

		_interactionMode = GraphInteractionMode.None;
	}

	private void OnPointerPressed( object? sender, PointerPressedEventArgs eventArgs )
	{
		// Don't attempt to do anything if some of the necessary objects have not yet been created.
		// This was added mostly to prevent exceptions from being thrown in the previewer in design mode.
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if( ChartConfiguration == null || _selectionSpan == null )
		{
			return;
		}

		var point = eventArgs.GetCurrentPoint( this );
		if( point.Properties.IsMiddleButtonPressed )
		{
			return;
		}

		if( eventArgs.Handled || _interactionMode != GraphInteractionMode.None )
		{
			return;
		}

		_selectionStartTime = 0;
		_selectionEndTime   = 0;
		
		HideTimeMarker();

		// We will want to do different things depending on where the PointerPressed happens, such 
		// as within the data area of the graph versus on the chart title, etc. 
		var dataRect = GetDataAreaBounds();
		if( !dataRect.Contains( point.Position ) )
		{
			return;
		}
		
		if( eventArgs.KeyModifiers == KeyModifiers.None && point.Properties.IsLeftButtonPressed )
		{
			(_selectionStartTime, _) = Chart.GetMouseCoordinates();
			_selectionEndTime        = _selectionStartTime;
			_selectionSpan.X1        = _selectionStartTime;
			_selectionSpan.X2        = _selectionStartTime;
			_selectionSpan.IsVisible = true;

			_interactionMode = GraphInteractionMode.Selecting;
			
			eventArgs.Handled = true;
		}
		else if( eventArgs.KeyModifiers == KeyModifiers.Shift && point.Properties.IsLeftButtonPressed )
		{
			(_selectionStartTime, _) = Chart.GetMouseCoordinates();
			_selectionEndTime        = _selectionStartTime;
			_selectionSpan.X1        = _selectionStartTime;
			_selectionSpan.X2        = _selectionStartTime;
			_selectionSpan.IsVisible = true;

			// Provide a 3-minute zoom window around the clicked position
			ZoomTo( _selectionStartTime - 1.5 * 60, _selectionEndTime + 1.5 * 60 );
			
			eventArgs.Handled = true;
		}
		else if( (eventArgs.KeyModifiers & KeyModifiers.Control) != 0 || point.Properties.IsRightButtonPressed )
		{
			if( !_hasInputFocus )
			{
				Focus();
			}
			
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

			var amount = Math.Max( args.Delta.Y * 0.25 + 1.0, 0.25 );
			Chart.Plot.AxisZoom( amount, 1.0, x, y );

			args.Handled = true;

			HideTimeMarker();
			OnAxesChanged( this, EventArgs.Empty );
			Focus();
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

		// Race condition: Ignore this event when the chart is not yet fully set up
		if( double.IsNaN( timeOffset ) )
		{
			return;
		}
		
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
				var totalSeconds      = timeRangeSelected.TotalSeconds;

				EventTooltip.Tag       = totalSeconds > 1.0 ? FormattedTimespanConverter.FormatTimeSpan( timeRangeSelected, TimespanFormatType.Long, false ) : $"{totalSeconds:N1} seconds";
				EventTooltip.IsVisible = totalSeconds > double.Epsilon;
				
				eventArgs.Handled = true;
			
				RenderGraph( false );
			
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
				OnAxesChanged( this, EventArgs.Empty );

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

	public void RenderGraph( bool highQuality )
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		Chart.Configuration.Quality = highQuality ? QualityMode.High : QualityMode.Low;
		
		Chart.RenderRequest();
		
		Chart.Configuration.AxesChangedEventEnabled = true;
	}

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

		RenderGraph( true );

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

	#endregion 
	
	#region Private functions

	private Rect GetDataAreaBounds()
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
	
	private void OnAxesChanged( object? sender, EventArgs e )
	{
		if( _day == null || !_hasDataAvailable || !IsEnabled )
		{
			return;
		}
		
		var currentAxisLimits = Chart.Plot.GetAxisLimits();

		var eventArgs = new DateTimeRangeRoutedEventArgs
		{
			RoutedEvent = GraphEvents.DisplayedRangeChangedEvent,
			Source      = this,
			StartTime   = _day.RecordingStartTime.AddSeconds( currentAxisLimits.XMin ),
			EndTime     = _day.RecordingStartTime.AddSeconds( currentAxisLimits.XMax )
		};

		RaiseEvent( eventArgs );
	}

	private void CancelSelectionMode()
	{
		_interactionMode         = GraphInteractionMode.None;
		_selectionStartTime      = 0;
		_selectionEndTime        = 0;
		_selectionSpan.IsVisible = false;
		EventTooltip.IsVisible   = false;

		RenderGraph( true );
	}
	
	private void AddAnnotationForCurrentSelection()
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );

		if( _day is not DailyReportViewModel dayVM )
		{
			throw new NullReferenceException();
		}
		
		if( _selectionEndTime < _selectionStartTime )
		{
			(_selectionStartTime, _selectionEndTime) = (_selectionEndTime, _selectionStartTime);
		}

		var startTime = _day.RecordingStartTime.AddSeconds( _selectionStartTime );
		var endTime   = _day.RecordingStartTime.AddSeconds( _selectionEndTime );

		dayVM.CreateNewAnnotation( ChartConfiguration.Title, startTime, endTime );
		
		CancelSelectionMode();
	}

	protected virtual void InitializeVisualizationsMenu()
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		List<SignalMenuItem> standardItems = new List<SignalMenuItem>
		{
			new SignalMenuItem( "Sliding Average",              VisualizeSlidingAverage ),
			new SignalMenuItem( "Standard Deviation (Sliding)", VisualizeStandardDeviation ),
			new SignalMenuItem( "Average (Entire series)",      VisualizeAverage ),
			new SignalMenuItem( "Median (Entire series)",       VisualizeMedian ),
			new SignalMenuItem( "95th Percentile",              VisualizePercentile95 ),
			new SignalMenuItem( "-",                            () => { } ),
			new SignalMenuItem( "Respiration Rate",             VisualizeRespirationRate ),
			new SignalMenuItem( "Tidal Volume",                 VisualizeTidalVolume ),
		};

		if( ChartConfiguration.SignalName == SignalNames.FlowRate )
		{
			btnSettings.Visualizations.Add( new SignalMenuItem( "Sliding Average Flow", VisualizeRMS ) );
			btnSettings.Visualizations.Add( new SignalMenuItem( "Highlight Centerline", VisualizeCenterline ) );
			btnSettings.Visualizations.Add( new SignalMenuItem( "Mark Respirations",    VisualizeRespirations ) );
			btnSettings.Visualizations.Add( new SignalMenuItem( "Show Noise Filter",    VisualizeNoiseFilter ) );
		}
		else
		{
			btnSettings.Visualizations.AddRange( standardItems );
		}

		btnSettings.Visualizations.Add( new SignalMenuItem( "-",                    () => { } ) );
		btnSettings.Visualizations.Add( new SignalMenuItem( "Clear Visualizations", ClearVisualizations ) );
	}
	
	private void VisualizeRespirationRate()
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		foreach( var session in _day.Sessions )
		{
			var flowSignal = session.GetSignalByName( SignalNames.FlowRate );
			if( flowSignal == null )
			{
				return;
			}

			var breaths         = BreathDetection.DetectBreaths( flowSignal );
			var respirationRate = DerivedSignals.GenerateRespirationRateSignal( breaths );

			var graph = Chart.Plot.AddSignal(
				respirationRate.Samples.ToArray(),
				respirationRate.FrequencyInHz,
				Color.Magenta,
				null
			);

			graph.OffsetX    = (respirationRate.StartTime - _day.RecordingStartTime).TotalSeconds;
			graph.LineStyle  = LineStyle.Solid;
			graph.MarkerSize = 0;

			Chart.RenderRequest();

			_visualizations.Add( graph );
		}

		RenderGraph( true );
	}

	private void VisualizeTidalVolume()
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		foreach( var session in _day.Sessions )
		{
			var flowSignal = session.GetSignalByName( SignalNames.FlowRate );
			if( flowSignal == null )
			{
				return;
			}

			var breaths           = BreathDetection.DetectBreaths( flowSignal, useVariableBaseline: true );
			var tidalVolumeSignal = DerivedSignals.GenerateTidalVolumeSignal( breaths );

			var graph = Chart.Plot.AddSignal(
				tidalVolumeSignal.Samples.ToArray(),
				tidalVolumeSignal.FrequencyInHz,
				Color.DarkMagenta,
				null
			);

			graph.OffsetX    = (tidalVolumeSignal.StartTime - _day.RecordingStartTime).TotalSeconds;
			graph.LineStyle  = LineStyle.Solid;
			graph.MarkerSize = 0;

			Chart.RenderRequest();

			_visualizations.Add( graph );
		}

		RenderGraph( true );
	}

	private async void VisualizeRMS()
	{
		var windowLengthInSeconds = await InputDialog.InputInteger(
			TopLevel.GetTopLevel( this )!,
			"Visualize Average Flow",
			"Enter the length of the window, in seconds",
			120,
			2,
			60 * 60 * 60
		);

		if( windowLengthInSeconds == null )
		{
			return;
		}
		
		VisualizeRMS( windowLengthInSeconds.Value, Color.DeepPink, $"RMS ({windowLengthInSeconds})" );

		RenderGraph( true );
	}
	
	private void VisualizeRMS( int windowLength, Color color, string label )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );

		var timeRange = Chart.Plot.GetAxisLimits();
		var startTime = _day.RecordingStartTime.AddSeconds( timeRange.XMin );
		var endTime   = _day.RecordingStartTime.AddSeconds( timeRange.XMax );

		bool first = true;

		foreach( var signal in _signals )
		{
			if( !DateHelper.RangesOverlap( signal.StartTime, signal.EndTime, startTime, endTime ) )
			{
				continue;
			}

			var absFlow      = signal.Samples.Select( x => x = Math.Abs( x ) ).ToArray();
			var filteredFlow = ButterworthFilter.Filter( absFlow, signal.FrequencyInHz, 1 );

			var calc   = new MovingAverageCalculator( (int)(windowLength * signal.FrequencyInHz) );
			var output = new double[ signal.Samples.Count ];

			for( int i = 0; i < signal.Samples.Count; i++ )
			{
				var sample = filteredFlow[ i ];
				
				calc.AddObservation( sample );

				if( !calc.HasFullPeriod )
				{
					output[ i ] = 0;
					continue;
				}

				var rms = calc.Average + calc.StandardDeviation;

				output[ i ] = rms;
			}

			var graph = Chart.Plot.AddSignal( output, signal.FrequencyInHz, color, first ? label : null );
			graph.OffsetX    = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;
			graph.LineStyle  = LineStyle.Solid;
			graph.MarkerSize = 0;

			_visualizations.Add( graph );

			first = false;
		}
	}

	private void VisualizeNoiseFilter()
	{
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );
	
		var timeRange = Chart.Plot.GetAxisLimits();
		var startTime = _day.RecordingStartTime.AddSeconds( timeRange.XMin );
		var endTime   = _day.RecordingStartTime.AddSeconds( timeRange.XMax );

		bool first = true;
		
		foreach( var signal in _signals )
		{
			if( !DateHelper.RangesOverlap( signal.StartTime, signal.EndTime, startTime, endTime ) )
			{
				continue;
			}

			#if TRUE
				var filtered = ButterworthFilter.Filter( signal.Samples.ToArray(), signal.FrequencyInHz, 1 );
			#else
				var filtered = SmoothingFilter.Filter( signal.Samples, 3, 1.0 / 3.0 );
			#endif

			var graph = Chart.Plot.AddSignal( filtered, signal.FrequencyInHz, Color.Magenta, first ? "Filtered" : null );
			graph.OffsetX    = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;
			graph.LineStyle  = LineStyle.Solid;
			graph.MarkerSize = 0;

			_visualizations.Add( graph );

			first = false;
		}

		RenderGraph( true );
	}

	private void VisualizeRespirations()
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );

		var timeRange = Chart.Plot.GetAxisLimits();
		var startTime = _day.RecordingStartTime.AddSeconds( timeRange.XMin );
		var endTime   = _day.RecordingStartTime.AddSeconds( timeRange.XMax );

		foreach( var signal in _signals )
		{
			if( !DateHelper.RangesOverlap( signal.StartTime, signal.EndTime, startTime, endTime ) )
			{
				continue;
			}

			var breaths = BreathDetection.DetectBreaths( signal, useVariableBaseline: true );

			double signalOffset = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;

			foreach( var breath in breaths )
			{
				if( !DateHelper.RangesOverlap( breath.StartInspiration, breath.EndTime, startTime, endTime ) )
				{
					continue;
				}
				
				var breathOffset = (breath.StartInspiration - signal.StartTime).TotalSeconds;
				
				var line = Chart.Plot.AddVerticalLine( signalOffset + breathOffset, Color.Red, 1f, LineStyle.Solid );
				_visualizations.Add( line );
			}
		}
		
		RenderGraph( true );
	}

	private void ClearVisualizations()
	{
		foreach( var loop in _visualizations )
		{
			Chart.Plot.Remove( loop );
		}
		
		_visualizations.Clear();
		
		RenderGraph( true );
	}

	private void VisualizePercentile95()
	{
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		var stats = _day.Statistics.FirstOrDefault( x => x.SignalName == ChartConfiguration.SignalName );
		if( stats != null )
		{
			var color = DataColors.GetDataColor( _visualizations.Count + 8 ).ToDrawingColor();

			_visualizations.Add( Chart.Plot.AddHorizontalLine( stats.Percentile95, color, 1f, LineStyle.Solid, "95%" ) );
			RenderGraph( true );
		}
	}

	private void VisualizeCenterline()
	{
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		var line = Chart.Plot.AddHorizontalLine( 0, Color.Red, 1f, LineStyle.Solid, "Baseline" );
		Chart.Plot.MoveFirst( line );
		
		_visualizations.Add( line );
		
		RenderGraph( true );
	}

	private void VisualizeMedian()
	{
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		var stats = _day.Statistics.FirstOrDefault( x => x.SignalName == ChartConfiguration.SignalName );
		if( stats != null )
		{
			var color = DataColors.GetDataColor( _visualizations.Count + 8 ).ToDrawingColor();

			_visualizations.Add( Chart.Plot.AddHorizontalLine( stats.Median, color, 1f, LineStyle.Solid, "Median" ) );
			RenderGraph( true );
		}
	}

	private void VisualizeAverage()
	{
		Debug.Assert( _day != null,               nameof( _day ) + " != null" );
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		var stats = _day.Statistics.FirstOrDefault( x => x.SignalName == ChartConfiguration.SignalName );
		if( stats != null )
		{
			var color = DataColors.GetDataColor( _visualizations.Count + 8 ).ToDrawingColor();

			_visualizations.Add( Chart.Plot.AddHorizontalLine( stats.Average, color, 1f, LineStyle.Solid, "Avg" ) );
			RenderGraph( true );
		}
	}

	private async void VisualizeSlidingAverage()
	{
		var windowLengthInSeconds = await InputDialog.InputInteger(
			TopLevel.GetTopLevel( this )!,
			"Visualize Sliding Average",
			"Enter the length of the window, in seconds",
			60,
			10,
			60 * 60 * 60
		);

		if( windowLengthInSeconds == null )
		{
			return;
		}
		
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		var  color = DataColors.GetDataColor( _visualizations.Count + 8 ).ToDrawingColor();
		color = Color.Red;

		bool first = true;
		foreach( var signal in _signals )
		{
			var numWindowSamples = (int)(windowLengthInSeconds * signal.FrequencyInHz);
			var calc             = new MovingAverageCalculator( numWindowSamples );

			var samples = new double[ signal.Samples.Count ];

			for( int i = 0; i < signal.Samples.Count; i++ )
			{
				var sample = signal.Samples[ i ];
				calc.AddObservation( sample );

				samples[ i ] = calc.Average;
			}

			var graph = Chart.Plot.AddSignal( samples, signal.FrequencyInHz, color, first ? $"Avg ({windowLengthInSeconds:F0})" : null );
			graph.OffsetX    = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;
			//graph.LineStyle  = LineStyle.Dash;
			graph.MarkerSize = 0;

			_visualizations.Add( graph );
			
			first = false;
		}
		
		RenderGraph( true );
	}
	
	private async void VisualizeStandardDeviation()
	{
		var windowLengthInSeconds = await InputDialog.InputInteger(
			TopLevel.GetTopLevel( this )!,
			"Visualize Standard Deviation",
			"Enter the length of the window, in seconds",
			60,
			10,
			60 * 60 * 60
		);

		if( windowLengthInSeconds == null )
		{
			return;
		}
		
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		var color = DataColors.GetDataColor( _visualizations.Count + 8 ).ToDrawingColor();

		color = Color.Red;
		
		bool first = true;
		foreach( var signal in _signals )
		{
			var numWindowSamples = (int)(windowLengthInSeconds * signal.FrequencyInHz);
			var calc             = new MovingAverageCalculator( numWindowSamples );

			var samples = new double[ signal.Samples.Count ];

			for( int i = 0; i < signal.Samples.Count; i++ )
			{
				var sample = signal.Samples[ i ];
				calc.AddObservation( sample );

				samples[ i ] = calc.Average + calc.StandardDeviation;

				Debug.Assert( !double.IsNaN( samples[ i ] ) && !double.IsInfinity( samples[ i ] ), "Unexpected invalid value in data" );
			}

			var graph = Chart.Plot.AddSignal( samples, signal.FrequencyInHz, color, first ? $"StdDev ({windowLengthInSeconds})" : null );
			graph.LineStyle  = LineStyle.Solid;
			graph.OffsetX    = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;
			graph.MarkerSize = 0;

			_visualizations.Add( graph );

			if( signal.MinValue < 0 )
			{
				var mirror = new double[ samples.Length ];
				for( int i = 0; i < samples.Length; i++ )
				{
					mirror[ i ] = -samples[ i ];
				}
				
				graph            = Chart.Plot.AddSignal( mirror, signal.FrequencyInHz, color, null );
				graph.LineStyle  = LineStyle.Dash;
				graph.OffsetX    = (signal.StartTime - _day.RecordingStartTime).TotalSeconds;
				graph.MarkerSize = 0;
			}
			
			first = false;
		}
		
		RenderGraph( true );
	}

	private void RefreshChartAxisLimits()
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		if( _signals.Count == 0 )
		{
			// This function might be called before initialization has been performed, or before data is available. 
			// In this case, it's okay to just return because init and data loading will perform the necessary
			// work when they happen.

			return;
		}
		
		var config            = ChartConfiguration;
		var signal            = _signals[ 0 ];
		var currentAxisLimits = Chart.Plot.GetAxisLimits();

		switch( config.ScalingMode )
		{
			case AxisScalingMode.Defaults:
				Chart.Plot.SetAxisLimits( currentAxisLimits.WithY( signal.MinValue, signal.MaxValue ) );
				break;
			case AxisScalingMode.AutoFit:
				setAutoFitAxisLimits();
				break;
			case AxisScalingMode.Override:
				Chart.Plot.SetAxisLimits( currentAxisLimits.WithY( config.AxisMinValue ?? signal.MinValue, config.AxisMaxValue ?? signal.MaxValue ) );
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		return;

		void setAutoFitAxisLimits()
		{
			var minDataValue = signal.MinValue >= 0 ? signal.MinValue : double.MaxValue;
			var maxDataValue = double.MinValue;

			foreach( var loop in _signals )
			{
				for( int i = 0; i < loop.Samples.Count; i++ )
				{
					var sample = loop[ i ];
					minDataValue = Math.Min( minDataValue, sample );
					maxDataValue = Math.Max( maxDataValue, sample );
				}
			}
			
			Chart.Plot.SetAxisLimits( currentAxisLimits.WithY( minDataValue, maxDataValue ) );
		}
	}

	private void RefreshPlotFill( bool renderImmediately = false)
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		var fill = ChartConfiguration.FillBelow ?? false;

		var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		
		// TODO: Which alpha values actually work best for each theme?
		double alpha = isDarkTheme ? 0.35 : 0.45;

		foreach( var plot in _signalPlots )
		{
			if( !fill )
			{
				plot.FillDisable();
			}
			else
			{
				if( ChartConfiguration.SignalName == SignalNames.FlowRate )
				{
					var fillColor = plot.Color;
					plot.FillAboveAndBelow( Color.Transparent, fillColor, Color.Transparent, fillColor, alpha );
					plot.BaselineColor = fillColor.MultiplyAlpha( (float)alpha );
					plot.BaselineWidth = 1;
				}
				else
				{
					plot.FillBelow( plot.Color, Colors.Transparent.ToDrawingColor(), alpha );
				}
			}
		}

		if( renderImmediately )
		{
			RenderGraph( true );
		}
	}
	
	private void RefreshPlotColor( bool renderImmediately = false )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		foreach( var plot in _signalPlots )
		{
			plot.Color = ChartConfiguration.PlotColor;
		}

		RefreshPlotFill( false );

		if( renderImmediately )
		{
			RenderGraph( true );
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
				RenderGraph( true );
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
			RenderGraph( true );

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
		
		RenderGraph( true );
	}
	
	private void HideEventHoverMarkers()
	{
		if( _hoverEvent != null )
		{
			_hoverMarkerSpan.IsVisible = false;
			_hoverMarkerLine.IsVisible = false;
			_hoverEvent                = null;
			
			RenderGraph( true );
		}
	}

	private void RaiseTimeMarkerChanged( DateTime time )
	{
		RaiseEvent( new DateTimeRoutedEventArgs()
		{
			RoutedEvent = GraphEvents.TimeMarkerChangedEvent,
			Source      = this,
			DateTime        = time
		} );
	}

	private void ZoomTo( double startTime, double endTime )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		
		if( !IsEnabled || !Chart.IsEnabled )
		{
			return;
		}
		
		// Don't allow zooming in closer than MINIMUM_TIME_WINDOW
		if( endTime - startTime < MINIMUM_TIME_WINDOW )
		{
			var center = (endTime + startTime) * 0.5;
			startTime = Math.Max( 0, center - 30 );
			endTime   = Math.Min( _day.TotalTimeSpan.TotalSeconds, center + 30 );
		}

		// disable events briefly to avoid an infinite loop
		var wasAxisChangedEventEnabled = Chart.Configuration.AxesChangedEventEnabled;
		Chart.Configuration.AxesChangedEventEnabled = false;
		{
			var currentAxisLimits  = Chart.Plot.GetAxisLimits();
			var modifiedAxisLimits = new AxisLimits( startTime, endTime, currentAxisLimits.YMin, currentAxisLimits.YMax );

			//Chart.Configuration.Quality = ScottPlot.Control.QualityMode.Low;
			Chart.Plot.SetAxisLimits( modifiedAxisLimits );
		}
		Chart.Configuration.AxesChangedEventEnabled = wasAxisChangedEventEnabled;
	}
	
	public void UpdateTimeMarker( DateTime time )
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
		var dataRect      = GetDataAreaBounds();
		var dims          = Chart.Plot.XAxis.Dims;
		var mousePosition = dims.PxPerUnit * (timeOffset - dims.Min) + dataRect.Left;

		_selectionStartTime      = double.MinValue;
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

		// TODO: Review why this specific line of code is so important to panning (without it, can't pan with right button, other panning is super slow)
		_selectionStartTime = _selectionEndTime = timeOffset;

		foreach( var signal in _signals )
		{
			// Signal start times may be slightly different than session start times, so need to check 
			// the signal itself also 
			if( signal.StartTime <= time && signal.EndTime >= time )
			{
				var value = signal.GetValueAtTime( time, !ChartConfiguration.ShowStepped );

				var valueText = $"{value:N2} {signal.UnitOfMeasurement}";
				
				// TODO: This shouldn't have to be a special case (at least not at this location in the code). Maybe pass in a formatter?
				if( ChartConfiguration.SignalName == SignalNames.InspToExpRatio )
				{
					valueText = value >= 1 ? $"1.0 \u2236 {value:F1}" : $"{value:F1} \u2236 1.0";
				}
				else if( ChartConfiguration.SignalName == SignalNames.SleepStages )
				{
					valueText = ((SleepStage)(int)value).ToString();
				}

				CurrentValue.Text = $"{time:T}        {ChartConfiguration.Title}: {valueText}";

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

		double         closestDistance = double.MaxValue;
		ReportedEvent? closestEvent    = null;
		
		// Find any events the mouse might be hovering over
		foreach( var flag in _events )
		{
			var bounds    = flag.GetTimeBounds();
			var startTime = (bounds.StartTime - _day.RecordingStartTime).TotalSeconds;
			var endTime   = (bounds.EndTime - _day.RecordingStartTime).TotalSeconds;
			
			if( timeOffset >= startTime - highlightDistance && timeOffset <= endTime + highlightDistance )
			{
				double distance = Math.Abs( timeOffset - startTime );
				if( timeOffset >= startTime && timeOffset <= endTime )
				{
					distance          = 0;
					highlightDistance = 0;
				}

				if( distance <= closestDistance )
				{
					closestDistance = distance;
					closestEvent    = flag;
				}
			}
		}

		if( closestEvent != null )
		{
			EventTooltip.Tag = $"{closestEvent.Type.ToName()}";
			if( closestEvent.Duration.TotalSeconds > 0 )
			{
				EventTooltip.Tag += $" ({FormattedTimespanConverter.FormatTimeSpan( closestEvent.Duration, TimespanFormatType.Short, false )})";
			}

			EventTooltip.IsVisible = true;

			ShowEventHoverMarkers( closestEvent );
			hoveringOverEvent = true;
		}
		else
		{
			Annotation? closestAnnotation = null;

			var annotationList = _day.Annotations.Where( x => x.Signal == ChartConfiguration.Title || x.Signal == ChartConfiguration.SignalName );
			foreach( var annotation in annotationList )
			{
				var startTime = (annotation.StartTime - _day.RecordingStartTime).TotalSeconds;
				var endTime   = (annotation.EndTime - _day.RecordingStartTime).TotalSeconds;

				if( timeOffset >= startTime - highlightDistance && timeOffset <= endTime + highlightDistance )
				{
					if( timeOffset >= startTime && timeOffset <= endTime )
					{
						highlightDistance = 0;
					}

					closestAnnotation = annotation;
				}
			}
			
			if( closestAnnotation != null )
			{
				EventTooltip.Tag       = closestAnnotation.Notes.Trim();
				EventTooltip.IsVisible = true;
			}
		}

		if( !hoveringOverEvent )
		{
			HideEventHoverMarkers();
		}
	}

	private void LoadData( DailyReport day )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		Debug.Assert( day != null,                $"{nameof( day )} == null" );
		
		if( day is not DailyReportViewModel viewModel )
		{
			throw new InvalidOperationException( $"Expected a {nameof( DailyReportViewModel )} instance" );
		}

		// TODO: Find out why graphs are being reloaded (only at startup? Not sure, but definitely then at least)
		if( _day != null && ReferenceEquals( _day, day ) )
		{
			// One way to know is whether being called with the same Day is an actual problem is to determine
			// whether Signal data is already loaded.  
			if( _signals.Count > 0 )
			{
				Debug.WriteLine( $"Re-loading {day.ReportDate},    Signal: {ChartConfiguration.SignalName},     Caller: {new System.Diagnostics.StackTrace()}" );
			}
		}
		
		_day = day;
		_events.Clear();

		// Subscribe to events that allow us to keep everything synchronized with the rest of the User Interface
		viewModel.AnnotationsChanged += OnAnnotationsChanged;
		
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
			_hasDataAvailable = day.HasDetailData && day.Sessions.FirstOrDefault( x => x.GetSignalByName( ChartConfiguration.SignalName ) != null ) != null;
			if( !_hasDataAvailable )
			{
				_day = null;
				IndicateNoDataAvailable();

				return;
			}
			else
			{
				NoDataLabel.IsVisible  = false;
				CurrentValue.IsVisible = true;
				Chart.IsEnabled        = true;
				this.IsEnabled         = true;
				btnSettings.IsEnabled  = true;
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

			// TODO: This should come *before* PlotSignal(), but relies on the axis limits being finalized first. Fix that.
			CreateEventMarkers( day );
			CreateAnnotationMarkers( day );

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
			RenderGraph( false );
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
			if( signal == null || signal.Samples.Count == 0 )
			{
				continue;
			}
			
			// Keep track of Min and Max values 
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

			if( ChartConfiguration.InvertAxisY )
			{
				graph.ScaleY = -1;
			}

			_signalPlots.Add( graph );

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
					// Keep track of Min and Max values 
					dataMinValue = Math.Min( dataMinValue, secondarySignal.Samples.Min() );
					dataMaxValue = Math.Max( dataMaxValue, secondarySignal.Samples.Max() );

					_secondarySignals.Add( secondarySignal );
					
					var secondaryGraph = chart.Plot.AddSignal( 
						secondarySignal.Samples.ToArray(), 
						secondarySignal.FrequencyInHz, 
						SecondaryConfiguration.PlotColor, 
						firstSessionAdded ? SecondaryConfiguration.Title : null );

					if( ChartConfiguration.InvertAxisY )
					{
						secondaryGraph.ScaleY = -1;
					}

					_signalPlots.Add( secondaryGraph );
					
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

			if( ChartConfiguration.InvertAxisY )
			{
				(minValue, maxValue) = (-maxValue, -minValue);
			}

			var extents = Math.Max( 1.0, maxValue - minValue );
			var padding = ChartConfiguration.ScalingMode == AxisScalingMode.AutoFit ? extents * 0.1 : 0;

			chart.Plot.YAxis.SetBoundary( minValue, maxValue + padding );
			chart.Plot.XAxis.SetBoundary( -1, day.TotalTimeSpan.TotalSeconds + 1 );
			chart.Plot.SetAxisLimits( -1, day.TotalTimeSpan.TotalSeconds + 1, minValue, maxValue + padding );

			double tickSpacing = extents / 4;
			chart.Plot.YAxis.ManualTickSpacing( tickSpacing );

			// TODO: This special-case code should not exist here. 
			if( ChartConfiguration.SignalName == SignalNames.SleepStages )
			{
				double[] positions = { 0, -1, -2, -3, -4, -5 };
				string[] labels    = { string.Empty, "Awake", "REM", "Light", "Deep", string.Empty };
				
				chart.Plot.YAxis.ManualTickPositions( positions, labels );
			}
		}
	}

	private void CreateAnnotationMarkers( DailyReport day )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		_annotationMarkers.Clear();
		
		var limits       = Chart.Plot.GetAxisLimits();
		var dims         = Chart.Plot.YAxis.Dims;
		var markerHeight = 6 * dims.UnitsPerPx;
		var top          = limits.YMax - dims.UnitsPerPx;
		var bottom       = top - markerHeight;

		foreach( var annotation in day.Annotations )
		{
			if( !annotation.ShowMarker || annotation.Signal != ChartConfiguration.Title )
			{
				continue;
			}

			var startOffset = (annotation.StartTime - day.RecordingStartTime).TotalSeconds;
			var endOffset   = (annotation.EndTime - day.RecordingStartTime).TotalSeconds;

			if( annotation.StartTime == annotation.EndTime )
			{
				startOffset -= 0.5;
				endOffset   += 0.5;
			}

			var currentTop    = top;
			var currentBottom = bottom;

			foreach( var existingMarker in _annotationMarkers )
			{
				var bounds     = existingMarker.GetAxisLimits();
				var isDisjoint = startOffset > bounds.XMax || endOffset < bounds.XMin;
				
				if( !isDisjoint )
				{
					currentTop    -= markerHeight;
					currentBottom -= markerHeight;
				}
			}
			
			var marker = Chart.Plot.AddRectangle( startOffset, endOffset, currentBottom, currentTop );
			marker.Color = Color.Yellow;
				
			_annotationMarkers.Add( marker );
			Chart.Plot.MoveFirst( marker );
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
					Debug.WriteLine( $"Missing event marker configuration for {eventFlag.Type}" );
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
		btnSettings.IsEnabled  = false;

		Chart.Plot.XAxis.AutomaticTickPositions();
		Chart.Plot.YAxis.AutomaticTickPositions();

		RenderGraph( true );
	}

	internal void InitializeChartProperties( AvaPlot chart )
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
		Chart.Configuration.Quality              = ScottPlot.Control.QualityMode.Low;

		plot.Style( _chartStyle );
		plot.Layout( 0, 0, 0, 8 );
		//plot.Margins( 0.0, 0.1 );
		
		plot.XAxis.TickLabelFormat( TickFormatter );
		//plot.XAxis.TickLabelFormat( x => $"{TimeSpan.FromSeconds( x ):c}" );
		plot.XAxis.MinimumTickSpacing( 1f );
		plot.XAxis.SetZoomInLimit( MINIMUM_TIME_WINDOW );
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.MajorGrid( false );
		//plot.XAxis.PixelSnap( true );
		plot.XAxis.AxisTicks.MajorTickLength = 15;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis2.Layout( 8, 1, 1 );

		plot.YAxis.TickDensity( 1f );
		plot.YAxis.TickLabelFormat( x => $"{x:0.##}" );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );
		// plot.YAxis.PixelSnap( true );

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

		if( _day != null )
		{
			Dispatcher.UIThread.Post( () =>
			{
				LoadData( _day );
			} );
		}
		
		// These changes won't be valid until the graph is rendered, so just render it in low resolution for now
		RenderGraph( false );
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
}

