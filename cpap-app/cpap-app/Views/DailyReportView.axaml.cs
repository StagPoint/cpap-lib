using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyReportView : UserControl
{
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
			DateSelector.SelectedDate = _datesWithData[ ^1 ];
			DateSelector.DisplayDateStart = _datesWithData[ 0 ];
			DateSelector.DisplayDateEnd = _datesWithData[ ^1 ];
		}
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

		if( string.IsNullOrEmpty( selectedItem.Tag as string ) )
		{
			return;
		}
	
		var typeName = $"{typeof( MainView ).Namespace}.{selectedItem.Tag}View";
		var pageType = Type.GetType( typeName );
	
		if( pageType == null )
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

