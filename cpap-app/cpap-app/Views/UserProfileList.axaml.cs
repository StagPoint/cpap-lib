using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class UserProfileList : UserControl
{
	public UserProfileList()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		UserProfileListView.ItemsSource = UserProfileStore.SelectAll();
	}

	private void MakeActive_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( sender is not Control { Tag: UserProfile profile } )
		{
			throw new InvalidOperationException();
		}

		profile.LastLogin = DateTime.Now;
		UserProfileStore.Update( profile );

		RaiseEvent( new UserProfileEventArgs
		{
			RoutedEvent = UserProfileEvents.UserProfileChangedEvent,
			Source      = this,
			Profile     = profile,
			Action      = UserProfileAction.Activated
		} );
	}
	
	private async void ViewDetails_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( sender is not Control { Tag: UserProfile profile } )
		{
			throw new InvalidOperationException();
		}

		var contentView = new EditUserProfileView()
		{
			DataContext = profile
		};

		var dialog = new TaskDialog()
		{
			Title = $"Edit User Profile",
			Buttons =
			{
				new TaskDialogButton( "Save", TaskDialogStandardResult.Yes ),
				TaskDialogButton.CancelButton,
			},
			XamlRoot = (Visual)VisualRoot!,
			Content  = contentView,
			MaxWidth = 800,
		};
		
		var dialogResult = await dialog.ShowAsync();

		if( (TaskDialogStandardResult)dialogResult == TaskDialogStandardResult.Yes && !string.IsNullOrEmpty( profile.UserName.Trim() ) )
		{
			if( UserProfileStore.Update( profile ) )
			{
				RaiseEvent( new UserProfileEventArgs
				{
					RoutedEvent = UserProfileEvents.UserProfileChangedEvent,
					Source      = this,
					Profile     = profile,
					Action      = UserProfileAction.Modified
				} );
		
				UserProfileListView.ItemsSource = UserProfileStore.SelectAll();
			}
		}
	}
	
	private async void Delete_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( sender is not Control { Tag: UserProfile profile } )
		{
			throw new InvalidOperationException();
		}
		
		var allUserProfiles = UserProfileStore.SelectAll();
		if( allUserProfiles.Count <= 1 )
		{
			var notificationDialog = MessageBoxManager.GetMessageBoxStandard(
				"Delete User Profile",
				"You cannot delete the only User Profile.\nIf you wish to delete this profile, you must first make another profile.",
				ButtonEnum.Ok,
				Icon.Error );

			await notificationDialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

			return;
		}
		
		string message = @$"Are you ABSOLUTELY SURE that you want to delete the profile for {profile.UserName}?
Doing so will also erase all of the data associated with this profile.
THIS OPERATION CANNOT BE UNDONE";

		var confirmDialog = MessageBoxManager.GetMessageBoxStandard(
			"Delete User Profile",
			message,
			ButtonEnum.YesNo,
			Icon.Warning );

		var confirmDialogResult = await confirmDialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		if( confirmDialogResult != ButtonResult.Yes )
		{
			return;
		}

		UserProfileStore.Delete( profile );

		allUserProfiles.RemoveAll( x => x.UserProfileID == profile.UserProfileID );

		RaiseEvent( new UserProfileEventArgs
		{
			RoutedEvent = UserProfileEvents.UserProfileChangedEvent,
			Source      = this,
			Profile     = profile,
			Action      = UserProfileAction.Deleted
		} );
		
		UserProfileListView.ItemsSource = allUserProfiles;
	}
	
	private void MenuItem_OnDoubleTapped( object? sender, TappedEventArgs e )
	{
		ViewDetails_OnTapped( sender, e );
	}
}

