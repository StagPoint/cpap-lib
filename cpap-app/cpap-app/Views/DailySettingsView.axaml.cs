using Avalonia;
using Avalonia.Controls;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySettingsView : UserControl
{
	public DailySettingsView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is DailyReport day )
			{
				DataContext = MachineSettingsViewModel.Generate( day );
			}
		}
	}
}
