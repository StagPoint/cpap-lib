using System;
using System.Collections.Generic;

namespace cpap_app.ViewModels;

public class DailySummaryViewModel
{
	public DateTime     ReportDate         { get; set; }
	public DateTime     RecordingStartTime { get; set; }
	public DateTime     RecordingEndTime   { get; set; }
	public TimeSpan     TotalSleepTime     { get; set; }
	public List<string> Sources            { get; set; } = new List<string>();
}
