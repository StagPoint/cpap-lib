using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.ViewModels;

using cpaplib;

using DynamicData.Binding;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

using OAuth;

namespace cpap_app.Views;

public partial class DailySleepStagesView : UserControl
{
	public bool IsAuthorizationConfigured
	{
		get { return _authConfig is { IsValid: true }; }
	}
	
	private SleepStagesViewModel? _sleepStages;
	
	private AuthorizationConfig? _authConfig;
	private AccessTokenInfo?     _accessTokenInfo;
	
	public DailySleepStagesView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		var config = AuthorizationConfigStore.GetConfig();
		if( !config.IsValid )
		{
			AuthNotConfigured.IsVisible = true;
		}
		else
		{
			_accessTokenInfo       = AccessTokenStore.GetAccessTokenInfo();
			AuthRequired.IsVisible = !_accessTokenInfo.IsValid;
		}
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
		};

		if( _sleepStages.Periods.Count == 0 )
		{
			viewModel.StartDate = day.RecordingStartTime.Date;
			viewModel.StartTime = day.RecordingStartTime;
		}
		else
		{
			var last = _sleepStages.Periods.Last();

			viewModel.Stage = last.Stage;
			
			viewModel.StartDate = last.EndDate.Date;
			viewModel.StartTime = last.EndTime;
		}
		
		viewModel.EndTime = viewModel.StartTime.AddHours( 1 );
		viewModel.EndDate = viewModel.EndTime.Date;
		
		await EditSleepPeriod( viewModel, true );
	}
	
	private async Task EditSleepPeriod( SleepStagePeriodViewModel viewModel, bool isNew )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );

		while( true )
		{
			var dialog = new ContentDialog()
			{
				Title             = isNew ? "Add a Sleep Stage period" : "Edit Sleep Stage Period",
				PrimaryButtonText = isNew ? "Add" : "Save",
				CloseButtonText   = "Cancel",
				DefaultButton     = ContentDialogButton.Primary,
				Content = new AddSleepStageView()
				{
					DataContext = viewModel
				},
			};

			var task   = dialog.ShowAsync( TopLevel.GetTopLevel( this ) );
			var result = await task;

			if( result != ContentDialogResult.Primary )
			{
				break;
			}

			if( viewModel.ValidationErrors.Count == 0 )
			{
				if( isNew )
				{
					_sleepStages.AddPeriod( viewModel );
				}
				else
				{
					_sleepStages.SavePeriod( viewModel );
				}
				
				break;
			}

			var msgBox = MessageBoxManager.GetMessageBoxStandard(
				"Data Validation Error",
				string.Join( "\r\n", viewModel.ValidationErrors ),
				ButtonEnum.Ok,
				Icon.Error );

			await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}
	}

	private void Delete_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );
		
		if( e.Source is Control { Tag: SleepStagePeriodViewModel item } )
		{
			_sleepStages.RemovePeriod( item );
		}
	}
	
	private async void Edit_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );
		
		if( e.Source is Control { Tag: SleepStagePeriodViewModel item } )
		{
			await EditSleepPeriod( item, false );
		}
	}
	
	private void Item_DoubleTapped( object? sender, TappedEventArgs e )
	{
		Edit_OnClick( sender, e );
	}
	
	private void SaveAuthConfiguration_OnClick( object? sender, RoutedEventArgs e )
	{
		if( string.IsNullOrEmpty( ClientID.Text ) || string.IsNullOrEmpty( ClientSecret.Text ) )
		{
			return;
		}
		
		_authConfig = new AuthorizationConfig
		{
			ClientID     = ClientID.Text,
			ClientSecret = ClientSecret.Text
		};
		
		AuthorizationConfigStore.SaveConfig( _authConfig );

		AuthNotConfigured.IsVisible = !_authConfig.IsValid;
		AuthRequired.IsVisible      = true;
	}
	
	private void AuthorizeAccess_OnClick( object? sender, RoutedEventArgs e )
	{
		//throw new System.NotImplementedException();
	}
}

