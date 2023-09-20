using System;

using cpaplib;

namespace cpap_app.ViewModels;

public class HomeViewModel : ViewModelBase
{
	public DateTime ReportDate { get; set; }
	
	public HomeViewModel( DateTime date )
	{
		ReportDate = date;
	}
}
