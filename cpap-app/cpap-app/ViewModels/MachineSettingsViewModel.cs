﻿using System.Collections.Generic;
using System.Drawing;
using System.Reactive;

using cpaplib;
using cpap_app.Helpers;

namespace cpap_app.ViewModels;

public class MachineSettingsItemViewModel
{
	public string Name  { get; set; }
	public object Value { get; set; }
	public string Units { get; set; }

	public MachineSettingsItemViewModel( string name, object value, string units = "" )
	{
		Name  = name;
		Value = value;
		Units = units;
	}

	public override string ToString()
	{
		return $"{Name} = {Value} {Units}";
	}
}

public class MachineSettingsViewModel
{
	public List<MachineSettingsItemViewModel> Settings { get; init; } = new List<MachineSettingsItemViewModel>();

	public static MachineSettingsViewModel Generate( DailyReport day )
	{
		if( day.MachineInfo.Manufacturer == MachineManufacturer.ResMed )
		{
			return CreateResMedViewModel( day );
		}

		return CreatePhilipsRespironicsViewModel( day );
	}
	
		private static MachineSettingsViewModel CreatePhilipsRespironicsViewModel( DailyReport day )
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
				items.Add( new MachineSettingsItemViewModel( "Min Pressure", $"{settings[ SettingNames.MinPressure ]:F2}", "cmH20" ) );
				items.Add( new MachineSettingsItemViewModel( "Max Pressure", $"{settings[ SettingNames.MaxPressure ]:F2}", "cmH20" ) );

				var flexMode = (FlexMode)settings[ SettingNames.FlexMode ];
				items.Add( new MachineSettingsItemViewModel( "Flex Mode", flexMode ) );

				if( flexMode != FlexMode.None && flexMode != FlexMode.Unknown )
				{
					items.Add( new MachineSettingsItemViewModel( "Flex Level",  (int)settings[ SettingNames.FlexLevel ] ) );
					items.Add( new MachineSettingsItemViewModel( "Flex Locked", (bool)settings[ SettingNames.FlexLock ] ) );
				}
				break;
		}

		var rampTime = (int)settings[ SettingNames.RampTime ];
		if( rampTime > 0 )
		{
			items.Add( new MachineSettingsItemViewModel( "Ramp Pressure", $"{settings[ SettingNames.RampPressure ]:F2}", "cmH20" ) );
			items.Add( new MachineSettingsItemViewModel( "Ramp Time",     rampTime,                                      "Minutes" ) );
		}

		items.Add( new MachineSettingsItemViewModel( "Auto On",  (bool)settings[ SettingNames.AutoOn ] ) );
		items.Add( new MachineSettingsItemViewModel( "Auto Off", (bool)settings[ SettingNames.AutoOff ] ) );
		
		items.Add( new MachineSettingsItemViewModel( "Hose Diameter", (int)settings[ SettingNames.HoseDiameter ] ) );
		
		var humidiferAttached = (bool)settings[ SettingNames.HumidifierAttached ];
		items.Add( new MachineSettingsItemViewModel( "Humidifier Connected", humidiferAttached ? "Yes" : "No" ) );
		if( humidiferAttached )
		{
			var humidifierMode = (HumidifierMode)settings[ SettingNames.HumidifierMode ];
			items.Add( new MachineSettingsItemViewModel( "Humidifier Mode", humidifierMode ) );

			// ReSharper disable once ConvertIfStatementToSwitchStatement
			if( humidifierMode == HumidifierMode.Fixed )
			{
				items.Add( new MachineSettingsItemViewModel( "Humidity Level", (int)settings[ SettingNames.HumidityLevel ] ) );
			}
			else if( humidifierMode == HumidifierMode.HeatedTube )
			{
				items.Add( new MachineSettingsItemViewModel( "Tube Temperature", (double)settings[ SettingNames.TubeTemperature ] ) );
			}
		}

		if( settings.TryGetValue( SettingNames.TubeTempLocked, out bool tubeTempLocked ) )
		{
			items.Add( new MachineSettingsItemViewModel( "Tube Locked", tubeTempLocked ) );
		}

		return viewModel;
	}

	private static MachineSettingsViewModel CreateResMedViewModel( DailyReport day )
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
			OperatingMode.Cpap            => "CPAP",
			OperatingMode.Apap            => "Auto",
			OperatingMode.Asv             => "ASV",
			OperatingMode.AsvVariableEpap => "ASV Auto",
			_                             => mode.ToString()
		};
	}
}
