using System;
using System.Diagnostics;
using System.Linq;

using cpaplib;

namespace cpap_app.ViewModels.Tooltips;

public class AhiHistoryViewModel
{
	public DateTime Date        { get; set; }
	public bool     IsEmpty     { get; set; }
	public bool     SummaryOnly { get; set; }

	public double   ApneaHypopneaIndex { get; set; }
	public int      TotalApneaCount    { get; set; }
	public TimeSpan TotalTimeInApnea   { get; set; }

	public double HypopneaIndex { get; set; }
	public int    HypopneaCount { get; set; }
	
	public double ObstructiveIndex { get; set; }
	public int    ObstructiveCount { get; set; }
	
	public double ClearAirwayIndex { get; set; }
	public int    ClearAirwayCount { get; set; }
	
	public double UnclassifiedIndex { get; set; }
	public int    UnclassifiedCount { get; set; }

	public AhiHistoryViewModel( DailyReport day )
	{
		Date    = day.ReportDate.Date;
		IsEmpty = day.Sessions.Count == 0;

		var totalSleepHours = Math.Max( day.TotalSleepTime.TotalHours, 1.0 );

		if( IsEmpty )
		{
			return;
		}

		if( day.Events.Count == 0 && !day.HasDetailData )
		{
			ApneaHypopneaIndex = day.EventSummary.AHI;
			
			ObstructiveIndex = day.EventSummary.ApneaIndex;
			ObstructiveCount = (int)Math.Ceiling( ObstructiveIndex * totalSleepHours );
			
			HypopneaIndex = day.EventSummary.HypopneaIndex;
			HypopneaCount = (int)Math.Ceiling( HypopneaIndex * totalSleepHours );

			SummaryOnly = true;

			return;
		}
		
		var events          = day.Events.Where( x => EventTypes.Apneas.Contains( x.Type ) ).ToList();
		
		TotalApneaCount    = events.Count;
		ApneaHypopneaIndex = TotalApneaCount / totalSleepHours;
		TotalTimeInApnea   = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );

		HypopneaCount = events.Count( x => x.Type == EventType.Hypopnea );
		HypopneaIndex = HypopneaCount / totalSleepHours;

		ObstructiveCount = events.Count( x => x.Type == EventType.ObstructiveApnea );
		ObstructiveIndex = ObstructiveCount / totalSleepHours;

		ClearAirwayCount = events.Count( x => x.Type == EventType.ClearAirway );
		ClearAirwayIndex = ClearAirwayCount / totalSleepHours;

		UnclassifiedCount = events.Count( x => x.Type == EventType.UnclassifiedApnea );
		UnclassifiedIndex = UnclassifiedCount / totalSleepHours;
	}
	
	public AhiHistoryViewModel()
	{
		Date    = DateTime.Today;
		IsEmpty = true;
	}
}
