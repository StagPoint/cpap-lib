using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
	private SleepStagesViewModel? _sleepStages;
	
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
		
		_sleepStages = new SleepStagesViewModel();

		Container.DataContext = _sleepStages;
	}
	
	private async void AddNew_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );
		
		if( DataContext is not DailyReport day )
		{
			return;
		}

		var viewModel = new SleepStagePeriodViewModel
		{
			Stage     = SleepStage.Awake,
			EndDate   = day.RecordingEndTime.Date,
			EndTime   = day.RecordingEndTime,
		};

		if( _sleepStages.Periods.Count == 0 )
		{
			viewModel.StartDate = day.RecordingStartTime.Date;
			viewModel.StartTime = day.RecordingStartTime;
		}
		else
		{
			var last = _sleepStages.Periods.Last();
			
			viewModel.StartDate = last.EndDate.Date;
			viewModel.StartTime = last.EndTime;

			if( viewModel.EndTime <= viewModel.StartTime )
			{
				viewModel.EndTime = viewModel.EndTime.AddHours( 1 );
			}
		}
		
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

		var task   = dialog.ShowAsync( TopLevel.GetTopLevel( this ) );
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
			else
			{
				_sleepStages.AddPeriod( viewModel );
			}
		}
	}
	
	private void Delete_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );
		
		if( e.Source is Control control && control.Tag is SleepStagePeriodViewModel item )
		{
			_sleepStages.RemovePeriod( item );
		}
	}
}

