using System;

namespace cpap_app.ViewModels.Tooltips;

public class SessionTimesViewModel
{
	public DateTime Date               { get; set; } = DateTime.Today;
	public DateTime Start              { get; set; } = DateTime.Today;
	public DateTime End                { get; set; } = DateTime.Today.AddHours( 8 );
	public TimeSpan TotalTimeSpan      { get; set; } = TimeSpan.FromHours( 8.25 );
	public TimeSpan TotalSleepTime     { get; set; } = TimeSpan.FromHours( 7 );
	public int      NumberOfSessions   { get; set; } = 2;
	public TimeSpan LongestSessionTime { get; set; } = TimeSpan.FromHours( 4.256789 );

	public double   SleepEfficiency  { get => TotalSleepTime.TotalHours / TotalTimeSpan.TotalHours; }
}
