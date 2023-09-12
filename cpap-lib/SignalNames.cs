using System.Collections.Generic;
using System.Diagnostics;

namespace cpaplib
{
	public static class SignalNames
	{
		private static Dictionary<string, string> _standardNames = new Dictionary<string, string>()
		{
			{ "Flow.40ms", "Flow Rate" },

			{ "Press.40ms", "Mask Pressure" }, // High-resolution mask pressure
			{ "Press", "Mask Pressure" },      // High-resolution mask pressure

			{ "MaskPress.2s", "Mask Pressure (Low)" }, // Low-resolution mask pressure 
			{ "MaskPress", "Mask Pressure (Low)" },    // Low-resolution mask pressure 

			{ "Press.2s", "Pressure" }, // Could also be Inhalation Pressure if in Bilevel or APAP Mode 

			{ "EprPress.2s", "Expiratory Pressure" },
			{ "EPRPress.2s", "Expiratory Pressure" },
			{ "EprPress", "Expiratory Pressure" },
			{ "EPAP", "Expiratory Pressure" },
			{ "S.BL.EPAP", "Expiratory Pressure" },
			
			{ "S.AFH.StartPress", "S.AS.StartPress" },
			{ "S.AFH.MaxPress", "S.AS.MaxPress" },
			{ "S.AFH.MinPress", "S.AS.MinPress" },

			{ "Leak.2s", "Leak Rate" },
			{ "Leak", "Leak Rate" },

			{ "RespRate.2s", "Respiration Rate" },
			{ "RespRate", "Respiration Rate" },

			{ "TidVol.2s", "Tidal Volume" },
			{ "TidVol", "Tidal Volume" },

			{ "MinVent.2s", "Minute Vent" },
			{ "MinVent", "Minute Vent" },

			{ "Snore.2s", "Snore" },
			{ "Snore", "Snore" },

			{ "FlowLim.2s", "Flow Limit" },
			{ "FlowLim", "Flow Limit" },

			{ "Pulse.1s", "Pulse" },
			{ "Pulse", "Pulse" },

			{ "SpO2.1s", "SpO2" },
			{ "SpO2", "SpO2" },
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
