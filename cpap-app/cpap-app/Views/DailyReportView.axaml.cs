using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_app.Events;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class DailyReportView : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<ReportedEventTypeArgs> ReportedEventTypeSelectedEvent =
		RoutedEvent.Register<DailyReportView, ReportedEventTypeArgs>( nameof( ReportedEventTypeSelected ), RoutingStrategies.Bubble );

	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> TimeRangeSelectedEvent =
		RoutedEvent.Register<DailyReportView, DateTimeRangeRoutedEventArgs>( nameof( TimeRangeSelected ), RoutingStrategies.Bubble );

	public static readonly RoutedEvent<DateTimeRoutedEventArgs> TimeSelectedEvent =
		RoutedEvent.Register<DailyReportView, DateTimeRoutedEventArgs>( nameof( TimeSelected ), RoutingStrategies.Bubble );
	
	public event EventHandler<ReportedEventTypeArgs> ReportedEventTypeSelected
	{
		add => AddHandler( ReportedEventTypeSelectedEvent, value );
		remove => RemoveHandler( ReportedEventTypeSelectedEvent, value );
	}
	
	public event EventHandler<DateTimeRangeRoutedEventArgs> TimeRangeSelected
	{
		add => AddHandler( TimeRangeSelectedEvent, value );
		remove => RemoveHandler( TimeRangeSelectedEvent, value );
	}
	
	public event EventHandler<DateTimeRoutedEventArgs> TimeSelected
	{
		add => AddHandler( TimeSelectedEvent, value );
		remove => RemoveHandler( TimeSelectedEvent, value );
	}
	
	#endregion 
	
	#region Private fields 
	
	private List<DateTime> _datesWithData = new List<DateTime>();
	
	#endregion 
	
	#region Constructor 
	
	public DailyReportView()
	{
		InitializeComponent();
		
		TabFrame.Content = new DailyDetailsView();
		
		AddHandler( DailySpO2View.DeletionRequestedEvent, DailySpO2View_OnDeletionRequested );
	}
	
	#endregion 
	
	#region Base class overrides

	protected override void OnKeyDown( KeyEventArgs e )
	{
		if( e.Handled )
		{
			return;
		}
		
		if( e.Key == Key.G && (e.KeyModifiers & KeyModifiers.Control) != 0 )
		{
			e.Handled = true;
			ShowGotoTimeDialog();
		}
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( TabFrame.Content is Control control )
			{
				control.DataContext = change.NewValue;
			}
		}
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		using var store = StorageService.Connect();
		
		_datesWithData = store.GetStoredDates();

		// TODO: Keep DisplayDateStart/DisplayDateEnd up to date (after importing, etc.)
		if( _datesWithData.Count == 0 )
		{
			DateSelector.DisplayDateStart = DateTime.Today;
			DateSelector.DisplayDateEnd   = DateTime.Today;
			DateSelector.SelectedDate     = DateTime.Today;

			DateSelector.IsEnabled = false;
		}
		else
		{
			DateSelector.DisplayDateStart = _datesWithData.Min();
			DateSelector.DisplayDateEnd   = _datesWithData.Max();
			DateSelector.SelectedDate     = _datesWithData.Max();

			DateSelector.IsEnabled = true;
		}
	}
	
	#endregion 
	
	#region Event handlers 

	private void OnTimeRangeSelected( object? sender, DateTimeRangeRoutedEventArgs e )
	{
		Charts.SelectTimeRange( e.StartTime, e.EndTime );
	}

	private void OnTimeSelected( object? sender, DateTimeRoutedEventArgs e )
	{
		Charts.SelectTimeRange( e.DateTime - TimeSpan.FromMinutes( 3 ), e.DateTime + TimeSpan.FromMinutes( 3 ) );
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
	
	private void DailyReportView_OnReportedEventTypeSelected( object? sender, ReportedEventTypeArgs eventArgs )
	{
		DetailTypes.SelectedItem = TabEvents;
		
		if( TabFrame.Content is DailyEventsListView view )
		{
			view.SelectedEventType = eventArgs.Type;
		}
		
		Charts.ShowEventType( eventArgs.Type );
	}
	
	private async void DailySpO2View_OnDeletionRequested( object? sender, DateTimeRoutedEventArgs e )
	{
		var dialog = MessageBoxManager.GetMessageBoxStandard(
			"Delete Pulse Oximetry Data",
			$"Are you sure you wish to delete pulse oximetry data for {e.DateTime:D}?",
			ButtonEnum.YesNo,
			Icon.Warning
		);
		
		var result = await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

		if( result != ButtonResult.Yes )
		{
			return;
		}

		using var connection = StorageService.Connect();
		connection.DeletePulseOximetryData( e.DateTime );
		
		DataContext = connection.LoadDailyReport( e.DateTime );
	}

	#endregion 
	
	#region Private functions 
	
	private async void ShowGotoTimeDialog()
	{
		if( DataContext is not DailyReport day )
		{
			return;
		}
		
		var dialog = new ContentDialog()
		{
			Title             = "Go to a specific time",
			PrimaryButtonText = "Ok",
			CloseButtonText   = "Cancel",
			DefaultButton     = ContentDialogButton.Primary,
		};

		var input = new TextBox()
		{
			Watermark = "hh:mm:ss (24-hour time)",
		};

		dialog.Content = input;

		var task = dialog.ShowAsync();
		Dispatcher.UIThread.Post( () =>
		{
			input.Focus();
		}, DispatcherPriority.Loaded );

		var result = await task;

		if( string.IsNullOrEmpty( input.Text ) )
		{
			return;
		}

		if( result != ContentDialogResult.Primary )
		{
			return;
		}
		
		if( !TimeSpan.TryParse( input.Text, out TimeSpan time ) )
		{
			var msgBox = MessageBoxManager.GetMessageBoxStandard(
				"Go to a specific time",
				$"The value '{input.Text}' is not a valid time code",
				ButtonEnum.Ok,
				Icon.Error );

			await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}

		var dateTime = day.RecordingStartTime;

		if( time <= day.RecordingEndTime.TimeOfDay )
		{
			dateTime = day.RecordingEndTime.Date + time;
		}
		else if( time >= day.RecordingStartTime.TimeOfDay )
		{
			dateTime = day.RecordingStartTime.Date + time;
		}
		else if( time.Add( TimeSpan.FromHours( 12 ) ) >= day.RecordingStartTime.TimeOfDay )
		{
			// Even though we told the user to use 24-hour time, see if it's possible to fix it for them anyways. 
			// Worse case scenario should be that they go to the wrong time, and have to learn to use 24-hour time as directed ;)
			dateTime = day.RecordingStartTime.Date + time + TimeSpan.FromHours( 12 );
		}
		else
		{
			var msgBox = MessageBoxManager.GetMessageBoxStandard(
				"Go to a specific time",
				$"The value '{input.Text}' is is out of range",
				ButtonEnum.Ok,
				Icon.Error );

			await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

			return;
		}
			
		Charts.SelectTimeRange( dateTime - TimeSpan.FromMinutes( 3 ), dateTime + TimeSpan.FromMinutes( 3 ) );
	}

	#endregion 
}

