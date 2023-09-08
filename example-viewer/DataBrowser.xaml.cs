using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using cpaplib;

using ModernWpf;

using ScottPlot;
using ScottPlot.Plottable;

using Color = System.Drawing.Color;
using Orientation = ScottPlot.Orientation;

namespace example_viewer;

public partial class DataBrowser
{
	private ResMedDataLoader _data = null;
	private string _dataPath = String.Empty;

	private DailyReport SelectedDay = null;
	
	public DataBrowser( string dataPath )
	{
		InitializeComponent();

		// Save the path for when the OnLoaded handler executes 
		_dataPath = dataPath;

		this.Loaded += OnLoaded;

		calendar.SelectedDate       = DateTime.MinValue;
		calendar.IsTodayHighlighted = false;
		calendar.SelectedDateChanged += CalendarOnSelectedDateChanged;

		scrollStatistics.Visibility = Visibility.Hidden;
		
		this.SizeChanged += OnSizeChanged;
	}
	
	private void OnLoaded( object sender, RoutedEventArgs e )
	{
		_data = new ResMedDataLoader();

		var startTime = Environment.TickCount;
		
		_data.LoadFromFolder( _dataPath );

		var elapsed = Environment.TickCount - startTime;
		Debug.WriteLine( $"Time to load CPAP data ({_data.Days.Count} days): {elapsed/1000.0f:F3} seconds" );

		// It shouldn't be possible to load this page without a valid path, but if it happened anyways
		// go back to the Welcome screen.
		if( _data.Days.Count == 0 )
		{
			NavigationService.Navigate( new WelcomeNotice() );
			NavigationService.RemoveBackEntry();
			return;
		}
			
		var selectedDay = _data.Days.LastOrDefault();
		if( selectedDay != null )
		{
			calendar.SelectedDate = selectedDay.ReportDate.Date;
		}

		initializeChart( graphBreathing, "Flow Rate" );
	}
	
	private void addSession( WpfPlot chart, DailyReport day )
	{
		chart.Plot.Clear();
		
		// var timeline  = new double[ signal.Samples.Count ];
		// var startTime = signal.StartTime;
		// for( int i = 0; i < timeline.Length; i++ )
		// {
		// 	timeline[ i ] = startTime.ToTimeCode();
		// 	startTime     = startTime.AddSeconds( signal.SampleInterval * TimeSpan.NanosecondsPerTick );
		// }

		// var scatterValues = chart.Plot.AddScatter( timeline, signal.Samples.ToArray(), Color.DodgerBlue, 2, 0, MarkerShape.none, LineStyle.Solid, "Data" );
		// scatterValues.OnNaN = ScatterPlot.NanBehavior.Gap;

		var minValue = double.MaxValue;
		var maxValue = double.MinValue;

		double offset  = 0;
		double endTime = 0;

		foreach( var session in day.Sessions )
		{
			var signal = session.Signals[ 0 ];

			minValue = Math.Min( minValue, signal.MinValue );
			maxValue = Math.Max( maxValue, signal.MaxValue );

			offset  = (signal.StartTime - day.RecordingStartTime).TotalSeconds;
			endTime = (signal.EndTime - day.RecordingStartTime).TotalSeconds;

			var graph = chart.Plot.AddSignal( signal.Samples.ToArray(), 1.0 / signal.SampleInterval, Color.DodgerBlue, signal.Name );
			
			graph.OffsetX    = offset;
			graph.MarkerSize = 0;
			graph.ScaleY     = 60;
		}

		//chart.Plot.XAxis.TickLabelFormat( x => DateTime.FromFileTime( (long)x * TimeSpan.TicksPerSecond / TimeSpan.NanosecondsPerTick ).ToString( "hh:mm:ss tt" ) );

		// Set zoom and boundary limits
		chart.Plot.YAxis.SetBoundary( minValue * 60, maxValue * 60 );
		chart.Plot.XAxis.SetBoundary( -1, day.Duration.TotalSeconds + 1 );
		chart.Plot.Margins( 0, 0.5 );
		
		// double[] positions = new[] { signal.MinValue, signal.MinValue + (signal.MaxValue - signal.MinValue) * 0.5, signal.MaxValue };
		// string[] labels    = new[] { $"{signal.MinValue}", "MED", $"{signal.MaxValue}" };
		// chart.Plot.YAxis.AutomaticTickPositions(positions, labels);

		chart.Refresh();
	}

	private void initializeChart( WpfPlot chart, string label )
	{
		var chartStyle        = new CustomChartStyle( this );
		var plot              = chart.Plot;
		var maximumLabelWidth = MeasureText( "8888.8", chartStyle.TickLabelFontName, (float)12 );
		
		chart.RightClicked -= chart.DefaultRightClickEvent;

		plot.Style( chartStyle );
		plot.LeftAxis.Label( label );
		plot.Layout( 0, 0, 0, 0 );
		
		chart.Padding                        = new Thickness( 0, 0, 0, 0 );
		chart.Margin                         = new Thickness( 0, 0, 0, 0 );
		chart.Configuration.LockVerticalAxis = true;

		// A Windows file time is a 64-bit value that represents the number of 100-nanosecond intervals
		const long TimeUnit = TimeSpan.TicksPerSecond / TimeSpan.NanosecondsPerTick * 100;
		// plot.XAxis.TickLabelFormat( x => DateTime.FromFileTime( (long)x * TimeSpan.TicksPerSecond ).ToString( "hh:mm:ss tt" ) );

		plot.XAxis.MinimumTickSpacing( 1f );
		// plot.XAxis.SetZoomInLimit( (15f * 60f) * TimeUnit ); // Smallest zoom window is 10 minutes 
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.AxisTicks.MajorTickLength = 10;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis.TickMarkDirection( outward: false );
		plot.XAxis2.Layout( 0, 1, 1 );

		//plot.YAxis.MinimumTickSpacing( 5f );
		plot.YAxis.TickDensity( 1f );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );

