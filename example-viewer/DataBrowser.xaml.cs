using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using cpaplib;

namespace example_viewer;

public partial class DataBrowser
{
	private CpapDataLoader _data = null;
	private string _dataPath = String.Empty;

	private DailyReport SelectedDay = null;
	
	public DataBrowser( string dataPath )
	{
		InitializeComponent();

		// Save the path for when the OnLoaded handler executes 
		_dataPath = dataPath;

		this.Loaded += OnLoaded;

		calendar.SelectedDate       = DateTime.Today;
		calendar.IsTodayHighlighted = false;
		calendar.SelectedDateChanged += CalendarOnSelectedDateChanged;

		scrollStatistics.Visibility = Visibility.Hidden;
		
		this.SizeChanged += OnSizeChanged;
	}
	
	private void OnLoaded( object sender, RoutedEventArgs e )
	{
		_data = new CpapDataLoader();

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
		pnlNoDataAvailable.Visibility = Visibility.Visible;
	}
	
	private void LoadDay( DailyReport day )
	{
		SelectedDay           = day;
		calendar.SelectedDate = day.ReportDate.Date;
		
		scrollStatistics.Visibility   = Visibility.Visible;
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
}

