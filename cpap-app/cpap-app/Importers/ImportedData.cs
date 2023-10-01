using System;
using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.Importers;

public class ImportedData
{
	public DateTime StartTime { get; set;  }
	public DateTime EndTime   { get; set; }

	public List<Session>       Sessions { get; set; } = new List<Session>();
	public List<ReportedEvent> Events   { get; set; } = new List<ReportedEvent>();
}
