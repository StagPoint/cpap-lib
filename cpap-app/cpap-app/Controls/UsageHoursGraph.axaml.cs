using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using cpap_app.Styling;
using cpap_app.ViewModels;

using cpap_db;

using ScottPlot.Avalonia;
using ScottPlot.Control;

namespace cpap_app.Controls;

public partial class UsageHoursGraph : UserControl
{
	#region Styled Properties

	public static readonly StyledProperty<IBrush> ChartBackgroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBackground ) );
	public static readonly StyledProperty<IBrush> ChartAlternateBackgroundProperty = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartAlternateBackground ) );
	public static readonly StyledProperty<IBrush> ChartGridLineColorProperty       = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartGridLineColor ) );
	public static readonly StyledProperty<IBrush> ChartForegroundProperty          = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartForeground ) );
	public static readonly StyledProperty<IBrush> ChartBorderColorProperty         = AvaloniaProperty.Register<SignalChart, IBrush>( nameof( ChartBorderColor ) );

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
	
	#endregion
	
	#region Private fields

	private HistoryViewModel _history = new();

	private CustomChartStyle? _chartStyle;
	private bool              _chartInitialized = false;
	
	#endregion 
	
	#region Constructor 
	
	public UsageHoursGraph()
	{
		InitializeComponent();
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
			LoadData( vm );
		}
	}

	#endregion
	
	#region Public functions 
	
	public void RenderGraph( bool highQuality )
	{
		Chart.Configuration.AxesChangedEventEnabled = false;
		Chart.Configuration.Quality                 = highQuality ? QualityMode.High : QualityMode.Low;
		
		Chart.RenderRequest();
		
		Chart.Configuration.AxesChangedEventEnabled = true;
	}

	#endregion
	
	#region Private functions

	private void LoadData( HistoryViewModel viewModel )
	{
		_history = viewModel;
		
		NoDataLabel.IsVisible = false;
		Chart.IsEnabled       = true;
		this.IsEnabled        = true;

		Chart.Plot.Clear();

		var totalDays = (int)Math.Ceiling( (viewModel.End.Date - viewModel.Start.Date).TotalDays );

		Chart.Plot.SetAxisLimitsX( 0, totalDays );
		Chart.Plot.SetAxisLimitsY( 0, 12 );
		Chart.Plot.XAxis.SetBoundary( 0, totalDays );
		Chart.Plot.YAxis.SetBoundary( 0, 12 );

		using var store = StorageService.Connect();

		var days = viewModel.Days;

		var values = new double[ totalDays ];

		foreach( var day in days )
		{
			int index = (int)(day.RecordingStartTime.Date - viewModel.Start.Date).TotalDays;
			if( index < 0 || index >= totalDays )
			{
				continue;
			}
			
			values[ index ] = day.TotalSleepTime.TotalHours;
		}

		var barChart = Chart.Plot.AddBar( values );
		barChart.PositionOffset = 0.5;
		barChart.BarWidth       = 0.8;

		var positions = new double[] { 0, 3, 6, 9, 12 };
		var labels    = new string[] { "0", "3", "6", "9", "12" };

		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		
		RenderGraph( true );
	}
	
	private void InitializeChartProperties( AvaPlot chart )
	{
		_chartInitialized = true;
		_chartStyle       = new CustomChartStyle( ChartForeground, ChartBackground, ChartBorderColor, ChartGridLineColor );
		
		var plot = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "888", _chartStyle.TickLabelFontName, 12 );

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
		plot.XAxis.AxisTicks.MajorTickLength = 7;
		plot.XAxis.AxisTicks.MinorTickLength = 1;
		plot.XAxis2.Layout( 8, 1, 1 );

		plot.YAxis.AxisTicks.MinorTickLength = 5;
		plot.YAxis.AxisTicks.MajorTickLength = 5;
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

	private string TickFormatter( double time )
	{
		// TODO: Figure out specifically how to format DateTime from System.Double here
		var date = _history.Start.AddDays( time );

		return $"{date:d}";
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

