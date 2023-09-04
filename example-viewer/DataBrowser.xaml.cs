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

	private DailyReport SelectedDay = null;
	
	public DataBrowser( string dataPath )
	{
		InitializeComponent();

		_data = new CpapDataLoader();
		_data.LoadFromFolder( dataPath );

		// It shouldn't be possible to load this page without a valid path, but if it happened anyways
		// go back to the Welcome screen.
		if( _data.Days.Count == 0 )
		{
			NavigationService.Navigate( new WelcomeNotice() );
			NavigationService.RemoveBackEntry();
			return;
		}

		calendar.SelectedDate       = DateTime.Today;
		calendar.IsTodayHighlighted = false;
		calendar.SelectedDateChanged += CalendarOnSelectedDateChanged;

		scrollStatistics.Visibility = Visibility.Hidden;
		
		this.SizeChanged += OnSizeChanged;

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
		
		scrollStatistics.Visibility = Visibility.Hidden;
	}
	
	private void LoadDay( DailyReport day )
	{
		SelectedDay           = day;
		calendar.SelectedDate = day.ReportDate.Date;
		
		scrollStatistics.Visibility         = Visibility.Visible;
		RespiratoryEventSummary.DataContext = day.EventSummary;
		this.DataContext                    = day;
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

