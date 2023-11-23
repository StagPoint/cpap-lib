using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyScoreSummaryViewModel
{
	public DateTime Date         { get; set; }
	public int      DailyScore   { get; set; }
	public int      MaximumScore { get; set; }

	public List<DailyScoreItemViewModel> Items { get; set; } = new();

	public DailyScoreSummaryViewModel( DailyReport day, DailyReport? previousDay )
	{
		Date = day.ReportDate.Date;
		
		Items.Add( ScoreUsageHours( day, previousDay, 0, 7, 60 ) );
		Items.Add( ScoreRespiratoryEvents( day, previousDay, 20, 0, 10 ) );
		Items.Add( ScoreOxygenSaturation( day, previousDay, 90, 98, 10 ) );
		Items.Add( ScoreMaskSeal( day, previousDay, 24, 8, 10 ) );
		Items.Add( ScoreSessionCount( day, previousDay, 10, 0, 10 ) );

		for( int i = 0; i < Items.Count; i++ )
		{
			DailyScore   += Items[ i ].DailyScore;
			MaximumScore += Items[ i ].MaximumScore;
		}
	}
	
	private static DailyScoreItemViewModel ScoreRespiratoryEvents( DailyReport day, DailyReport? previousDay, double minValue, double maxValue, int weight )
	{
		var eventCount = day.Events.Count( x => EventTypes.RespiratoryDisturbance.Contains( x.Type ) );
		var rdi        = eventCount / day.TotalSleepTime.TotalHours;
		var dailyScore = (int)MathUtil.Remap( minValue, maxValue, 0, weight, rdi );

		var isImprovement = previousDay == null;

		if( previousDay != null )
		{
			var prevEventCount = previousDay.Events.Count( x => EventTypes.RespiratoryDisturbance.Contains( x.Type ) );
			var prevRDI        = prevEventCount / previousDay.TotalSleepTime.TotalHours;

			isImprovement = rdi <= prevRDI;
		}

		return new DailyScoreItemViewModel
		{
			Label         = "Resp. events (per hour)",
			Data          = rdi.ToString( "F1" ),
			DailyScore    = dailyScore,
			MaximumScore  = weight,
			IsHidden      = false,
			IsImprovement = isImprovement,
		};
	}

	private static DailyScoreItemViewModel ScoreOxygenSaturation( DailyReport day, DailyReport? previousDay, double minValue, double maxValue, int weight )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.SpO2 );
		if( stat == null )
		{
			return new DailyScoreItemViewModel
			{
				Label         = "Oxygen saturation (%)",
				Data          = "N/A",
				DailyScore    = 0,
				MaximumScore  = weight,
				IsHidden      = false,
				IsImprovement = false,
			};
		}

		var averageOxygen = (int)Math.Round( stat.Average );

		var dailyScore = (int)MathUtil.Remap( minValue, maxValue, 0, weight, averageOxygen );

		var isImprovement = previousDay == null;

		var prevStat = previousDay?.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.SpO2 );
		if( prevStat != null )
		{
			isImprovement = (int)stat.Average >= (int)prevStat.Average;
		}

		return new DailyScoreItemViewModel
		{
			Label         = "Oxygen saturation (%)",
			Data          = $"{averageOxygen}",
			DailyScore    = dailyScore,
			MaximumScore  = weight,
			IsHidden      = false,
			IsImprovement = isImprovement,
		};
	}
	
	private static DailyScoreItemViewModel ScoreMaskSeal( DailyReport day, DailyReport? previousDay, double minValue, double maxValue, int weight )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.LeakRate );
		if( stat == null )
		{
			return new DailyScoreItemViewModel
			{
				Label         = "Mask leak (L/Min)",
				Data          = "N/A",
				DailyScore    = 0,
				MaximumScore  = weight,
				IsHidden      = false,
				IsImprovement = false,
			};
		}

		var dailyScore = (int)MathUtil.Remap( minValue, maxValue, 0, weight, stat.Percentile95 );

		var isImprovement = previousDay == null;

		if( previousDay != null )
		{
			var prevStat = previousDay.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.LeakRate );
			isImprovement = prevStat != null && stat.Percentile95 <= prevStat.Percentile95;
		}

		return new DailyScoreItemViewModel
		{
			Label         = "Mask leak (L/Min)",
			Data          = stat.Percentile95.ToString( "F1" ),
			DailyScore    = dailyScore,
			MaximumScore  = weight,
			IsHidden      = false,
			IsImprovement = isImprovement,
		};
	}

	private static DailyScoreItemViewModel ScoreSessionCount( DailyReport day, DailyReport? previousDay, double minValue, double maxValue, int weight )
	{
		var sessionCount = day.Sessions.Count( x => x.SourceType == SourceType.CPAP );
		var dailyScore   = (int)MathUtil.Remap( minValue, maxValue, 0, weight, sessionCount );

		var isImprovement = previousDay == null;

		if( previousDay != null )
		{
			var prevSessionCount = previousDay.Sessions.Count( x => x.SourceType == SourceType.CPAP );
			isImprovement = sessionCount <= prevSessionCount;
		}

		return new DailyScoreItemViewModel
		{
			Label         = "Session count",
			Data          = $"{sessionCount}",
			DailyScore    = dailyScore,
			MaximumScore  = weight,
			IsHidden      = false,
			IsImprovement = isImprovement,
		};
	}
	
	private static DailyScoreItemViewModel ScoreUsageHours( DailyReport day, DailyReport? previousDay, double minValue, double maxValue, int weight )
	{
		var dailyScore = (int)MathUtil.Remap( minValue, maxValue, 0, weight, day.TotalSleepTime.TotalHours );

		var isImprovement = (previousDay == null) || day.TotalSleepTime.TotalHours >= previousDay.TotalSleepTime.TotalHours;

		return new DailyScoreItemViewModel
		{
			Label         = "PAP therapy time",
			Data          = $"{day.TotalSleepTime:h\\:mm}",
			DailyScore    = dailyScore,
			MaximumScore  = weight,
			IsHidden      = false,
			IsImprovement = isImprovement,
		};
	}
}

public class DailyScoreItemViewModel
{
	public static readonly DailyScoreItemViewModel Empty = new DailyScoreItemViewModel() { IsHidden = true };

	public double Delay         { get; set; }
	public string Label         { get; set; } = "Goal";
	public string Data          { get; set; } = "N/A";
	public int    DailyScore    { get; set; }
	public int    MaximumScore  { get; set; }
	public bool   IsHidden      { get; set; }
	public bool   IsImprovement { get; set; } = true;
}
