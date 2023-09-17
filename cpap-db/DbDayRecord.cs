using cpaplib;

using SQLite;

namespace cpap_db;

[Table( "Day" )]
public class DbDayRecord
{
	[PrimaryKey]
	public long ReportDate         { get; set; }
	
	public DateTime RecordingStartTime { get; set; }
	public DateTime RecordingEndTime   { get; set; }
	public TimeSpan Duration           { get; set; }
	public TimeSpan OnDuration         { get; set; }
	public double   PatientHours       { get; set; }

	public DbDayRecord( DailyReport day )
	{
		ReportDate = day.ReportDate.Date.ToFileTimeUtc();

		RecordingStartTime = day.RecordingStartTime;
		RecordingEndTime   = day.RecordingEndTime;
		Duration           = day.Duration;
		OnDuration         = day.OnDuration;
		PatientHours       = day.PatientHours;
	}
}
