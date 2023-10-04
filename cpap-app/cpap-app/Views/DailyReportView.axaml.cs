using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyReportView : UserControl
{
	public static readonly RoutedEvent<TimeRangeRoutedEventArgs> TimeRangeSelectedEvent = RoutedEvent.Register<DailyReportView, TimeRangeRoutedEventArgs>( nameof( TimeRangeSelected ), RoutingStrategies.Bubble );
	public static readonly RoutedEvent<TimeRoutedEventArgs> TimeSelectedEvent = RoutedEvent.Register<DailyReportView, TimeRoutedEventArgs>( nameof( TimeSelected ), RoutingStrategies.Bubble );
	
	public event EventHandler<TimeRangeRoutedEventArgs> TimeRangeSelected
	{
		add => AddHandler( TimeRangeSelectedEvent, value );
		remove => RemoveHandler( TimeRangeSelectedEvent, value );
	}
	
	public event EventHandler<TimeRoutedEventArgs> TimeSelected
	{
		add => AddHandler( TimeSelectedEvent, value );
		remove => RemoveHandler( TimeSelectedEvent, value );
	}
	
	private List<DateTime> _datesWithData = new List<DateTime>();
	
	public DailyReportView()
	{
		InitializeComponent();
		
		TabFrame.Content = new DailyDetailsView();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		using( var store = StorageService.Connect() )
		{
			_datesWithData = store.GetStoredDates();

			// TODO: Keep DisplayDateStart/DisplayDateEnd up to date (after importing, etc.)
			DateSelector.DisplayDateStart = _datesWithData.Min();
			DateSelector.DisplayDateEnd   = _datesWithData.Max();
			DateSelector.SelectedDate     = _datesWithData.Max();
		}
	}
	
	private void OnTimeRangeSelected( object? sender, TimeRangeRoutedEventArgs e )
	{
		Charts.SelectTimeRange( e.StartTime, e.EndTime );
	}

	private void OnTimeSelected( object? sender, TimeRoutedEventArgs e )
	{
		Charts.SelectTimeRange( e.Time - TimeSpan.FromMinutes( 3 ), e.Time + TimeSpan.FromMinutes( 3 ) );
	}

	private void DetailTypes_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( sender is not TabStrip tabs )
		{ 
			return;
		}

		if( tabs.SelectedItem is not TabItem selectedItem )
		{
			return;
		}

		if( selectedItem.Tag is not System.Type pageType )
		{
			throw new Exception( $"Unhandled page type: {selectedItem.Tag}" );
		}
	
		var page = Activator.CreateInstance( pageType );
		TabFrame.Content = page;

		if( page is StyledElement view )
		{
			view.DataContext = DataContext;
		}
	}
	
	private void DateSelector_OnSelectedDateChanged( object? sender, SelectionChangedEventArgs e )
	{
		using( var store = StorageService.Connect() )
		{
			// Keep this up-to-date. Probably unnecessary and overkill, but it's quick and not terribly wasteful.
			_datesWithData = store.GetStoredDates();

			// TODO: Implement visual indication of "no data available" to match previous viewer codebase
			var day = store.LoadDailyReport( DateSelector.SelectedDate ?? store.GetMostRecentStoredDate() );

			DataContext = day;

			btnPrevDay.IsEnabled   = day != null && _datesWithData.Any( x => x < day.ReportDate.Date );
			btnNextDay.IsEnabled   = day != null && _datesWithData.Any( x => x > day.ReportDate );
			btnLastDay.IsEnabled   = _datesWithData.Count > 0;
			NoDataNotice.IsVisible = day == null;

			TabFrame.IsVisible    = (day != null);
			DetailTypes.IsVisible = (day != null);

			// I don't know why setting DataContext doesn't cascade down in Avalonia like it did in WPF, 
			// but apparently I need to handle that manually.
			if( TabFrame.Content is StyledElement childView )
			{
				childView.DataContext = day;
			}
		}
	}
	private void BtnLastDay_OnClick( object? sender, RoutedEventArgs e )
	{
		DateSelector.SelectedDate = _datesWithData[ ^1 ];
	}
	
	private void DateSelector_OnCalendarOpened( object? sender, EventArgs e )
	{
	}
	
	private void BtnPrevDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( _datesWithData.Count > 0 && DataContext is DailyReport day )
		{
			DateSelector.SelectedDate = _datesWithData.Where( x => x.Date < day.ReportDate.Date ).Max();
		}
	}
	
	private void BtnNextDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( _datesWithData.Count > 0 && DataContext is DailyReport day )
		{
			DateSelector.SelectedDate = _datesWithData.Where( x => x.Date > day.ReportDate.Date ).Min();
		}
	}
}

