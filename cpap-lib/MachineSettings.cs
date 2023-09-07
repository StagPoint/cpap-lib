using System;
using System.Collections.Generic;

namespace cpaplib
{
	public class MachineSettings
	{
		#region Public properties

		/// <summary>
		/// The mode (CPAP, AutoSet, BiLevel, etc.) used on the reported day
		/// </summary>
		public OperatingMode Mode { get; private set; }

		public bool HeatedTube { get; private set; }

		public bool Humidifier { get; private set; }

		public EprSettings EPR { get; set; } = new EprSettings();
		public CpapSettings CPAP { get; set; } = new CpapSettings();
		public AutoSetSettings AutoSet { get; set; } = new AutoSetSettings();

		public AsvSettings ASV { get; set; } = null;

		public AvapsSettings AVAP { get; set; } = null;

		public RampModeType RampMode { get; set; }
		public double RampTime { get; set; }

		public double RampStartingPressure
		{
			get
			{
				switch( Mode )
				{
					case OperatingMode.CPAP:
						return CPAP.StartPressure;
					case OperatingMode.APAP:
						return AutoSet.StartPressure;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		

		public EssentialsMode Essentials { get; set; }
		public bool AntibacterialFilter { get; set; }
		public MaskType Mask { get; set; }
		public double Tube { get; set; }
		public ClimateControlType ClimateControl { get; set; }
		public OnOffType HumidifierStatus { get; set; }
		public double HumidityLevel { get; set; }
		public bool TemperatureEnabled { get; set; }
		public double Temperature { get; set; }
		public bool SmartStart { get; set; }
		public double PtAccess { get; set; }

		#endregion

		#region Public functions

		internal void ReadFrom( Dictionary<string, double> data )
		{
			if( data.TryGetValue( "CPAP_MODE", out double legacyMode ) )
			{
				switch( (int)legacyMode )
				{
					case 1:
					case 2:
						Mode = OperatingMode.APAP;
						break;
					case 3:
						Mode = OperatingMode.CPAP;
						break;
					default:
						Mode = OperatingMode.UNKNOWN;
						break;
				}
			}
			else
			{
				var mode = (int)data.GetValue( "Mode" );
				if( mode >= (int)OperatingMode.MAX_VALUE )
				{
					mode = -1;
				}

				Mode = (OperatingMode)mode;
			}

			if( Mode == OperatingMode.ASV )
			{
				ASV = new AsvSettings
				{
					StartPressure = data[ "S.AV.StartPress" ],
					MinPressure   = data[ "S.AV.MinPS" ],
					MaxPressure   = data[ "S.AV.MaxPS" ],
					EPAP          = data[ "S.AV.EPAP" ],
					EpapMin       = data[ "S.AV.EPAP" ],
					EpapMax       = data[ "S.AV.EPAP" ],
					IpapMin       = data[ "S.AV.EPAP" ] + data[ "S.AV.MinPS" ],
					IpapMax       = data[ "S.AV.EPAP" ] + data[ "S.AV.MaxPS" ],
				};
			}
			else if( Mode == OperatingMode.ASV_VARIABLE_EPAP )
			{
				ASV = new AsvSettings
				{
					StartPressure = data[ "S.AA.StartPress" ],
					MinPressure   = data[ "S.AA.MinPS" ],
					MaxPressure   = data[ "S.AA.MaxPS" ],
					EPAP          = data[ "S.AA.MinEPAP" ],
					EpapMin       = data[ "S.AA.MinEPAP" ],
					EpapMax       = data[ "S.AA.MaxEPAP" ],
				};
			}
			else if( Mode == OperatingMode.AVAPS )
			{
				AVAP = new AvapsSettings
				{
					StartPressure = data[ "S.i.StartPress" ],
					MinPressure   = data[ "S.i.MinPS" ],
					MaxPressure   = data[ "S.i.MaxPS" ],
					EpapAuto      = data[ "S.i.EPAPAuto" ] > 0.5,
					EPAP          = data[ "S.i.EPAP" ],
					EpapMin       = data[ "S.i.EPAP" ],
					EpapMax       = data[ "S.i.EPAP" ],
				};

				if( AVAP.EpapAuto )
				{
					AVAP.EpapMin = data[ "S.i.MinEPAP" ];
					AVAP.EpapMax = data[ "S.i.MaxEPAP" ];
					
					AVAP.IPAP    = AVAP.EpapMin + AVAP.MinPressure;
					AVAP.IpapMin = AVAP.EpapMin + AVAP.MinPressure;
					AVAP.IpapMax = AVAP.EpapMax + AVAP.MaxPressure;
				}
				else 
				{
					AVAP.IPAP    = AVAP.EPAP + AVAP.MinPressure;
					AVAP.IpapMin = AVAP.EPAP + AVAP.MinPressure;
					AVAP.IpapMax = AVAP.EPAP + AVAP.MaxPressure;
				}
			}

			HeatedTube = data["HeatedTube"] > 0.5;
			Humidifier = data["Humidifier"] > 0.5;

			CPAP.StartPressure = data["S.C.StartPress"];
			CPAP.Pressure = data["S.C.Press"];

			EPR.ClinicianEnabled = data["S.EPR.ClinEnable"] >= 0.5;
			EPR.EprEnabled = data["S.EPR.EPREnable"] >= 0.5;
			EPR.Level = (int)data["S.EPR.Level"];
			EPR.Mode = (EprType)(int)(data["S.EPR.EPRType"] + 1);

			AutoSet.ResponseType = (AutoSetResponseType)(int)data["S.AS.Comfort"];
			AutoSet.StartPressure = data["S.AS.StartPress"];
			AutoSet.MaxPressure = data["S.AS.MaxPress"];
			AutoSet.MinPressure = data["S.AS.MinPress"];

			RampMode = (RampModeType)(int)data["S.RampEnable"];
			RampTime = data["S.RampTime"];

			SmartStart = data["S.SmartStart"] > 0.5;
			AntibacterialFilter = data["S.ABFilter"] >= 0.5;

			ClimateControl     = (ClimateControlType)(int)data["S.ClimateControl"];
			Tube               = data["S.Tube"];
			HumidifierStatus   = data["S.HumEnable"] > 0.5 ? OnOffType.On : OnOffType.Off;
			HumidityLevel      = data["S.HumLevel"];
			TemperatureEnabled = data["S.TempEnable"] > 0.5;
			Temperature        = data["S.Temp"] * 1.8 + 32; // Converted from Celsius to Fahrenheit

			Mask = (MaskType)(int)data["S.Mask"];

			Essentials = data["S.PtAccess"] > 0.5 ? EssentialsMode.On : EssentialsMode.Plus;

			PtAccess = data["S.PtAccess"];
		}

		#endregion
	}
}
