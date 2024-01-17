using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class OximetryImportSettingsView : UserControl
{
	public OximetryImportSettingsView()
	{
		InitializeComponent();
	}
	
	private async void SaveChanges_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is ImportOptionsViewModel viewModel )
		{
			viewModel.SaveChanges();
			
			var msgBox = MessageBoxManager.GetMessageBoxStandard( "Edit Events", "Your changes have been saved", ButtonEnum.Ok, Icon.Database );
			await msgBox.ShowWindowDialogAsync( (Window)TopLevel.GetTopLevel( this )! );
		}
	}
}

