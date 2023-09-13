using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using cpaplib;

using example_viewer.Controls;
using example_viewer.Helpers;

namespace example_viewer;

public partial class DataBrowser
{
	private ResMedDataLoader _data = null;
	private string _dataPath = String.Empty;

	private DailyReport _selectedDay = null;

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
		
		_data = new ResMedDataLoader();
		_data.LoadFromFolder( _dataPath );

		var elapsed = Environment.TickCount - startTime;
		Debug.WriteLine( $"Time to load CPAP data ({_data.Days.Count} days): {elapsed/1000.0f:F3} seconds" );

		// It shouldn't be possible to load this page without a valid path, but if it happened anyways
		// go back to the Welcome screen.
		if( _data.Days.Count == 0 )
		{
			NavigationService!.Navigate( new WelcomeNotice() );
			NavigationService.RemoveBackEntry();
			return;
		}
			
		var selectedDay = _data.Days.LastOrDefault();
		if( selectedDay != null )
		{
			calendar.SelectedDate = selectedDay.ReportDate.Date;
		}
		
		scrollGraphs.PreviewMouseWheel += ScrollGraphsOnPreviewMouseWheel;
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
		
		DataContext                         = day;
		MachineID.DataContext               = _data.MachineID;
		RespiratoryEventSummary.DataContext = day.EventSummary;
		StatisticsSummary.DataContext       = day.Statistics;
		MachineSettings.DataContext         = day.Settings;

		SessionList.Children.Clear();
		SessionList.RowDefinitions.Clear();
		
		foreach( var session in day.Sessions )
		{
			AddToSessionList( session );
		}
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

	private void AddToSessionList( MaskSession session )
	{
		var row = new RowDefinition();
		SessionList.RowDefinitions.Add( row );
		int rowIndex = SessionList.RowDefinitions.Count - 1;

		var text = new TextBlock() { Text = $"{session.StartTime:d}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 0 );

		text           =  new TextBlock() { Text = $"{session.StartTime:t} - {session.EndTime:t}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 2 );

		text           =  new TextBlock() { Text = $"{session.Duration:g}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 4 );
	}

	private void SessionsList_RowOnMouseDown( object sender, MouseButtonEventArgs e )
	{
		var session   = (MaskSession)((TextBlock)sender).Tag;
		var startTime = (session.StartTime - _selectedDay.RecordingStartTime).TotalSeconds;
		var endTime   = (session.EndTime - _selectedDay.RecordingStartTime).TotalSeconds;

		// TODO: Shouldn't be accessing the SignalChart.Chart property directly
		// Note: Only need to zoom the first chart found, any chart, and it will handle synchronization with the rest. 
		var graph = this.FindVisualChildren<SignalChart>().FirstOrDefault();
		if( graph != null )
		{
			graph.Chart.Plot.SetAxisLimitsX( startTime, endTime);
			graph.Chart.Refresh();
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
			_selectedDay = _data.Days.LastOrDefault( x => x.ReportDate < _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _data.Days.First();
		}

		LoadDay( _selectedDay );
	}
	
	private void btnNextDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( _selectedDay != null )
		{
			_selectedDay = _data.Days.FirstOrDefault( x => x.ReportDate > _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _data.Days.Last();
		}
		
		LoadDay( _selectedDay );
	}
	
	private void EventTree_OnOnTimeSelected( object sender, DateTime time )
	{
		var startTime = time.AddMinutes( -5 );
		var endTime   = time.AddMinutes( 5 );
	
		var chart = this.FindVisualChildren<SignalChart>().FirstOrDefault();
		chart.ZoomToTime( startTime, endTime );
	}
}

