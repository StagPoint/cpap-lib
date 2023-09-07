using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using cpaplib;

using ModernWpf;

using ScottPlot;
using ScottPlot.Plottable;

using Color = System.Drawing.Color;

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

		var chartStyle = new CustomChartStyle( this );
		graphBreathing.Plot.Style( chartStyle );

		graphBreathing.Plot.LeftAxis.Label( "Breathing" );
		graphBreathing.Plot.Layout( 0, 0, 0, 0 );
		
		graphBreathing.Padding                        = new Thickness( 0, 0, 0, 0 );
		graphBreathing.Margin                         = new Thickness( 0, 0, 0, 0 );
		graphBreathing.Configuration.LockVerticalAxis = true;
		
		graphBreathing.Refresh();
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

