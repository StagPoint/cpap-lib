using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class DailyDetailsView : UserControl
{
	public DailyDetailsView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			SummaryDataOnly.IsVisible = change.NewValue is null or DailyReport { HasDetailData: false };
		}
	}

	private async void DeleteCurrentDate( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReportViewModel day )
		{
			return;
		}

		var dialog = MessageBoxManager.GetMessageBoxStandard(
			$"Delete Data for {day.ReportDate.Date:D}",
			$"Are you sure you wish to delete all data for {day.ReportDate.Date:D}?\n\nThis cannot be undone. Proceed with extreme caution.",
			ButtonEnum.YesNo,
			Icon.Warning
		);
		
		var dialogresult = await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		if( dialogresult != ButtonResult.Yes )
		{
			return;
		}

		using var db = StorageService.Connect();
		db.Delete( (DailyReport)day, day.ID );

		var mostRecentStoredDate = db.GetMostRecentStoredDate( day.UserProfile.UserProfileID );
		var args = new DateTimeRoutedEventArgs
		{
			RoutedEvent = MainView.LoadDateRequestedEvent,
			Source      = this,
			DateTime    = mostRecentStoredDate
		};

		RaiseEvent( args );
	}
	
	private async void ReimportCurrentDate( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReportViewModel day )
		{
			return;
		}

		var dialog = MessageBoxManager.GetMessageBoxStandard(
			$"Re-Import Data for {day.ReportDate.Date:D}",
			$"Are you sure you wish to delete all data for {day.ReportDate.Date:D} and re-import it?\n\nIf re-import does not succeed, this data could be lost forever.\n\nProceed with extreme caution.",
			ButtonEnum.YesNo,
			Icon.Warning
		);
		
		var dialogresult = await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		if( dialogresult != ButtonResult.Yes )
		{
			return;
		}

		using var db = StorageService.Connect();
		db.Delete( (DailyReport)day, day.ID );

		var args = new MainView.ImportRequestEventArgs( MainView.ImportCpapRequestedEvent )
		{
			Source           = this,
			StartDate        = day.ReportDate.Date,
			EndDate          = day.ReportDate.Date,
			OnImportComplete = () => { day.Reload(); }
		};

		RaiseEvent( args );
	}
}

