using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using cpap_app.Styling;

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
	
	public string Title { get; set; }

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
	
	private CustomChartStyle    _chartStyle         = null;
	private Tooltip             _tooltip            = null;
	private VLine               _mouseTrackLine     = null;
	private MarkerPlot          _currentValueMarker = null;
	private DailyReport         _day                = null;
	private List<ReportedEvent> _events             = new();
	private bool                _hasDataAvailable   = false;

	#endregion 
	
	#region Constructor 
	
	public SignalChart()
	{
		InitializeComponent();
	}
	
	#endregion 
	
	#region Base class overrides 

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
		
		ChartLabel.Text  = Title;

		InitializeChartProperties( Chart );
	}
	
	#endregion 
	
	#region Private functions 
	
	private void InitializeChartProperties( AvaPlot chart )
	{
		_chartStyle = new CustomChartStyle( this, ChartForeground, ChartBackground, ChartBorderColor, ChartGridLineColor );
		var plot = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "88888.8", _chartStyle.TickLabelFontName, 12 );

		chart.Configuration.Quality                                      = ScottPlot.Control.QualityMode.High;
		chart.Configuration.QualityConfiguration.BenchmarkToggle         = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.AutoAxis                = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDragged = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDropped = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseWheelScrolled      = RenderType.HighQuality;

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
		
		chart.Refresh();
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

