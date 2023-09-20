using System;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyReportViewModel : ViewModelBase
{
	public DateTime ReportDate { get; set; }
	
	public DailyReportViewModel( DateTime date )
	{
		ReportDate = date;
	}
}
