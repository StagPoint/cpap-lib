using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

namespace cpap_app.Controls;

public partial class CheckMarkMenuItem : UserControl
{
	public CheckMarkMenuItem()
	{
		InitializeComponent();
	}
	
	private void InputElement_OnPointerPressed( object? sender, PointerPressedEventArgs e )
	{
		if( DataContext is CheckmarkMenuItemViewModel vm )
		{
			vm.IsChecked = !vm.IsChecked;
		}
	}
}

