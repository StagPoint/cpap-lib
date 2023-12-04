using System;
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
using cpap_app.ViewModels;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Control;
using ScottPlot.Plottable;

namespace cpap_app.Controls;

public partial class HistoryGraphBase : UserControl
{
	#region Styled Properties

	public static readonly StyledProperty<IBrush> ChartBackgroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<IBrush> ChartAlternateBackgroundProperty = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartAlternateBackground ) );
	public static readonly StyledProperty<IBrush> ChartGridLineColorProperty       = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<IBrush> ChartForegroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<IBrush> ChartBorderColorProperty         = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBorderColor ) );
	public static readonly StyledProperty<int>    ColorIndexProperty               = AvaloniaProperty.Register<SignalStatisticGraph, int>( nameof( ColorIndex ) );

	#endregion
	
	#region Public properties

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
	
	public int ColorIndex
	{
		get => GetValue( ColorIndexProperty );
		set => SetValue( ColorIndexProperty, value );
	}

	#endregion
	
	#region Private fields

	// Don't allow the user to zoom in to any time frame smaller than two weeks. 
	protected const int MINIMUM_TIME_WINDOW = 14;

	protected HistoryViewModel _history = new();

	protected CustomChartStyle? _chartStyle;
	protected bool              _chartInitialized = false;

	protected GraphInteractionMode _interactionMode = GraphInteractionMode.None;
	protected bool                 _hasInputFocus   = false;
	protected Point                _pointerDownPosition;
	protected AxisLimits           _pointerDownAxisLimits = AxisLimits.NoLimits;
	protected double               _selectionStartTime    = 0;
	protected double               _selectionEndTime      = 0;
	protected HSpan?               _selectionSpan         = null;

	#endregion 
	
	#region Constructor 
	
	// ReSharper disable once MemberCanBeProtected.Global
	public HistoryGraphBase()
	{
		InitializeComponent();
		
		Chart.ContextMenu = null;
		
		PointerWheelChanged += OnPointerWheelChanged;
		PointerEntered      += OnPointerEntered;
		PointerExited       += OnPointerExited;
		PointerPressed      += OnPointerPressed;
		PointerReleased     += OnPointerReleased;
		PointerMoved        += OnPointerMoved;
		GotFocus            += OnGotFocus;
		LostFocus           += OnLostFocus;
	}
	
	#endregion 
	
	#region Base class overrides 
	
	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
	
		if( !_chartInitialized )
		{
			InitializeChartProperties( Chart );
		}

		if( _history.Days.Count == 0 )
		{
			ShowNoDataAvailable();
			return;
		}

		LoadData( _history );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name != nameof( DataContext ) )
		{
			return;
		}

		if( change.NewValue is HistoryViewModel vm )
		{
			if( vm.Days.Count == 0 )
			{
				ShowNoDataAvailable();
			}
			else
			{
				LoadData( vm );
			}

			if( ToolTip.GetTip( this ) is Control tooltip )
			{
				tooltip.DataContext = null;
			}
		}
	}

	protected override void OnKeyDown( KeyEventArgs args )
	{
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
			
				UpdateVisibleRange( startTime, endTime );
				OnAxesChanged( this, EventArgs.Empty );
				
				_selectionSpan!.IsVisible = false;
				ToolTip.SetIsOpen( this, false );

				args.Handled = true;
				break;
			}
			case Key.Up or Key.Down:
			{
				double increment = ((args.KeyModifiers & KeyModifiers.Shift) != 0) ? 0.35 : 0.2;
				double amount    = (args.Key == Key.Up ? 1.0 : -1.0) * increment + 1.0;
			
				Chart.Plot.AxisZoom( amount, 1.0 );
				
				RenderGraph( false );
				OnAxesChanged( this, EventArgs.Empty );
				
				_selectionSpan!.IsVisible = false;
				ToolTip.SetIsOpen( this, false );

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
		}
	}
	
	#endregion
	
	#region Event handlers 
	
	protected void OnPointerMoved( object? sender, PointerEventArgs eventArgs )
	{
		if( DataContext == null || !IsEnabled )
		{
			return;
		}

		var mouseRelativePosition = eventArgs.GetCurrentPoint( Chart ).Position;

		(double timeOffset, _) = Chart.Plot.GetCoordinate( (float)mouseRelativePosition.X, (float)mouseRelativePosition.Y );

		// Race condition: Ignore this event when the chart is not yet fully set up
		if( double.IsNaN( timeOffset ) || _selectionSpan == null )
		{
			return;
		}
		
		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch( _interactionMode )
		{
			case GraphInteractionMode.Selecting:
			{
				// TODO: This still allows selecting areas of the Signal that are not in the graph's visible area. Leave it?
				_selectionEndTime = Math.Max( 0, Math.Min( timeOffset, _history.TotalDays ) );
				
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
				ToolTip.SetIsOpen( this, false );
			
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
					end   = Math.Min( _history.TotalDays, _pointerDownAxisLimits.XMax + panAmount );
					start = end - _pointerDownAxisLimits.XSpan;
				}
				
				UpdateVisibleRange( start, end );
				OnAxesChanged( this, EventArgs.Empty );
			
				eventArgs.Handled = true;
				ToolTip.SetIsOpen( this, false );
				
				return;
			}
			case GraphInteractionMode.None:
			{
				var point    = eventArgs.GetCurrentPoint( this );
				var dataRect = GetDataAreaBounds();
				
				if( !dataRect.Contains( point.Position ) )
				{
					return;
				}
		
				(double mousePosX, _)     = Chart.GetMouseCoordinates();

				var hoveredDayIndex = (int)(mousePosX + 0.5);
				var hoveredDate     = _history.Start.AddDays( hoveredDayIndex );

				_selectionSpan.X1        = hoveredDayIndex - 0.5;
				_selectionSpan.X2        = hoveredDayIndex + 0.5;
				_selectionSpan.IsVisible = true;
				
				RenderGraph( true );
				OnHover( point, hoveredDayIndex, hoveredDate );
				
				break;
			}
		}
	}

	protected void OnPointerReleased( object? sender, PointerReleasedEventArgs e )
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

	protected void OnPointerPressed( object? sender, PointerPressedEventArgs eventArgs )
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
				_selectionSpan!.X1       = _selectionStartTime;
				_selectionSpan!.X2       = _selectionStartTime;
				_selectionSpan.IsVisible = true;

				_interactionMode = GraphInteractionMode.Selecting;
			
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
					_pointerDownAxisLimits = Chart.Plot.GetAxisLimits();
					_interactionMode       = GraphInteractionMode.Panning;
				}
				break;
			}
		}
	}
	
	protected void OnPointerExited( object? sender, PointerEventArgs e )
	{
		ToolTip.SetIsOpen( this, false );

		if( _selectionSpan != null )
		{
			_selectionSpan.IsVisible = false;
			RenderGraph( true );
		}
	}
	
	protected void OnPointerEntered( object? sender, PointerEventArgs e )
	{
	}
	
	protected void OnPointerWheelChanged( object? sender, PointerWheelEventArgs args )
	{
		ToolTip.SetIsOpen( this, false );

		// Because the charts are likely going to be used within a scrolling container, I've disabled the built-in mouse wheel 
		// handling which performs zooming, and re-implemented it here with the additional requirement that the Control key be
		// held down while scrolling the mouse wheel in order to zoom. If the Control key is held down, the chart will zoom in
		// and out and the event will be marked Handled so that it doesn't cause scrolling in the parent container. 
		if( (args.KeyModifiers & KeyModifiers.Control) != 0x00 )
		{
			(double x, _) = Chart.GetMouseCoordinates();

			var amount = Math.Max( args.Delta.Y * 0.25 + 1.0, 0.25 );
			Chart.Plot.AxisZoom( amount, 1.0, x );
			
			// There is an "off by one" issue with Bar charts that allows zooming past the defined limits 
			// when using AxisZoom(), so we need to force update the axis limits ourselves.
			ForceUpdateAxisLimits();

			args.Handled = true;

			OnAxesChanged( this, EventArgs.Empty );
			Focus();
			
			_selectionSpan!.IsVisible = false;
		
			RenderGraph( false );
		}
	}

	protected void OnLostFocus( object? sender, RoutedEventArgs e )
	{
		FocusAdornerBorder.Classes.Remove( "FocusAdorner" );
		_hasInputFocus = false;
	}

	protected void OnGotFocus( object? sender, GotFocusEventArgs e )
	{
		FocusAdornerBorder.Classes.Add( "FocusAdorner" );
		_hasInputFocus = true;
	}

	#endregion 
	
	#region Public functions

	public void UpdateVisibleRange( DateTime start, DateTime end )
	{
		var startOffset = (start - _history.Start).TotalDays;
		var endOffset   = (end - _history.Start).TotalDays;

		UpdateVisibleRange( startOffset, endOffset );
	}
	
	public void UpdateVisibleRange( double start, double end )
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		{
			Chart.Plot.SetAxisLimitsX( start, end );
			RenderGraph( false );
		}
		Chart.Configuration.AxesChangedEventEnabled = true;
	}
	
	public void RenderGraph( bool highQuality )
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		Chart.Configuration.Quality                 = highQuality ? QualityMode.High : QualityMode.Low;
		
		Chart.RenderRequest();
		
		Chart.Configuration.AxesChangedEventEnabled = true;
	}

	#endregion
	
	#region Virtual functions

	protected virtual void OnHover( PointerPoint mousePosition, int hoveredDayIndex, DateTime hoveredDate )
	{
		const int HORZ_OFFSET = 24;
		const int VERT_OFFSET = 12;

		var day = _history.Days.FirstOrDefault( x => x.ReportDate.Date == hoveredDate );
		
		var tooltip = ToolTip.GetTip( this ) as ToolTip;
		Debug.Assert( tooltip != null, nameof( tooltip ) + " != null" );

		tooltip.DataContext = BuildTooltipDataContext( day ?? new DailyReport() { ReportDate = hoveredDate } );
		if( tooltip.DataContext == null )
		{
			ToolTip.SetIsOpen( this, false );
			return;
		}

		tooltip.Measure( Bounds.Size );

		var axisLimits       = Chart.Plot.GetAxisLimits();
		var onLeftSide       = hoveredDayIndex < axisLimits.XCenter;
		var tooltipWidth     = tooltip.Bounds.Width;
		var tooltipPositionX = !onLeftSide ? mousePosition.Position.X - HORZ_OFFSET : mousePosition.Position.X + HORZ_OFFSET + tooltipWidth;
		var tooltipPositionY = mousePosition.Position.Y - tooltip.Bounds.Height + VERT_OFFSET;
		
		tooltip.InvalidateVisual();
		ToolTip.SetIsOpen( this, true );
		ToolTip.SetPlacement( this, PlacementMode.LeftEdgeAlignedTop );
		ToolTip.SetHorizontalOffset( this, tooltipPositionX );
		ToolTip.SetVerticalOffset( this, tooltipPositionY ); 
	}
	
	protected virtual object? BuildTooltipDataContext( DailyReport day )
	{
		return null;
	}
	
	protected virtual void LoadData( HistoryViewModel viewModel )
	{
		
	}

	protected virtual void InitializeChartProperties( AvaPlot chart )
	{
		_chartInitialized = true;
		_chartStyle       = new CustomChartStyle( ChartForeground, ChartBackground, ChartBorderColor, ChartGridLineColor );
		
		var plot = chart.Plot;
		
		// ReSharper disable once StringLiteralTypo
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "XXXXXXX", _chartStyle.TickLabelFontName, 12 );

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
		plot.XAxis.TickDensity( 2f );
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.MajorGrid( false );
		plot.XAxis.AxisTicks.MajorTickLength = 4;
		plot.XAxis.AxisTicks.MinorTickLength = 0;
		plot.XAxis2.Layout( 8, 1, 1 );
		plot.XAxis.SetZoomInLimit( MINIMUM_TIME_WINDOW );

		plot.YAxis.AxisTicks.MinorTickLength = 4;
		plot.YAxis.AxisTicks.MajorTickLength = 4;
		plot.YAxis.TickDensity( 1f );
		plot.YAxis.TickLabelFormat( x => $"{x:0.##}" );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );
		plot.YAxis.SetBoundary( 0, 10 );
		plot.YAxis.MajorGrid( true );
		plot.YAxis.MinorGrid( false );
		
		chart.Configuration.LockVerticalAxis = true;

		// These changes won't be valid until the graph is rendered, so just render it in low resolution for now
		RenderGraph( false );
	}

	#endregion 

	#region Private functions 
	
	protected void ShowNoDataAvailable()
	{
		Chart.Plot.Clear();
		
		NoDataLabel.IsVisible = true;
		Chart.IsEnabled       = false;
		this.IsEnabled        = false;
	}

	private void ForceUpdateAxisLimits()
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		{
			var axisLimits = Chart.Plot.GetAxisLimits();
			Chart.Plot.SetAxisLimitsX( axisLimits.XMin, axisLimits.XMax );
		}
		Chart.Configuration.AxesChangedEventEnabled = true;
	}
	
	protected void EndSelectionMode()
	{
		// Sanity check
		if( DataContext == null )
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
		if( pixelDifference <= 5 || Math.Abs( _selectionEndTime - _selectionStartTime ) <= 1.0 )
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

	protected Rect GetDataAreaBounds()
	{
		var chartBounds = Chart.Bounds;
		var xDims       = Chart.Plot.XAxis.Dims;
		var yDims       = Chart.Plot.YAxis.Dims;

		double tickLengthY = Chart.Plot.YAxis.AxisTicks.MajorTickLength;
		
		var rect = new Rect(
			(int)(chartBounds.X + xDims.DataOffsetPx + tickLengthY ),
			(int)(chartBounds.Y + yDims.DataOffsetPx),
			(int)xDims.DataSizePx - tickLengthY, 
			(int)yDims.DataSizePx
		);

		return rect;
	}
	
	protected void OnAxesChanged( object? sender, EventArgs e )
	{
		if( DataContext == null || !IsEnabled )
		{
			return;
		}

		var limits = Chart.Plot.GetAxisLimits();
		
		var eventArgs = new DateTimeRangeRoutedEventArgs
		{
			RoutedEvent = GraphEvents.DisplayedRangeChangedEvent,
			Source      = this,
			StartTime   = _history.Start.AddDays( limits.XMin ),
			EndTime     = _history.Start.AddDays( limits.XMax ),
		};
		
		RaiseEvent( eventArgs );
	}
	
	protected void CancelSelectionMode()
	{
		_interactionMode          = GraphInteractionMode.None;
		_selectionStartTime       = 0;
		_selectionEndTime         = 0;
		_selectionSpan!.IsVisible = false;

		ToolTip.SetIsOpen( this, false );

		RenderGraph( true );
	}
	
	protected static float MeasureText( string text, string fontFamily, float emSize )
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

	protected string TickFormatter( double time )
	{
		var date = _history.Start.AddDays( Math.Round( time ) );
		return $"{date:d}";
	}

	#endregion 
}

