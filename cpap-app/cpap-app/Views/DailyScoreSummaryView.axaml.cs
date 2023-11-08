using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyScoreSummaryView : UserControl
{
	#region Constructor 
	
	public DailyScoreSummaryView()
	{
		InitializeComponent();
	}
	
	#endregion 
	
	#region Base class overrides 

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		LoadLastAvailableDate();
	}
	
	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is DailyScoreSummaryViewModel vm )
			{
				using var db        = StorageService.Connect();
				var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
				var       dates     = db.GetStoredDates( profileID );

				btnPrevDay.IsEnabled = dates.Any( x => x < vm.Date );
				btnNextDay.IsEnabled = dates.Any( x => x > vm.Date );
			}
		}
	}
	
	#endregion 
	
	#region Event handlers 
	private void DisplayedDate_OnPointerPressed( object? sender, PointerPressedEventArgs e )
	{
		if( DataContext is not DailyScoreSummaryViewModel vm )
		{
			return;
		}

		RaiseEvent( new DateTimeRoutedEventArgs
		{
			DateTime    = vm.Date,
			RoutedEvent = MainView.LoadDateRequestedEvent,
			Source      = this,
		} );
	}
	
	private void BtnPrevDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyScoreSummaryViewModel vm )
		{
			return;
		}
		
		using var db        = StorageService.Connect();
		var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var       dates     = db.GetStoredDates( profileID );

		var prevDate = dates.Where( x => x < vm.Date ).Max();

		LoadDate( db, profileID, prevDate, dates );
	}
	
	private void BtnNextDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyScoreSummaryViewModel vm )
		{
			return;
		}
		
		using var db        = StorageService.Connect();
		var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var       dates     = db.GetStoredDates( profileID );

		var nextDate = dates.Where( x => x > vm.Date ).Min();

		LoadDate( db, profileID, nextDate, dates );
	}
	
	#endregion 
	
	#region Public functions 
	
	public void LoadLastAvailableDate()
	{
		using var db = StorageService.Connect();

		// TODO: ProfileID should be retrieved from a central service
		var profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dates     = db.GetStoredDates( profileID );

		if( dates is { Count: > 0 } )
		{
			var date = dates[ ^1 ];
			LoadDate( db, profileID, date, dates );

			IsVisible = true;
		}
		else
		{
			IsVisible = false;
		}
	}
	
	#endregion 
	
	#region Private functions 

	private void LoadDate( StorageService db, int profileID, DateTime date, List<DateTime> dates )
	{
		var day      = db.LoadDailyReport( profileID, date );
		var prevDate = dates.LastOrDefault( x => x < date );

		DailyReport? previousDay = (prevDate > DateTime.MinValue) ? db.LoadDailyReport( profileID, prevDate ) : null;

		DataContext = new DailyScoreSummaryViewModel( day, previousDay );
	}

	#endregion
}

