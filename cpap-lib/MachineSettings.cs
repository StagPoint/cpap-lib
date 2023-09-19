﻿using System;
using System.Collections.Generic;

namespace cpaplib
{
	public class MachineSettings
	{
		#region Public properties

		/// <summary>
		/// The mode (CPAP, AutoSet, BiLevel, etc.) used on the reported day
		/// </summary>
		public OperatingMode Mode { get; set; }

		public bool HeatedTube { get; set; }

		public bool Humidifier { get; set; }

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
		public OnOffType SmartStart { get; set; }
		public double PtAccess { get; set; }

		#endregion
	}
}
