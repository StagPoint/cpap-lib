using System;

using cpaplib;

namespace cpap_app.ViewModels.Tooltips;

public class SignalStatisticsViewModel
{
	public DateTime          Date          { get; set; } = DateTime.Today;
	public SignalStatistics? Statistics    { get; set; } = null;
	public string            Units         { get; set; } = string.Empty;

	public bool IsValid { get => Statistics != null; }
}
