using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Controls;

public partial class EventMarkerEditor : UserControl
{
	public EventMarkerEditor()
	{
		InitializeComponent();

		PointerWheelChanged += ( sender, args ) =>
		{
			args.Handled = true;
		};
	}
	
	private async void SaveChanges_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is EventMarkerConfigurationViewModel viewModel )
		{
			viewModel.SaveChanges();

			var msgBox = MessageBoxManager.GetMessageBoxStandard( "Edit Events", "Your changes have been saved", ButtonEnum.Ok, Icon.Database );
			
			await msgBox.ShowWindowDialogAsync( (Window)TopLevel.GetTopLevel( this )! );
		}
	}
	
	private void ResetAll_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is EventMarkerConfigurationViewModel viewModel )
		{
			viewModel.ResetAll();
			
			// Force the display to refresh. This is really stupid, but since we're not using ObservableObjects 
			// and Avalonia doesn't provide a better way, :shrug:
			DataContext = null;
			DataContext = viewModel;
		}
	}
}

