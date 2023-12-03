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

		items.Add( new MachineSettingsItemViewModel( "Mode", GetModeString( settings.Mode ) ) );

		switch( settings.Mode )
		{
			case OperatingMode.CPAP:
				items.Add( new MachineSettingsItemViewModel( "Pressure", settings.CPAP.Pressure, "cmH20" ) );
				break;
			case OperatingMode.APAP:
				items.Add( new MachineSettingsItemViewModel( "Min Pressure",  settings.AutoSet.MinPressure, "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max Pressure",  settings.AutoSet.MaxPressure, "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Response Type", settings.AutoSet.ResponseType ) );
				break;
			case OperatingMode.ASV:
				items.Add( new MachineSettingsItemViewModel( "EPAP",     settings.ASV.EPAP,                       "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max IPAP", $"{settings.ASV.IpapMax:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Min",   $"{settings.ASV.MinPressureSupport:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Max",   $"{settings.ASV.MaxPressureSupport:F2}", "cmH20" ) );
				break;
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
	
	private static string GetModeString( OperatingMode mode )
	{
		// TODO: Should probably refer to the raw Mode setting to differentiate modes, which would entail making the raw settings data available 
		return mode switch
		{
			OperatingMode.CPAP              => "CPAP",
			OperatingMode.APAP              => "Auto",
			OperatingMode.ASV               => "ASV",
			OperatingMode.ASV_VARIABLE_EPAP => "ASV Auto",
			_                               => mode.ToString()
		};
	}
}
