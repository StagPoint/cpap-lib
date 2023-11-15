using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.ViewModels;

public class EventSummaryViewModel
{
	public DailyReport Day        { get; set; }
	public int         TotalCount { get; set; }
	public TimeSpan    TotalTime  { get; set; }
	public double      IndexValue { get; set; }

	public List<EventGroupSummary> Indexes    { get; set; } = new();
	
	public List<EventTypeSummary>  Items   { get; set; } = new();
	
	public EventSummaryViewModel( DailyReport day )
	{
		Day = day;
		
		var events = day.Events;
		var types  = events.Select( x => x.Type ).Distinct();

		// Calculate the total time (in hours) for each SourceType
		var totalSleepTime = new Dictionary<SourceType, double>();
		foreach( var session in day.Sessions )
		{
			totalSleepTime.TryAdd( session.SourceType, 0 );
			totalSleepTime[ session.SourceType ] += session.Duration.TotalHours;
		}
		
		foreach( var type in types )
		{
			var summary = new EventTypeSummary( type, totalSleepTime, events );
			
			Items.Add( summary );
		}

		TotalCount = events.Count;
		TotalTime  = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );
		IndexValue = TotalCount / day.TotalSleepTime.TotalHours;
	}

	public EventSummaryViewModel( DailyReport day, Session session, EventType[]? eventTypeFilter = null )
	{
		Day = day;

		eventTypeFilter ??= EventTypes.RespiratoryDisturbance;

		var events = day.Events.Where( x =>
           x.SourceType == session.SourceType &&
           DateHelper.RangesOverlap( session.StartTime, session.EndTime, x.StartTime, x.StartTime + x.Duration ) &&
           eventTypeFilter.Contains( x.Type )
		).ToList();
		
		var types  = events.Select( x => x.Type ).Distinct();

		// Calculate the total time (in hours) for each SourceType
		var totalSleepTime = new Dictionary<SourceType, double>();
		totalSleepTime.TryAdd( session.SourceType, 0 );
		totalSleepTime[ session.SourceType ] += session.Duration.TotalHours;
		
		foreach( var type in types )
		{
			var summary = new EventTypeSummary( type, totalSleepTime, events );
			
			Items.Add( summary );
		}

		if( session.SourceType == SourceType.CPAP )
		{
			Indexes.Add( new EventGroupSummary( "Apnea/Hypopnea Index (AHI)", EventTypes.Apneas, session.Duration, events ) );

			if( events.Any( x => EventTypes.RespiratoryDisturbancesOnly.Contains( x.Type ) ) )
			{
				Indexes.Add( new EventGroupSummary( "Respiratory Disturbance (RDI)", EventTypes.RespiratoryDisturbance, session.Duration, events ) );
			}
		}

		TotalCount = events.Count;
		TotalTime  = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );
		IndexValue = TotalCount / session.Duration.TotalHours;
	}

	public EventSummaryViewModel( DailyReport day, params EventType[] filter )
	{
		Day = day;
		
		var events = day.Events.Where( x => filter.Contains( x.Type ) ).ToList();
		
		// Calculate the total time (in hours) for each SourceType
		var totalSleepTime = new Dictionary<SourceType, double>();
		foreach( var session in day.Sessions )
		{
			totalSleepTime.TryAdd( session.SourceType, 0 );
			totalSleepTime[ session.SourceType ] += session.Duration.TotalHours;
		}
		
		foreach( var type in filter )
		{
			var summary = new EventTypeSummary( type, totalSleepTime, events );

			Items.Add( summary );
		}

		TotalCount = events.Count;
		TotalTime  = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );
		IndexValue = TotalCount > 0 ? TotalCount / totalSleepTime[ events[ 0 ].SourceType ] : 0;
	}

	public void AddGroupSummary( string name, EventType[] groupFilter )
	{
		Indexes.Add( new EventGroupSummary( name, groupFilter, Day.TotalSleepTime, Day.Events ) );
	}
}

public class EventGroupSummary
{
	public string   Name         { get; set; }
	public int      TotalCount   { get; set; }
	public double   IndexValue   { get; set; }
	public double   PercentTime { get; set; }
	public TimeSpan TotalTime    { get; set; }

	public EventGroupSummary( string name, EventType[] groupFilter, TimeSpan dailyTotalTime, IEnumerable<ReportedEvent> events )
	{
		Name = name;

		double totalSeconds = 0;

		foreach( var evt in events )
		{
			if( groupFilter.Contains( evt.Type ) )
			{
				TotalCount   += 1;
				totalSeconds += evt.Duration.TotalSeconds;
			}
		}

		PercentTime = TotalCount > 0 ? (totalSeconds / dailyTotalTime.TotalSeconds) : 0;
		IndexValue  = TotalCount > 0 ? TotalCount / dailyTotalTime.TotalHours : 0;
		TotalTime   = TimeSpan.FromSeconds( totalSeconds );
	}

	public override string ToString()
	{
		return $"{Name} ({TotalCount})";
	}
}

public class EventTypeSummary
{
	public EventType Type         { get; set; }
	public int       TotalCount   { get; set; }
	public double    IndexValue   { get; set; }
	public double    PercentTime { get; set; }
	public TimeSpan  TotalTime    { get; set; }

	public List<ReportedEvent> Events { get; } = new List<ReportedEvent>();

	public EventTypeSummary( EventType type, Dictionary<SourceType, double> totalSleepTime, IEnumerable<ReportedEvent> events )
	{
		Type = type;

		double totalSeconds   = 0;
		double dailyTotalTime = 0;

		foreach( var evt in events )
		{
			if( evt.Type == type )
			{
				dailyTotalTime = totalSleepTime[ evt.SourceType ];
				
				Events.Add( evt );
				TotalCount   += 1;
				totalSeconds += evt.Duration.TotalSeconds;
			}
		}

		PercentTime = TotalCount > 0 ? totalSeconds / (dailyTotalTime * 3600) : 0;
		IndexValue  = TotalCount > 0 ? TotalCount / dailyTotalTime : 0;
		TotalTime   = TimeSpan.FromSeconds( totalSeconds );
	}

	public override string ToString()
	{
		return $"{Type.ToName()} ({TotalCount})";
	}
}
