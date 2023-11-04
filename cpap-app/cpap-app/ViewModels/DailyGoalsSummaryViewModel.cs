using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyGoalsSummaryViewModel
{
	public DateTime Date         { get; set; }
	public int      DailyScore   { get; set; }
	public int      MaximumScore { get; set; }

	public List<DailyGoalItem> Items { get; set; } = new();

	public DailyGoalsSummaryViewModel( DailyReport day )
	{
		Date = day.ReportDate.Date;
		
		Items.Add( ScoreUsageHours( day ) );
		Items.Add( ScoreRespiratoryEvents( day ) );
		Items.Add( ScoreMaskSeal( day ) );
		Items.Add( ScoreMaskTime( day ) );
		Items.Add( ScoreOxygenSaturation( day ) );

		for( int i = 0; i < Items.Count; i++ )
		{
			DailyScore   += Items[ i ].DailyScore;
			MaximumScore += Items[ i ].MaximumScore;
		}
	}
	
	private static DailyGoalItem ScoreRespiratoryEvents( DailyReport day )
	{
		var eventCount = day.Events.Count( x => EventTypes.RespiratoryDisturbance.Contains( x.Type ) );
		var rdi        = eventCount / day.TotalSleepTime.TotalHours;
		var dailyScore = (int)MathUtil.Remap( 0, 19, 5, 0, rdi );
		
		return new DailyGoalItem
		{
			Label          = "Events per hour",
			Data           = rdi.ToString( "F1" ),
			DailyScore     = dailyScore,
			MaximumScore   = 5,
			IsHidden       = false
		};
	}

	private static DailyGoalItem ScoreOxygenSaturation( DailyReport day )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.SpO2 );
		if( stat == null )
		{
			return new DailyGoalItem
			{
				Label          = "Oxygen Saturation",
				Data           = "N/A",
				DailyScore     = 0,
				MaximumScore   = 5,
				IsHidden       = false
			};
		}

		var dailyScore = (int)MathUtil.Remap( 90, 98, 0, 5, stat.Average );
		
		return new DailyGoalItem
		{
			Label          = "Oxygen Saturation",
			Data           = stat.Average.ToString( "F1" ),
			DailyScore     = dailyScore,
			MaximumScore   = 5,
			IsHidden       = false
		};
	}
	
	private static DailyGoalItem ScoreMaskSeal( DailyReport day )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.LeakRate );
		if( stat == null )
		{
			return new DailyGoalItem
			{
				Label          = "Mask leak",
				Data           = "N/A",
				DailyScore     = 0,
				MaximumScore   = 10,
				IsHidden       = false
			};
		}

		var dailyScore = (int)MathUtil.Remap( 8, 24, 10, 0, stat.Percentile95 );
		
		return new DailyGoalItem
		{
			Label          = "Mask leak",
			Data           = stat.Percentile95.ToString( "F1" ),
			DailyScore     = dailyScore,
			MaximumScore   = 10,
			IsHidden       = false
		};
	}

	private static DailyGoalItem ScoreMaskTime( DailyReport day )
	{
		var maskTime   = day.TotalSleepTime.TotalHours / day.TotalTimeSpan.TotalHours;
		var dailyScore = (int)MathUtil.Remap( 0, 1, 0, 10, maskTime );
		
		return new DailyGoalItem
		{
			Label          = "Mask Time",
			Data           = $"{maskTime:P0}",
			DailyScore     = dailyScore,
			MaximumScore   = 10,
			IsHidden       = false
		};
	}
	
	private static DailyGoalItem ScoreUsageHours( DailyReport day )
	{
		var dailyScore = (int)MathUtil.Remap( 0, 7, 0, 70, day.TotalSleepTime.TotalHours );
		
		return new DailyGoalItem
		{
			Label          = "Usage hours",
			Data           = $"{day.TotalSleepTime:h\\:mm}",
			DailyScore     = dailyScore,
			MaximumScore   = 70,
			IsHidden       = false
		};
	}
}

public class DailyGoalItem
{
	public static readonly DailyGoalItem Empty = new DailyGoalItem() { IsHidden = true };

	public double Delay          { get; set; }
	public string Label          { get; set; } = "Goal";
	public string Data           { get; set; } = "N/A";
	public int    DailyScore     { get; set; }
	public int    MaximumScore   { get; set; }
	public bool   IsHidden       { get; set; }
}
