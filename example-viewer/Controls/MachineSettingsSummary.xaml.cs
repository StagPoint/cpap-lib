using System.Windows;
using System.Windows.Controls;

using cpaplib;

namespace cpapviewer.Controls;

public partial class MachineSettingsSummary : UserControl
{
	public MachineSettingsSummary()
	{
		InitializeComponent();
	}
	
	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DailyReport day )
			{
				grdMachineSettings.DataContext = day.Settings;
			}
		}
	}
}

