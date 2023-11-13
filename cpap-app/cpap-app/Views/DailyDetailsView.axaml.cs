using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

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

	private async void ReimportCurrentDate( object? sender, RoutedEventArgs e )
	{
		// TODO: Implement "Re-import this date" functionality

		if( DataContext is not DailyReport day )
		{
			return;
		}

		var dialog = MessageBoxManager.GetMessageBoxStandard(
			$"Re-Import Data for {day.ReportDate.Date:D}",
			$"Are you sure you wish to delete all data for {day.ReportDate.Date:D} and re-import it?\n\nTHIS FUNCTIONALITY HAS NOT YET BEEN IMPLEMENTED!",
			ButtonEnum.Ok,
			Icon.Warning
		);
		
		await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
	}
}

