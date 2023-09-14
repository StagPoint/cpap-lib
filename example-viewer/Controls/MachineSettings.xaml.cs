using System.Windows;
using System.Windows.Controls;

using cpaplib;

namespace example_viewer.Controls;

public partial class MachineSettings : UserControl
{
	public MachineSettings()
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

