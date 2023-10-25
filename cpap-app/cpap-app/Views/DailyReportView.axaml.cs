﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_app.Animation;
using cpap_app.Events;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;

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
		
		AddHandler( DailySpO2View.DeletionRequestedEvent, DailySpO2View_OnDeletionRequested );

		TabFrame.IsNavigationStackEnabled = false;
		TabFrame.CacheSize                = 0;
		
		TabFrame.Navigate( typeof( DailyDetailsView ), DataContext, new SuppressNavigationTransitionInfo() );
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
		
		_datesWithData = store.GetStoredDates( UserProfileStore.GetLastUserProfile().UserProfileID );

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
		if( sender is not TabStrip tabStrip )
		{ 
			return;
		}

		if( tabStrip.SelectedItem is not TabItem selectedItem )
		{
			return;
		}

		switch( selectedItem.Tag )
		{
			case System.Type pageType:
			{
				if( Activator.CreateInstance( pageType ) is not Control page )
				{
					throw new Exception( $"{pageType} is an invalid view type" );
				}

				// Set the frame's content to the new view instance
				TabFrame.Content = page;
				page.DataContext = DataContext;
			
				// Changing the item's Tag from the view type to the view instance allows us to cache the loaded view
				// so that next time the user clicks on the tab the view will have retained its state. This is preferable
				// to using the caching built into Frame.Navigate(), as we don't need a Forward/Back navigation stack.  
				selectedItem.Tag = page;
			
				// Post the animation otherwise pages that take slightly longer to load won't
				// have an animation since it will run before layout is complete
				Dispatcher.UIThread.Post( () =>
				{
					new FadeNavigationTransitionInfo().RunAnimation( page, CancellationToken.None );
				}, DispatcherPriority.Render );
				break;
			}
			case Control page:
				// Load the cached view into the frame 
				TabFrame.Content = page;

				// Post the animation otherwise pages that take slightly longer to load won't
				// have an animation since it will run before layout is complete
				Dispatcher.UIThread.Post( () =>
				{
					new FadeNavigationTransitionInfo().RunAnimation( page, CancellationToken.None );
				}, DispatcherPriority.Render );
				break;
			default:
				throw new Exception( $"Unhandled page type: {selectedItem.Tag}" );
		}
	}
	
	private void DateSelector_OnSelectedDateChanged( object? sender, SelectionChangedEventArgs e )
	{
		using var store = StorageService.Connect();

		var profileID = UserProfileStore.GetLastUserProfile().UserProfileID;

		// Keep this up-to-date. Probably unnecessary and overkill, but it's quick and not terribly wasteful.
		_datesWithData = store.GetStoredDates( profileID );

		// TODO: Implement visual indication of "no data available" to match previous viewer codebase
		var day = store.LoadDailyReport( profileID, DateSelector.SelectedDate ?? _datesWithData[ ^1 ] );

		DataContext = day;

		btnPrevDay.IsEnabled   = day != null && _datesWithData.Any( x => x < day.ReportDate.Date );
		btnNextDay.IsEnabled   = day != null && _datesWithData.Any( x => x > day.ReportDate );
		btnLastDay.IsEnabled   = _datesWithData.Count > 0;
		NoDataNotice.IsVisible = day == null;

		TabFrame.IsVisible    = (day != null);
		DetailTypes.IsVisible = (day != null);

		// DataContext won't cascade down to Frame.Content, so we need to pass it along manually
		if( TabFrame.Content is Control childView )
		{
			childView.DataContext = day;
		}
	}
	
	private void BtnLastDay_OnClick( object? sender, RoutedEventArgs e )
	{
		DateSelector.SelectedDate = _datesWithData[ ^1 ];
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

		var profileID = UserProfileStore.GetLastUserProfile().UserProfileID;

		using var connection = StorageService.Connect();
		connection.DeletePulseOximetryData( profileID, e.DateTime );
		
		DataContext = connection.LoadDailyReport( profileID, e.DateTime );
	}

	#endregion 
	
	#region Private functions 
	
	private async void ShowGotoTimeDialog()
	{
		if( DataContext is not DailyReport day )
		{
			return;
		}
		
		var input = new MaskedTextBox()
		{
			Mask = "00:00:00"
		};

		var dialog = new ContentDialog()
		{
			Title             = "Go to a specific time",
			PrimaryButtonText = "Go",
			CloseButtonText   = "Cancel",
			DefaultButton     = ContentDialogButton.Primary,
			Content           = new StackPanel()
			{
				Orientation = Orientation.Vertical,
				Children =
				{
					new TextBlock() { Text = "Enter the time (in 24-hour time format)" },
					input
				}
			},
		};

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
			
			return;
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
				$"The value '{input.Text}' is out of range",
				ButtonEnum.Ok,
				Icon.Error );

			await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

			return;
		}
			
		Charts.SelectTimeRange( dateTime - TimeSpan.FromMinutes( 2 ), dateTime + TimeSpan.FromMinutes( 2 ) );
	}

	#endregion 
}

