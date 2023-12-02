using System;

using cpaplib;

namespace cpap_app.ViewModels.Tooltips;

public class SignalStatisticsViewModel
{
	public DateTime          Date       { get; set; } = DateTime.Today;
	public SignalStatistics? Statistics { get; set; } = new();
	public string            Units      { get; set; } = string.Empty;
}
