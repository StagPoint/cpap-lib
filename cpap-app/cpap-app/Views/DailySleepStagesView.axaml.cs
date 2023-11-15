using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class DailySleepStagesView : UserControl
{
	public DailySleepStagesView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name != nameof( DataContext ) )
		{
			return;
		}
		
		StagesSummary.DataContext = new SleepStagesViewModel();
	}
	
	private async void AddNew_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReport day )
		{
			return;
		}

		var viewModel = new SleepStagePeriodViewModel
		{
			Stage     = SleepStage.Awake,
			StartDate = day.RecordingStartTime.Date,
			EndDate   = day.RecordingEndTime.Date,
			StartTime = day.RecordingStartTime,
			EndTime   = day.RecordingEndTime,
		};
		
		var view = new AddSleepStageView()
		{
			DataContext = viewModel
		};

		var dialog = new ContentDialog()
		{
			Title             = "Add a Sleep Stage period",
			PrimaryButtonText = "Add",
			CloseButtonText   = "Cancel",
			DefaultButton     = ContentDialogButton.Primary,
			Content           = view,
		};

		viewModel.ValidationStatusChanged += ( o, isValid ) => dialog.IsPrimaryButtonEnabled = isValid; 

		var task = dialog.ShowAsync( TopLevel.GetTopLevel( this ) );

		// Dispatcher.UIThread.Post( () =>
		// {
		// 	dialog.Content.Focus();
		// }, DispatcherPriority.Loaded );

		var result = await task;

		if( result == ContentDialogResult.Primary )
		{
			if( viewModel.ValidationErrors.Count > 0 )
			{
				var msgBox = MessageBoxManager.GetMessageBoxStandard(
					"Data Validation Error",
					string.Join( "\r\n", viewModel.ValidationErrors ),
					ButtonEnum.Ok,
					Icon.Error );

				await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
			}
		}
	}
}

