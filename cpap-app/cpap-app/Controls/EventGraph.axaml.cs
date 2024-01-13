using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.Styling;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Control;
using ScottPlot.Plottable;

using Color = System.Drawing.Color;

namespace cpap_app.Controls;

public partial class EventGraph : UserControl
{
	#region Styled Properties

	public static readonly StyledProperty<IBrush> ChartBackgroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<IBrush> ChartAlternateBackgroundProperty = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartAlternateBackground ) );
	public static readonly StyledProperty<IBrush> ChartGridLineColorProperty       = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<IBrush> ChartForegroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<IBrush> ChartBorderColorProperty         = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBorderColor ) );
	public static readonly StyledProperty<IBrush> SessionBarColorProperty         = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( SessionBarColor ) );

	#endregion
	
	#region Public properties

	public static readonly DirectProperty<SignalChart, SignalChartConfiguration?> ChartConfigurationProperty =
		AvaloniaProperty.RegisterDirect<SignalChart, SignalChartConfiguration?>( nameof( ChartConfiguration ), o => o.ChartConfiguration );

	public SignalChartConfiguration? ChartConfiguration
	{
		get => _chartConfiguration;
		set => SetAndRaise( ChartConfigurationProperty, ref _chartConfiguration, value );
	}
	
	public List<EventMarkerConfiguration> MarkerConfiguration { get; set; } = null!;

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

	public IBrush ChartAlternateBackground
	{
		get => GetValue( ChartAlternateBackgroundProperty );
		set => SetValue( ChartAlternateBackgroundProperty, value );
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
	
	public IBrush SessionBarColor
	{
		get => GetValue( SessionBarColorProperty );
		set => SetValue( SessionBarColorProperty, value );
	}
	
	#endregion
	
	#region Private fields 
	
	private const double MINIMUM_TIME_WINDOW = 60;

	private SignalChartConfiguration? _chartConfiguration;
	private CustomChartStyle?         _chartStyle;
	private DailyReport?              _day              = null;
	private bool                      _chartInitialized = false;

	private GraphInteractionMode _interactionMode = GraphInteractionMode.None;
	private bool                 _hasInputFocus   = false;
	private Point                _pointerDownPosition;
	private AxisLimits           _pointerDownAxisLimits = AxisLimits.NoLimits;

	private double _selectionStartTime = 0;
	private double _selectionEndTime   = 0;
	private HSpan? _selectionSpan      = null;

	private HSpan? _leftOccluder  = null;
	private HSpan? _rightOccluder = null;

	#endregion 
	
	#region Constructor

	public EventGraph()
	{
		InitializeComponent();
		
		Chart.ContextMenu   = null;
		
		PointerWheelChanged += OnPointerWheelChanged;
		PointerEntered      += OnPointerEntered;
		PointerExited       += OnPointerExited;
		PointerPressed      += OnPointerPressed;
		PointerReleased     += OnPointerReleased;
		PointerMoved        += OnPointerMoved;
		GotFocus            += OnGotFocus;
		LostFocus           += OnLostFocus;
	}
	
	public EventGraph( List<EventMarkerConfiguration> markerConfiguration ) : this()
	{
		MarkerConfiguration = markerConfiguration;
	}

	#endregion
	
	#region Base class overrides 
	
	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
	
		TimeMarkerLine.IsVisible = false;

		if( !_chartInitialized )
		{
			InitializeChartProperties( Chart );
		}
	}

	protected override async void OnKeyDown( KeyEventArgs args )
	{
		if( _day == null )
		{
			return;
		}
		
		switch( args.Key )
		{
			case Key.Left or Key.Right:
			{
				var  startTime    = _leftOccluder!.X2;
				var  endTime      = _rightOccluder!.X1;
				var  range        = (endTime - startTime);
				bool isShiftDown  = (args.KeyModifiers & KeyModifiers.Shift) != 0;

				// If the SHIFT key is down, scroll by 25% of the visible timeframe.
				// Otherwise, scroll by 10% of the visible timeframe. 
				var amount = range * (isShiftDown ? 0.25 : 0.10);

				if( args.Key == Key.Left )
				{
					startTime = Math.Max( startTime - amount, 0 );
					endTime   = startTime + range;
				}
				else
				{
					endTime   = Math.Min( endTime + amount, _rightOccluder.X2 );
					startTime = endTime - range;
				}
			
				UpdateVisibleRange( startTime, endTime );
			
				OnAxesChanged( this, EventArgs.Empty ); 
			
				args.Handled = true;
				break;
			}
			case Key.PageUp or Key.PageDown:
			{
				var  startTime    = _leftOccluder!.X2;
				var  endTime      = _rightOccluder!.X1;
				var  range        = (endTime - startTime);

				if( args.Key == Key.PageUp )
				{
					startTime = Math.Max( startTime - range, 0 );
					endTime   = startTime + range;
				}
				else
				{
					endTime   = Math.Min( endTime + range, _rightOccluder.X2 );
					startTime = endTime - range;
				}
			
				UpdateVisibleRange( startTime, endTime );
			
				OnAxesChanged( this, EventArgs.Empty ); 
			
				args.Handled = true;
				break;
			}
			case Key.Up or Key.Down:
			{
				double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.35 : 0.2;
				double amount    = (args.Key == Key.Up ? -1.0 : 1.0) * increment + 1.0;
				var    range     = _rightOccluder!.X1 - _leftOccluder!.X2;
				var    center    = _leftOccluder.X2 + range * 0.5;

				range = Math.Max( range * Math.Max( amount, 0.25 ), MINIMUM_TIME_WINDOW );
				var left  = Math.Max( center - range * 0.5, _leftOccluder.X1 );
				var right = Math.Min( center + range * 0.5, _rightOccluder.X2 );

				UpdateVisibleRange( left, right );

				OnAxesChanged( this, EventArgs.Empty );
				
				args.Handled = true;
				break;
			}
			case Key.Home or Key.End:
			{
				var  startTime    = _leftOccluder!.X2;
				var  endTime      = _rightOccluder!.X1;
				var  range        = (endTime - startTime);

				if( args.Key == Key.Home )
				{
					UpdateVisibleRange( 0, 0 + range );
				}
				else
				{
					endTime   = _day.TotalTimeSpan.TotalSeconds;
					startTime = endTime - range;
			
					UpdateVisibleRange( startTime, endTime );
				}
			
				OnAxesChanged( this, EventArgs.Empty ); 
			
				args.Handled = true;
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
			case Key.R:
			{
				UpdateVisibleRange( 0, _day.TotalTimeSpan.TotalSeconds );
				OnAxesChanged( this, EventArgs.Empty );
				
				args.Handled = true;
				break;
			}
			case Key.D:
			{
				Debug.Assert( _leftOccluder != null,  nameof( _leftOccluder ) + " != null" );
				Debug.Assert( _rightOccluder != null, nameof( _rightOccluder ) + " != null" );
				
				var windowLengthInSeconds = await InputDialog.InputInteger(
					TopLevel.GetTopLevel( this )!,
					"Specify Viewport Duration",
					"Enter number of minutes",
					30,
					1,
					60 * 60
				);

				if( windowLengthInSeconds == null )
				{
					return;
				}

				// Convert duration to seconds 
				windowLengthInSeconds *= 60;

				var startTime  = _leftOccluder.X2;
				var endTime    = _rightOccluder.X1;
				var midPoint   = startTime + (endTime - startTime) * 0.5f;

				startTime = midPoint - windowLengthInSeconds.Value * 0.5f;
				endTime   = startTime + windowLengthInSeconds.Value;

				if( startTime < 0 )
				{
					startTime = 0;
					endTime   = windowLengthInSeconds.Value;
				}
				else if( endTime > _rightOccluder.X2 )
				{
					endTime   = _rightOccluder.X2;
					startTime = endTime - windowLengthInSeconds.Value;
				}

				UpdateVisibleRange( startTime, endTime );
				OnAxesChanged( this, EventArgs.Empty );

				args.Handled = true;
				break;
			}
		}
		
		if( args.Key is >= Key.D0 and <= Key.D9 )
		{
			int windowLength = 60 * ((args.Key == Key.D0) ? 10 : (int)args.Key - (int)Key.D0);

			var range  = _rightOccluder!.X1 - _leftOccluder!.X2;
			var center = _leftOccluder.X2 + range * 0.5;

			var left  = center - windowLength * 0.5f;
			var right = left + windowLength;

			if( left < 0 )
			{
				left = 0;
				right   = windowLength;
			}
			else if( right > _rightOccluder.X2 )
			{
				right = _rightOccluder.X2;
				left  = right - windowLength;
			}

			UpdateVisibleRange( left, right );
			OnAxesChanged( this, EventArgs.Empty );

			args.Handled = true;
		}
	}
	
	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		switch( change.Property.Name )
		{
			case nameof( DataContext ):
				switch( change.NewValue )
				{
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
		}
	}

	#endregion 
	
	#region Event handlers 
	
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
		
		switch( _interactionMode )
		{
			case GraphInteractionMode.Selecting:
			{
				// TODO: This still allows selecting areas of the Signal that are not in the graph's visible area. Leave it?
				_selectionEndTime = Math.Max( 0, Math.Min( timeOffset, _day.TotalTimeSpan.TotalSeconds ) );

				if( timeOffset < _selectionStartTime )
				{
					_selectionSpan!.X1 = _selectionEndTime;
					_selectionSpan!.X2 = _selectionStartTime;
				}
				else
				{
					_selectionSpan!.X1 = _selectionStartTime;
					_selectionSpan!.X2 = _selectionEndTime;
				}

				eventArgs.Handled = true;
			
				RenderGraph( false );
			
				return;
			}
			case GraphInteractionMode.Panning:
			{
				var position  = eventArgs.GetCurrentPoint( this ).Position;
				var panAmount = -(_pointerDownPosition.X - position.X) / Chart.Plot.XAxis.Dims.PxPerUnit;
			
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
				
				UpdateVisibleRange( start, end );
				OnAxesChanged( this, EventArgs.Empty );
			
				eventArgs.Handled = true;

				eventArgs.Handled = true;
				
				return;
			}
			case GraphInteractionMode.None:
			{
				break;
			}
		}
	}
	
	private void OnPointerReleased( object? sender, PointerReleasedEventArgs e )
	{
		// Don't attempt to do anything if some of the necessary objects have not yet been created.
		// This was added mostly to prevent exceptions from being thrown in the previewer in design mode.
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if( _selectionSpan == null )
		{
			return;
		}
		
		_selectionSpan.IsVisible = false;
		//EventTooltip.IsVisible   = false;
		
		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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
		if( DataContext == null )
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
		
		// We will want to do different things depending on where the PointerPressed happens, such 
		// as within the data area of the graph versus on the chart title, etc. 
		var dataRect = GetDataAreaBounds();
		if( !dataRect.Contains( point.Position ) )
		{
			return;
		}
		
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch( eventArgs.KeyModifiers )
		{
			case KeyModifiers.None when point.Properties.IsLeftButtonPressed:
				(_selectionStartTime, _) = Chart.GetMouseCoordinates();
				_selectionEndTime        = _selectionStartTime;
				_selectionSpan!.X1        = _selectionStartTime;
				_selectionSpan!.X2        = _selectionStartTime;
				_selectionSpan.IsVisible = true;

				_interactionMode = GraphInteractionMode.Selecting;
			
				eventArgs.Handled = true;
				break;
			case KeyModifiers.Shift when point.Properties.IsLeftButtonPressed:
				(_selectionStartTime, _) = Chart.GetMouseCoordinates();
				_selectionEndTime        = _selectionStartTime;
				_selectionSpan!.X1        = _selectionStartTime;
				_selectionSpan!.X2        = _selectionStartTime;
				_selectionSpan.IsVisible = true;

				// Provide a 3-minute zoom window around the clicked position
				UpdateVisibleRange( _selectionStartTime - 1.5 * 60, _selectionEndTime + 1.5 * 60 );
				OnAxesChanged( this, EventArgs.Empty );
			
				eventArgs.Handled = true;
				break;
			default:
			{
				if( point.Properties.IsRightButtonPressed )
				{
					if( !_hasInputFocus )
					{
						Focus();
					}
			
					Chart.Configuration.Quality = ScottPlot.Control.QualityMode.Low;

					_pointerDownPosition   = point.Position;
					_pointerDownAxisLimits = new AxisLimits( _leftOccluder!.X2, _rightOccluder!.X1, 0, 1 );
					_interactionMode       = GraphInteractionMode.Panning;
				}
				break;
			}
		}
	}
	
	private void OnPointerExited( object? sender, PointerEventArgs e )
	{
	}
	
	private void OnPointerEntered( object? sender, PointerEventArgs e )
	{
	}
	
	private void OnPointerWheelChanged( object? sender, PointerWheelEventArgs args )
	{
		// Because the charts are likely going to be used within a scrolling container, I've disabled the built-in mouse wheel 
		// handling which performs zooming, and re-implemented it here with the additional requirement that the Control key be
		// held down while scrolling the mouse wheel in order to zoom. If the Control key is held down, the chart will zoom in
		// and out and the event will be marked Handled so that it doesn't cause scrolling in the parent container. 
		if( (args.KeyModifiers & KeyModifiers.Control) != 0x00 )
		{
			var range  = _rightOccluder!.X1 - _leftOccluder!.X2;
			var center = _leftOccluder.X2 + range * 0.5;

			range = Math.Max( range * Math.Max( -args.Delta.Y * 0.25 + 1.0, 0.25 ), MINIMUM_TIME_WINDOW );
			var left  = Math.Max( center - range * 0.5, _leftOccluder.X1 );
			var right = Math.Min( center + range * 0.5, _rightOccluder.X2 );

			UpdateVisibleRange( left, right );

			args.Handled = true;

			OnAxesChanged( this, EventArgs.Empty );
			Focus();
		}
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

	#endregion 

	#region Public Functions

	public void UpdateVisibleRange( DateTime startTime, DateTime endTime )
	{
		if( _day == null || _leftOccluder == null )
		{
			return;
		}
		
		Debug.Assert( _leftOccluder != null, nameof( _leftOccluder ) + " != null" );
		_leftOccluder.X1 = 0;
		_leftOccluder.X2 = (startTime - _day.RecordingStartTime).TotalSeconds;

		Debug.Assert( _rightOccluder != null, nameof( _rightOccluder ) + " != null" );
		_rightOccluder.X1 = (endTime - _day.RecordingStartTime).TotalSeconds;
		_rightOccluder.X2 = (_day.RecordingEndTime - _day.RecordingStartTime).TotalSeconds;

		// Duration without fractional seconds 
		var duration = TimeSpan.FromSeconds( Math.Round( (endTime - startTime).TotalSeconds ) );

		var eventCount         = _day.Events.Count( x => x.StartTime >= startTime && x.StartTime <= endTime && EventTypes.Apneas.Contains( x.Type ) );
		var apneaHypopneaIndex = eventCount / Math.Max( 1, GetTotalSleepTime( startTime, endTime ).TotalHours );

		CurrentValue.Text = $"{startTime:h:mm:ss tt} to {endTime:h:mm:ss tt}        Zoom: {duration:c}        AHI: {apneaHypopneaIndex:F2}";

		RenderGraph( false );
	}

	public void RenderGraph( bool highQuality )
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		Chart.Configuration.Quality                 = highQuality ? QualityMode.High : QualityMode.Low;
		
		Chart.RenderRequest();
		
		Chart.Configuration.AxesChangedEventEnabled = true;
	}

	#endregion
	
	#region Private functions

	private TimeSpan GetTotalSleepTime( DateTime startTime, DateTime endTime )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		
		TimeSpan result = TimeSpan.Zero;

		var cpapSessions = _day.Sessions.Where( x => x.SourceType == SourceType.CPAP );

		foreach( var session in cpapSessions )
		{
			if( !DateHelper.RangesOverlap( session.StartTime, session.EndTime, startTime, endTime ) )
			{
				continue;
			}

			var clippedStartTime = DateHelper.Max( session.StartTime, startTime );
			var clippedEndTime   = DateHelper.Min( session.EndTime, endTime );

			if( clippedEndTime > clippedStartTime )
			{
				result += clippedEndTime - clippedStartTime;
			}
		}

		return result;
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
		if( pixelDifference <= 5 )
		{
			_selectionSpan!.IsVisible = false;
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
		
		UpdateVisibleRange( _selectionStartTime, _selectionEndTime );
		OnAxesChanged( this, EventArgs.Empty );
	}

	public void UpdateVisibleRange( double startTime, double endTime )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		UpdateVisibleRange( _day.RecordingStartTime.AddSeconds( startTime ), _day.RecordingStartTime.AddSeconds( endTime ) );
	}
	
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
		if( _day == null || !IsEnabled )
		{
			return;
		}
		
		var eventArgs = new DateTimeRangeRoutedEventArgs
		{
			RoutedEvent = GraphEvents.DisplayedRangeChangedEvent,
			Source      = this,
			StartTime   = _day.RecordingStartTime.AddSeconds( _leftOccluder!.X2 ),
			EndTime     = _day.RecordingStartTime.AddSeconds( _rightOccluder!.X1 )
		};

		RaiseEvent( eventArgs );
	}

	private void CancelSelectionMode()
	{
		_interactionMode          = GraphInteractionMode.None;
		_selectionStartTime       = 0;
		_selectionEndTime         = 0;
		_selectionSpan!.IsVisible = false;

		RenderGraph( true );
	}
	
	private void LoadData( DailyReport day )
	{
		_day = day;

		Chart.Plot.Clear();

		// If there are no Sessions (or no Signals), then indicate that there is no data available
		if( !day.HasDetailData )
		{
			_day = null;
			IndicateNoDataAvailable();

			return;
		}
		
		NoDataLabel.IsVisible  = false;
		Chart.IsEnabled        = true;
		this.IsEnabled         = true;

		var eventTypes = GetVisibleEventTypes( day );
		
		Chart.Plot.SetAxisLimitsX( 0, day.TotalTimeSpan.TotalSeconds );
		Chart.Plot.SetAxisLimitsY( -eventTypes.Count, 1 );
		Chart.Plot.YAxis.SetBoundary( -eventTypes.Count, 1 );

		var positions = new double[ eventTypes.Count ];
		var labels    = new string[ eventTypes.Count ];

		var alternateBackgroundColor = ((SolidColorBrush)ChartAlternateBackground).Color.ToDrawingColor().MultiplyAlpha( 0.5f );
		var sessionBarColor = ((SolidColorBrush)SessionBarColor).Color.ToDrawingColor();

		for( int i = 0; i < eventTypes.Count; i++ )
		{
			positions[ i ] = -i;
			labels[ i ]    = eventTypes[ i ].ToInitials();

			if( i % 2 == 0 )
			{
				Chart.Plot.AddVerticalSpan( -i - 0.5, -i + 0.5, alternateBackgroundColor );
			}
		}
				
		Chart.Plot.YAxis.ManualTickPositions( positions, labels );

		foreach( var session in day.Sessions.Where( x => x.SourceType == SourceType.CPAP ) )
		{
			var left  = (session.StartTime - day.RecordingStartTime).TotalSeconds;
			var right = (session.EndTime - day.RecordingStartTime).TotalSeconds;

			var sessionBar = Chart.Plot.AddRectangle( left, right, 0.75, 1.0 );
			
			sessionBar.BorderLineWidth = 1;
			sessionBar.BorderColor     = sessionBarColor;
			sessionBar.Color           = sessionBarColor;
		}

		foreach( var evt in day.Events )
		{
			var index = eventTypes.IndexOf( evt.Type );
			if( index != -1 )
			{
				var top    = -index - 0.5;
				var bottom = -index + 0.5;

				var bounds = evt.GetTimeBounds();
				var left   = (bounds.StartTime - day.RecordingStartTime).TotalSeconds;
				var right  = (bounds.EndTime - day.RecordingStartTime).TotalSeconds;

				// For events like Hypopnea which do not have any recorded duration, we need to provide a minimum width
				// in order for it to be drawable and visible on the graph. 
				if( right - left < 1.0 )
				{
					left  -= 1.0;
					right += 1.0;
				}

				var markerColor = MarkerConfiguration.FirstOrDefault( x => x.EventType == evt.Type )?.Color ?? Color.Red;

				var rect = Chart.Plot.AddRectangle( left, right, top, bottom );
				rect.BorderLineWidth = 1;
				rect.BorderColor     = markerColor;
				rect.Color           = markerColor;
			}
		}

		var occluderColor = Color.Gray.MultiplyAlpha( 0.25f );
		_leftOccluder  = Chart.Plot.AddHorizontalSpan( 0, 0, occluderColor );
		_rightOccluder = Chart.Plot.AddHorizontalSpan( 0, 0, occluderColor );

		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
		_selectionSpan.IgnoreAxisAuto = true;
		_selectionSpan.IsVisible      = false;

		UpdateVisibleRange( _day.RecordingStartTime, _day.RecordingEndTime );
	}

	private List<EventType> GetVisibleEventTypes( DailyReport day )
	{
		var eventTypes = EventTypes.RespiratoryDisturbance.ToList();
		eventTypes.Add( EventType.LargeLeak );

		if( day.MachineInfo.Manufacturer == MachineManufacturer.PhilipsRespironics )
		{
			eventTypes.Add( EventType.PeriodicBreathing );
			eventTypes.Add( EventType.VariableBreathing );
			eventTypes.Add( EventType.BreathingNotDetected );
			eventTypes.Add( EventType.VibratorySnore );
		}
		
		return eventTypes;
	}

	private void IndicateNoDataAvailable()
	{
		Chart.Plot.Clear();

		NoDataLabel.IsVisible  = true;
		Chart.IsEnabled        = false;
		this.IsEnabled         = false;

		// Chart.Plot.XAxis.AutomaticTickPositions();
		// Chart.Plot.YAxis.AutomaticTickPositions();

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
		
		plot.XAxis.TickLabelFormat( TickFormatter );
		plot.XAxis.MinimumTickSpacing( 1f );
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.MajorGrid( false );
		plot.XAxis.AxisTicks.MajorTickLength = 15;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis2.Layout( 8, 1, 1 );

		plot.YAxis.TickDensity( 1f );
		plot.YAxis.TickLabelFormat( x => $"{x:0.##}" );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );
		plot.YAxis.SetBoundary( 0, 10 );
		plot.YAxis.MajorGrid( false );
		plot.YAxis.MinorGrid( false );

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
			null
		);

		return (float)Math.Ceiling( formatted.Width );
	}

	public MemoryStream RenderGraphToBitmap( PixelSize renderSize )
	{
		// Ensure that the chart has a print-friendly style applied
		Chart.Plot.Style( CustomChartStyle.ChartPrintStyle );
		
		// Ensure that the chart is rendered in high quality
		var lastQualityMode = Chart.Configuration.Quality;
		Chart.Configuration.Quality = ScottPlot.Control.QualityMode.High;
		
		// Render the graph to an in-memory bitmap
		using var chartBitmap = Chart.Plot.Render( renderSize.Width, renderSize.Height, false, 4 );

		// Write the image to an in-memory stream 
		var stream = new MemoryStream();
		chartBitmap.Save( stream, ImageFormat.Jpeg );

		// Restore the previous style 
		Chart.Configuration.Quality = lastQualityMode;
		Chart.Plot.Style( _chartStyle );

		//chartBitmap.Save( $@"D:\Temp\{Guid.NewGuid().ToString()}.png", ImageFormat.Png );
		
		stream.Position = 0;

		return stream;
	}

	#endregion
}

