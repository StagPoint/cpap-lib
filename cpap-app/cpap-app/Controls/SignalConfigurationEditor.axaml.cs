using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Controls;

public partial class SignalConfigurationEditor : UserControl
{
	public SignalConfigurationEditor()
	{
		InitializeComponent();
	
		// PointerWheelChanged += ( sender, args ) =>
		// {
		// 	args.Handled = true;
		// };
	}
	
	private void ResetAll_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is SignalChartConfigurationViewModel viewModel )
		{
			viewModel.ResetAll();
					
			// Force the display to refresh. This is really stupid, but since we're not using ObservableObjects 
			// and Avalonia doesn't provide a better way, :shrug:
			DataContext = null;
			DataContext = viewModel;
		}
	}
	
	private async void SaveChanges_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is SignalChartConfigurationViewModel viewModel )
		{
			viewModel.SaveChanges();
			
			var msgBox = MessageBoxManager.GetMessageBoxStandard( "Edit Events", "Your changes have been saved", ButtonEnum.Ok, Icon.Database );
			
			await msgBox.ShowWindowDialogAsync( (Window)TopLevel.GetTopLevel( this )! );
		}
	}
}

