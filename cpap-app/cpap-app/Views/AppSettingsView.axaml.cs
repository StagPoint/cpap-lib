using Avalonia.Controls;

using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class AppSettingsView : UserControl
{
	public AppSettingsView()
	{
		InitializeComponent();

		DataContext = new AppSettingsViewModel();
	}
}

