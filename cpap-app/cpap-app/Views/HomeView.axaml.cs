using System;

using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpap_db;

namespace cpap_app.Views;

public partial class HomeView : UserControl
{
	public HomeView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		using var db = StorageService.Connect();
		
		var profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var date      = db.GetMostRecentStoredDate( profileID );

		if( date > DateTime.Today.AddDays( -30 ) )
		{
			var day = db.LoadDailyReport( profileID, date );

			DailyGoals.DataContext = new DailyGoalsSummaryViewModel( day );
		}
		else
		{
			DailyGoals.IsVisible = false;
		}
	}

	private void BtnImportCPAP_OnClick( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new RoutedEventArgs( MainView.ImportCpapRequestedEvent ) );
	}

	private void BtnImportOximetry_Click( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new RoutedEventArgs( MainView.ImportOximetryRequestedEvent ) );
	}
}
