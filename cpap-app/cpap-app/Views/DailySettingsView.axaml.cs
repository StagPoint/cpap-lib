using Avalonia;
using Avalonia.Controls;

using cpap_app.Helpers;
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
				DataContext = CreateMachineSettingsViewModel( day );
			}
		}
	}

	private MachineSettingsViewModel CreateMachineSettingsViewModel( DailyReport day )
	{
		var settings  = day.Settings;
		var viewModel = new MachineSettingsViewModel();
		var items     = viewModel.Settings;

		items.Add( new MachineSettingsItemViewModel( "Mode", settings.Mode ) );

		if( settings.Mode == OperatingMode.CPAP )
		{
			items.Add( new MachineSettingsItemViewModel( "Pressure", settings.CPAP.Pressure, "cmH20" ) );
		}
		else if( settings.Mode == OperatingMode.APAP )
		{
			items.Add( new MachineSettingsItemViewModel( "Min Pressure",  settings.AutoSet.MinPressure, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Max Pressure",  settings.AutoSet.MaxPressure, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Response Type", settings.AutoSet.ResponseType ) );
		}
		else if( settings.Mode == OperatingMode.ASV )
		{
			items.Add( new MachineSettingsItemViewModel( "EPAP", settings.ASV.EPAP, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Max IPAP", settings.ASV.IpapMax, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "PS Min", settings.ASV.MinPressureSupport, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "PS Max", settings.ASV.MaxPressureSupport, "cmH20" ) );
		}

		items.Add( new MachineSettingsItemViewModel( "Ramp Mode", settings.RampMode ) );
		if( settings.RampMode != RampModeType.Off )
		{
			items.Add( new MachineSettingsItemViewModel( "Ramp Pressure", settings.RampStartingPressure, "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Ramp Time",     settings.RampTime,             "Minutes" ) );
		}

		// TODO: Create a helper function to indicate which modes/models EPR is relevant for
		if( settings.Mode == OperatingMode.APAP || settings.Mode == OperatingMode.CPAP )
		{
			items.Add( new MachineSettingsItemViewModel( "EPR Enabled", settings.EPR.EprEnabled ) );
			if( settings.EPR.EprEnabled )
			{
				items.Add( new MachineSettingsItemViewModel( "EPR Mode",  NiceNames.Format( settings.EPR.Mode.ToString() ) ) );
				items.Add( new MachineSettingsItemViewModel( "EPR Level", settings.EPR.Level ) );
			}
		}

		items.Add( new MachineSettingsItemViewModel( "Antibacterial Filter", settings.AntibacterialFilter ? "Yes" : "No" ) );
		items.Add( new MachineSettingsItemViewModel( "Smart Start", settings.SmartStart ) );
		items.Add( new MachineSettingsItemViewModel( "Mask Type",   NiceNames.Format( settings.Mask.ToString() ) ) );

		items.Add( new MachineSettingsItemViewModel( "Climate Control",   settings.ClimateControl ) );
		items.Add( new MachineSettingsItemViewModel( "Humidifier Status", settings.HumidifierStatus ) );
		items.Add( new MachineSettingsItemViewModel( "Humidity Level",    settings.HumidityLevel ) );

		items.Add( new MachineSettingsItemViewModel( "Heating Enabled", settings.TemperatureEnabled ) );
		items.Add( new MachineSettingsItemViewModel( "Temperature",     $"{settings.Temperature:F1}", "\u00b0F" ) );

		return viewModel;
	}
}
