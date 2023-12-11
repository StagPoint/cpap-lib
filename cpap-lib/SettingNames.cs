using System.Collections.Generic;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class SettingNames
	{
		public const string MaskOn  = "MaskOn";
		public const string MaskOff = "MaskOff";
		
		private static Dictionary<string, string> _standardNames = new Dictionary<string, string>()
		{
			{ "MaskOn", MaskOn },
			{ "Mask On", MaskOn },
			{ "MaskOff", MaskOff },
			{ "Mask Off", MaskOff },
		};
		
		public static string GetStandardName( string key )
		{
			return _standardNames.TryGetValue( key, out string standardized ) ? standardized : key;
		}
	}
}
