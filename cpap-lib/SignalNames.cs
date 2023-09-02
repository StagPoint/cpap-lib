using System.Collections.Generic;
using System.Diagnostics;

namespace cpaplib
{
	public static class SignalNames
	{
		private static Dictionary<string, string> _standardNames = new Dictionary<string, string>()
		{
			{ "Flow.40ms", "Flow Rate" },

			{ "Press.40ms", "Mask Pressure (High)" },  // High-resolution mask pressure
			{ "MaskPress.2s", "Mask Pressure (Low)" }, // Low-resolution mask pressure 

			{ "Press.2s", "Therapy Pressure" },

			{ "EprPress.2s", "Expiratory Pressure" },
			{ "EPRPress.2s", "Expiratory Pressure" },
			{ "EPAP", "Expiratory Pressure" },
			{ "S.BL.EPAP", "Expiratory Pressure" },

			{ "Leak.2s", "Leak" },
			{ "RespRate.2s", "Respiration Rate" },
			{ "TidVol.2s", "Tidal Volume" },
			{ "MinVent.2s", "Minute Vent" },
			{ "Snore.2s", "Snore" },
			{ "FlowLim.2s", "Flow Limit" },
			{ "Pulse.1s", "Pulse" },
			{ "SpO2.1s", "SpO2" },
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
