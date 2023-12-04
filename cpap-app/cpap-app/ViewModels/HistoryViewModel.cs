using System;
using System.Collections.Generic;

using cpaplib;

namespace cpap_app.ViewModels;

public class HistoryViewModel : ViewModelBase
{
	public DateTime Start { get; set; } = DateTime.Today.AddDays( -90 );
	public DateTime End   { get; set; } = DateTime.Today;

	public int TotalDays
	{
		get => (int)Math.Floor( (End.Date - Start.Date).TotalDays + 1.0 );
	}

	public List<DailyReport> Days = new List<DailyReport>();
}
