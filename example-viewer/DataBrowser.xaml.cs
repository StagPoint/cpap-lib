using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using cpap_db;

using cpaplib;

using cpapviewer.Controls;
using cpapviewer.Helpers;

namespace cpapviewer;

public partial class DataBrowser
{
	private string _dataPath = String.Empty;

	private DailyReport       _selectedDay = null;
	private List<DailyReport> _days        = null;

	public DataBrowser( string dataPath )
	{
		InitializeComponent();

		// Save the path for when the OnPageLoaded handler executes 
		_dataPath = dataPath;

		this.Loaded += OnPageLoaded;

		calendar.SelectedDate       = DateTime.MinValue;
		calendar.IsTodayHighlighted = false;
		calendar.SelectedDateChanged += CalendarOnSelectedDateChanged;

		scrollStatistics.Visibility = Visibility.Hidden;
		
		this.SizeChanged += OnSizeChanged;
	}
	
	private void OnPageLoaded( object sender, RoutedEventArgs e )
	{
		var startTime = Environment.TickCount;

		DateTime mostRecentDay = DateTime.Today.AddDays( -30 );

		// DEBUG: Get rid of this
		const string databasePath = @"D:\Temp\CPAP.db";
		File.Delete( databasePath );

		using( var db = new StorageService( databasePath ) )
		{
			db.Connection.BeginTransaction();

			try
			{
				//mostRecentDay = db.GetMostRecentDay();

				var loader = new ResMedDataLoader();
				_days = loader.LoadFromFolder( _dataPath, mostRecentDay );

				foreach( var day in _days )
				{
					db.SaveDailyReport( day );
				}
				
				db.Connection.Commit();
			}
			catch( Exception err )
			{
				db.Connection.Rollback();
			}
		}

		var elapsed = Environment.TickCount - startTime;
		Debug.WriteLine( $"Time to load CPAP data ({_days.Count} days): {elapsed/1000.0f:F3} seconds" );

		// It shouldn't be possible to load this page without a valid path, but if it happened anyways
		// go back to the Welcome screen.
		if( _days.Count == 0 )
		{
			NavigationService!.Navigate( new WelcomeNotice() );
			NavigationService.RemoveBackEntry();
			return;
		}
			
		var selectedDay = _days.LastOrDefault();
		if( selectedDay != null )
		{
			calendar.SelectedDate = selectedDay.ReportDate.Date;
		}
		
		scrollGraphs.PreviewMouseWheel += ScrollGraphsOnPreviewMouseWheel;
	}
	
	private void CalendarOnSelectedDateChanged( object sender, SelectionChangedEventArgs e )
	{
		foreach( var day in _days )
		{
			if( day.ReportDate.Date == calendar.SelectedDate )
			{
				LoadDay( day );
				return;
			}
		}

		_selectedDay = null;
		
		scrollStatistics.Visibility   = Visibility.Hidden;
		pnlCharts.Visibility          = Visibility.Hidden;
		pnlNoDataAvailable.Visibility = Visibility.Visible;
	}
	
	private void LoadDay( DailyReport day )
	{
		_selectedDay = day;
		
		scrollStatistics.Visibility   = Visibility.Visible;
		pnlCharts.Visibility          = Visibility.Visible;
		pnlNoDataAvailable.Visibility = Visibility.Hidden;

		DataContext = day;
	}

	private void ScrollGraphsOnPreviewMouseWheel( object sender, MouseWheelEventArgs e )
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		bool         isShiftDown  = Keyboard.IsKeyDown( Key.LeftShift ) || Keyboard.IsKeyDown( Key.RightShift );
		bool         isControlDown  = Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl );

		if( isShiftDown )
		{
			// Apparently scrolling to the current vertical offset causes the scroll panel to consider the job done,
			// and the event gets passed down to the chart which will then zoom in or out. 
			scrollViewer.ScrollToVerticalOffset( scrollViewer.VerticalOffset );
		}
		else 
		{
			// manually scroll the window then mark the event as handled so any chart under the mouse does not zoom
			double scrollOffset = scrollViewer.VerticalOffset - (e.Delta * .25);
			scrollViewer.ScrollToVerticalOffset(scrollOffset);
			
			e.Handled = true;
		}
	}
	
	private void OnSizeChanged( object sender, SizeChangedEventArgs e )
	{
		// This may well be one of the stupidest and most frustrating things about WPF :/
		var height = e.NewSize.Height - 80;

		scrollStatistics.Height = height;
		scrollEvents.Height = height;	
	}
	
	private void btnPrevDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( _selectedDay != null )
		{
			_selectedDay = _days.LastOrDefault( x => x.ReportDate < _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _days.First();
		}

		calendar.SelectedDate = _selectedDay.ReportDate.Date;
		//LoadDay( _selectedDay );
	}
	
	private void btnNextDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( _selectedDay != null )
		{
			_selectedDay = _days.FirstOrDefault( x => x.ReportDate > _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _days.Last();
		}
		
		calendar.SelectedDate = _selectedDay.ReportDate.Date;
		//LoadDay( _selectedDay );
	}
	
	private void EventTree_OnOnTimeSelected( object sender, DateTime time )
	{
		var startTime = time.AddMinutes( -1.5 );
		var endTime   = time.AddMinutes( 1.5 );
	
		// Only need to zoom to the indicated time on the first chart, as it will synchronize all of the others
		var chart = this.FindVisualChildren<SignalChart>().FirstOrDefault();
		chart.ZoomToTime( startTime, endTime );
	}
	
	private void SessionList_OnOnTimeSelected( object sender, DateTime startTime, DateTime endTime )
	{
		// Only need to zoom to the indicated time on the first chart, as it will synchronize all of the others
		var chart = this.FindVisualChildren<SignalChart>().FirstOrDefault();
		chart.ZoomToTime( startTime, endTime );
	}
	
	private void OximetrySummary_OnDailyReportModified( object sender, DailyReport day )
	{
		DataContext = null;
		LoadDay( day );
	}
}

