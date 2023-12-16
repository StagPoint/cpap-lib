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
		return CreateResMedViewModel( day );
	}

	private MachineSettingsViewModel CreateResMedViewModel( DailyReport day )
	{
		var settings  = day.Settings;
		var viewModel = new MachineSettingsViewModel();
		var items     = viewModel.Settings;

		var mode = settings.GetValue<OperatingMode>( SettingNames.Mode );

		items.Add( new MachineSettingsItemViewModel( "Mode", GetModeString( mode ) ) );

		switch( mode )
		{
			case OperatingMode.Cpap:
				items.Add( new MachineSettingsItemViewModel( "Pressure", $"{settings[ SettingNames.Pressure ]:F2}", "cmH20" ) );
				break;
			case OperatingMode.Apap:
				items.Add( new MachineSettingsItemViewModel( "Min Pressure",  $"{settings[ SettingNames.MinPressure ]:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max Pressure",  $"{settings[ SettingNames.MaxPressure ]:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Response Type", (AutoSetResponseType)settings[ SettingNames.ResponseType ] ) );
				break;
			case OperatingMode.Asv:
				items.Add( new MachineSettingsItemViewModel( "EPAP",     $"{settings[ SettingNames.EPAP ]:F2}",               "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max IPAP", $"{settings[ SettingNames.IpapMax ]:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Min",   $"{settings[ SettingNames.MinPressureSupport ]:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Max",   $"{settings[ SettingNames.MaxPressureSupport ]:F2}", "cmH20" ) );
				break;
			case OperatingMode.AsvVariableEpap:
				items.Add( new MachineSettingsItemViewModel( "Min EPAP", $"{settings[ SettingNames.EpapMin ]:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max EPAP", $"{settings[ SettingNames.EpapMax ]:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Min IPAP", $"{settings[ SettingNames.IpapMin ]:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max IPAP", $"{settings[ SettingNames.IpapMax ]:F2}",            "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Min",   $"{settings[ SettingNames.MinPressureSupport ]:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "PS Max",   $"{settings[ SettingNames.MaxPressureSupport ]:F2}", "cmH20" ) );
				break;
		}

		var rampMode = settings.GetValue<RampModeType>( SettingNames.RampMode );

		items.Add( new MachineSettingsItemViewModel( "Ramp Mode", rampMode ) );
		if( rampMode != RampModeType.Off )
		{
			items.Add( new MachineSettingsItemViewModel( "Ramp Pressure", $"{settings[ SettingNames.RampPressure ]:F2}", "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Ramp Time",     settings[ SettingNames.RampTime ],             "Minutes" ) );
		}

		// TODO: Create a helper function to indicate which modes/models EPR is relevant for
		if( mode is OperatingMode.Apap or OperatingMode.Cpap )
		{
			if( settings.TryGetValue( SettingNames.EprEnabled, out bool eprEnabled ) )
			{
				items.Add( new MachineSettingsItemViewModel( "EPR Enabled", eprEnabled ) );
				if( eprEnabled )
				{
					var eprMode = settings.GetValue<EprType>( SettingNames.EprMode );

					items.Add( new MachineSettingsItemViewModel( "EPR Mode",  NiceNames.Format( eprMode.ToString() ) ) );
					items.Add( new MachineSettingsItemViewModel( "EPR Level", settings[ SettingNames.EprLevel ] ) );
				}
			}
		}

		var maskType         = settings.GetValue<MaskType>( SettingNames.MaskType );
		var humidifierStatus = (OnOffType)settings[ SettingNames.HumidifierMode ];

		items.Add( new MachineSettingsItemViewModel( "Antibacterial Filter", (bool)settings[ SettingNames.AntibacterialFilter ] ? "Yes" : "No" ) );
		items.Add( new MachineSettingsItemViewModel( "Smart Start",          (OnOffType)settings[ SettingNames.SmartStart ] ) );
		items.Add( new MachineSettingsItemViewModel( "Mask Type",            NiceNames.Format( maskType.ToString() ) ) );

		items.Add( new MachineSettingsItemViewModel( "Climate Control",   (ClimateControlType)settings[ SettingNames.ClimateControl ] ) );
		items.Add( new MachineSettingsItemViewModel( "Humidifier Status", humidifierStatus ) );

		if( humidifierStatus == OnOffType.On )
		{
			items.Add( new MachineSettingsItemViewModel( "Humidity Level", settings[ SettingNames.HumidityLevel ] ) );
		}

		var heatedTubeEnabled = (bool)settings[ SettingNames.HeatedTubeEnabled ];
		
		items.Add( new MachineSettingsItemViewModel( "Heated Tube Enabled", heatedTubeEnabled ) );
		
		if( heatedTubeEnabled )
		{
			items.Add( new MachineSettingsItemViewModel( "Tube Temperature", $"{settings[ SettingNames.TubeTemperature ]:F1}", "\u00b0F" ) );
		}

		return viewModel;
	}

	private static string GetModeString( OperatingMode mode )
	{
		// TODO: Should probably refer to the raw Mode setting to differentiate modes, which would entail making the raw settings data available 
		return mode switch
		{
			OperatingMode.Cpap              => "CPAP",
			OperatingMode.Apap              => "Auto",
			OperatingMode.Asv               => "ASV",
			OperatingMode.AsvVariableEpap => "ASV Auto",
			_                               => mode.ToString()
		};
	}
}