		// var legend = plot.Legend();
		// legend.Location     = Alignment.UpperRight;
		// legend.Orientation  = Orientation.Horizontal;
		// legend.OutlineColor = chartStyle.TickMajorColor;
		// legend.FillColor    = chartStyle.DataBackgroundColor;
		// legend.FontColor    = chartStyle.TitleFontColor;

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
			Brushes.Black,
			VisualTreeHelper.GetDpi( this ).PixelsPerDip
		);

		return (float)Math.Ceiling( formatted.Width );
	}

	private void CalendarOnSelectedDateChanged( object sender, SelectionChangedEventArgs e )
	{
		foreach( var day in _data.Days )
		{
			if( day.ReportDate.Date == calendar.SelectedDate )
			{
				LoadDay( day );
				return;
			}
		}

		SelectedDay = null;
		
		scrollStatistics.Visibility   = Visibility.Hidden;
		pnlCharts.Visibility          = Visibility.Hidden;
		pnlNoDataAvailable.Visibility = Visibility.Visible;
	}
	
	private void LoadDay( DailyReport day )
	{
		SelectedDay           = day;
		calendar.SelectedDate = day.ReportDate.Date;
		
		scrollStatistics.Visibility   = Visibility.Visible;
		pnlCharts.Visibility          = Visibility.Visible;
		pnlNoDataAvailable.Visibility = Visibility.Hidden;
		
		DataContext                         = day;
		MachineID.DataContext               = _data.MachineID;
		RespiratoryEventSummary.DataContext = day.EventSummary;
		StatisticsSummary.DataContext       = day.Statistics;
		MachineSettings.DataContext         = day.Settings;
		
		addSession( graphBreathing, day );
	}

	private void OnSizeChanged( object sender, SizeChangedEventArgs e )
	{
		var position = scrollStatistics.TransformToAncestor( this ).Transform( new Point( 0, 0 ) );
		scrollStatistics.Height = e.NewSize.Height - position.Y;
	}
	
	private void btnPrevDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( SelectedDay != null )
		{
			SelectedDay = _data.Days.LastOrDefault( x => x.ReportDate < SelectedDay.ReportDate );
		}

		if( SelectedDay == null )
		{
			SelectedDay = _data.Days.First();
		}

		LoadDay( SelectedDay );
	}
	
	private void btnNextDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( SelectedDay != null )
		{
			SelectedDay = _data.Days.FirstOrDefault( x => x.ReportDate > SelectedDay.ReportDate );
		}

		if( SelectedDay == null )
		{
			SelectedDay = _data.Days.Last();
		}
		
		LoadDay( SelectedDay );
	}
	
	public class CustomChartStyle : ScottPlot.Styles.Default
	{
		public override Color  FrameColor            { get; }
		public override Color  AxisLabelColor        { get; }
		public override Color  DataBackgroundColor   { get; }
		public override Color  FigureBackgroundColor { get; }
		public override Color  GridLineColor         { get; }
		public override Color  TickLabelColor        { get; }
		public override Color  TickMajorColor        { get; }
		public override Color  TickMinorColor        { get; }
		public override Color  TitleFontColor        { get; }
		
		public override string TickLabelFontName     { get; }
		public override string AxisLabelFontName     { get; }
		public override string TitleFontName         { get; }

		public CustomChartStyle( FrameworkElement theme )
		{
			var foreColor       = ((SolidColorBrush)theme.FindResource( "SystemControlForegroundBaseHighBrush" )).Color.ToPlotColor();
			var midColor       = ((SolidColorBrush)theme.FindResource( "SystemControlBackgroundBaseLowBrush" )).Color.ToPlotColor();
			var backgroundColor = ((SolidColorBrush)theme.FindResource( "SystemControlBackgroundAltHighBrush" )).Color.ToPlotColor();
			var fontName        = ((FontFamily)theme.FindResource( "ContentControlThemeFontFamily" )).FamilyNames.Values.First();

			FigureBackgroundColor = Color.Transparent;
			DataBackgroundColor   = backgroundColor;
			
			FrameColor     = foreColor;
			AxisLabelColor = foreColor;
			TitleFontColor = foreColor;
			TickLabelColor = foreColor;

			GridLineColor  = midColor;
			TickMajorColor = midColor;
			TickMinorColor = midColor;

			TickLabelFontName = fontName;
			AxisLabelFontName = fontName;
			TitleFontName     = fontName;
		}
		
	}
}

