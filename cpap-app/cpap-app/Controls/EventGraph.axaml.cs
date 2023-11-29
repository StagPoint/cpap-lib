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
using Avalonia.Threading;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.Styling;

using cpaplib;

using FluentAvalonia.Core;

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

	#endregion
	
	#region Public properties

	public static readonly DirectProperty<SignalChart, SignalChartConfiguration?> ChartConfigurationProperty =
		AvaloniaProperty.RegisterDirect<SignalChart, SignalChartConfiguration?>( nameof( ChartConfiguration ), o => o.ChartConfiguration );

	public SignalChartConfiguration? ChartConfiguration
	{
		get => _chartConfiguration;
		set => SetAndRaise( ChartConfigurationProperty, ref _chartConfiguration, value );
	}
	
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
	
	#endregion
	
	#region Private fields 
	
	private const double MINIMUM_TIME_WINDOW = 60;

	private SignalChartConfiguration? _chartConfiguration;
	private CustomChartStyle?         _chartStyle;
	private DailyReport?              _day              = null;
	private bool                      _chartInitialized = false;
	private bool                      _hasInputFocus    = false;

	private HSpan? _leftOccluder  = null;
	private HSpan? _rightOccluder = null;

	#endregion 
	
	#region Constructor 
	
	public EventGraph( List<EventMarkerConfiguration> markerConfiguration )
	{
		InitializeComponent();
		
		MarkerConfiguration = markerConfiguration;
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
	
	private void OnPointerMoved( object? sender, PointerEventArgs e )
	{
	}
	
	private void OnPointerReleased( object? sender, PointerReleasedEventArgs e )
	{
	}
	
	private void OnPointerPressed( object? sender, PointerPressedEventArgs e )
	{
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
			(double x, double y) = Chart.GetMouseCoordinates();

			var range  = _rightOccluder!.X1 - _leftOccluder!.X2;
			var center = _leftOccluder.X2 + range * 0.5;

			range = Math.Max( range * Math.Max( -args.Delta.Y * 0.25 + 1.0, 0.25 ), MINIMUM_TIME_WINDOW );
			var left  = Math.Max( center - range * 0.5, _leftOccluder.X1 );
			var right = Math.Min( center + range * 0.5, _rightOccluder.X2 );

			UpdateVisibleRange( _day.RecordingStartTime.AddSeconds( left ), _day.RecordingStartTime.AddSeconds( right ) );

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
		if( _day == null )
		{
			return;
		}
		
		Debug.Assert( _leftOccluder != null, nameof( _leftOccluder ) + " != null" );
		_leftOccluder.X1 = 0;
		_leftOccluder.X2 = (startTime - _day.RecordingStartTime).TotalSeconds;

		Debug.Assert( _rightOccluder != null, nameof( _rightOccluder ) + " != null" );
		_rightOccluder.X1 = (endTime - _day.RecordingStartTime).TotalSeconds;
		_rightOccluder.X2 = (_day.RecordingEndTime - _day.RecordingStartTime).TotalSeconds;

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
	}
	
	private void LoadData( DailyReport day )
	{
		_day = day;
		
		NoDataLabel.IsVisible  = false;
		Chart.IsEnabled        = true;
		this.IsEnabled         = true;

		var eventTypes = EventTypes.RespiratoryDisturbance;
		
		Chart.Plot.Clear();

		Chart.Plot.SetAxisLimitsX( 0, day.TotalTimeSpan.TotalSeconds );
		Chart.Plot.SetAxisLimitsY( -eventTypes.Length, 1 );
		Chart.Plot.YAxis.SetBoundary( -eventTypes.Length, 1 );

		var positions = new double[ eventTypes.Length ];
		var labels    = new string[ eventTypes.Length ];

		var alternateBackgroundColor = ((SolidColorBrush)ChartAlternateBackground).Color.ToDrawingColor().MultiplyAlpha( 0.5f );

		for( int i = 0; i < eventTypes.Length; i++ )
		{
			positions[ i ] = -i;
			labels[ i ]    = eventTypes[ i ].ToInitials();

			if( i % 2 == 0 )
			{
				Chart.Plot.AddVerticalSpan( -i - 0.5, -i + 0.5, alternateBackgroundColor );
			}
		}
				
		Chart.Plot.YAxis.ManualTickPositions( positions, labels );

		foreach( var evt in day.Events )
		{
			var index = eventTypes.IndexOf( evt.Type );
			if( index != -1 )
			{
				var top    = -index - 0.5;
				var bottom = -index + 0.5;
				var width  = 1;
				var time   = (evt.StartTime - day.RecordingStartTime).TotalSeconds;

				var markerColor = MarkerConfiguration.FirstOrDefault( x => x.EventType == evt.Type )?.Color ?? Color.Red;

				var rect = Chart.Plot.AddRectangle( time - width, time + width, top, bottom );
				rect.BorderLineWidth = 1;
				rect.BorderColor     = markerColor;
				rect.Color           = markerColor;
			}
		}

		var occluderColor = Color.Gray.MultiplyAlpha( 0.25f );
		_leftOccluder  = Chart.Plot.AddHorizontalSpan( 0, 0, occluderColor );
		_rightOccluder = Chart.Plot.AddHorizontalSpan( 0, 0, occluderColor );

		UpdateVisibleRange( _day.RecordingStartTime, _day.RecordingEndTime );
		
		RenderGraph( false );
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
			Brushes.Black
		);

		return (float)Math.Ceiling( formatted.Width );
	}

	#endregion
}

