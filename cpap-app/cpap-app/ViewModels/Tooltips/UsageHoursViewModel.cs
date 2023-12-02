using System;

using cpap_app.Converters;

namespace cpap_app.ViewModels.Tooltips;

public class UsageHoursViewModel
{
	public DateTime Date           { get; set; } = DateTime.Today;
	public TimeSpan TotalTimeSpan  { get; set; } = TimeSpan.FromHours( 8.25 );
	public TimeSpan TotalSleepTime { get; set; } = TimeSpan.FromHours( 7 );
	public TimeSpan NonTherapyTime { get; set; } = TimeSpan.FromHours( 1.25 );
}
