using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.Helpers;

public static class DailyReportExtensions
{
	public static List<string> GetSignalNames( this DailyReport day )
	{
		var signalNames = new List<string>();
		foreach( var session in day.Sessions )
		{
			signalNames.AddRange( session.Signals.Select( x => x.Name ) );
		}
		
		return signalNames.Distinct().ToList();
	}
}
