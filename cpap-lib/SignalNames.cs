using System.Collections.Generic;

namespace cpaplib
{
	public static class SignalNames
	{
		public const string FlowRate        = "Flow Rate";
		public const string MaskPressure    = "Mask Pressure";
		public const string MaskPressureLow = "Mask Pressure (Low)";
		public const string Pressure        = "Pressure";
		public const string EPAP            = "EPAP";
		public const string LeakRate        = "Leak Rate";
		public const string RespirationRate = "Respiration Rate";
		public const string TidalVolume     = "Tidal Volume";
		public const string MinuteVent      = "Minute Vent";
		public const string Snore           = "Snore";
		public const string FlowLimit       = "Flow Limit";
		public const string SpO2            = "SpO2";
		public const string Pulse           = "Pulse";
		public const string Movement        = "Movement";

		private static Dictionary<string, string> _standardNames = new Dictionary<string, string>()
		{
			{ "Flow.40ms",	FlowRate },
			{ "Flow Rate",	FlowRate },
			{ "Flow",		FlowRate },

			{ "Press.40ms", MaskPressure }, // High-resolution mask pressure
			{ "Press", MaskPressure },      // High-resolution mask pressure

			{ "MaskPress.2s", "Mask Pressure (Low)" }, // Low-resolution mask pressure 
			{ "MaskPress", "Mask Pressure (Low)" },    // Low-resolution mask pressure 

			{ "Press.2s", Pressure }, // Could also be Inhalation Pressure if in Bilevel or APAP Mode ?

			{ "EprPress.2s", EPAP },
			{ "EPRPress.2s", EPAP },
			{ "EprPress", EPAP },
			{ "EPAP", EPAP },
			{ "S.BL.EPAP", EPAP },

			{ "S.AFH.StartPress", "S.AS.StartPress" },
			{ "S.AFH.MaxPress", "S.AS.MaxPress" },
			{ "S.AFH.MinPress", "S.AS.MinPress" },

			{ "Leak.2s", LeakRate },
			{ "Leak", LeakRate },

			{ "RespRate.2s", RespirationRate },
			{ "RespRate", RespirationRate },

			{ "TidVol.2s", TidalVolume },
			{ "TidVol", TidalVolume },

			{ "MinVent.2s", MinuteVent },
			{ "MinVent", MinuteVent },

			{ "Snore.2s", Snore },
			{ "Snore", Snore },

			{ "FlowLim.2s", FlowLimit },
			{ "FlowLim", FlowLimit },

			{ "Pulse.1s", Pulse },
			{ "Pulse", Pulse },

			{ "SpO2.1s", SpO2 },
			{ "SpO2", SpO2 },
		};

		public static string GetStandardName( string key )
		{
			if( _standardNames.TryGetValue( key, out string result ) )
			{
				return result;
			}

			// _standardNames[ key ] = key;
			// Debug.WriteLine( $@"{{ ""{key}"", """"}}," );

			return key;
		}
	}
}
