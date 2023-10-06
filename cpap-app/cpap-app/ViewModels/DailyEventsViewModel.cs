using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyEventsViewModel
{
	public int      TotalCount { get; set; }
	public TimeSpan TotalTime  { get; set; }
	public double   IndexValue { get; set; }

	public List<EventGroupSummary> Indexes    { get; set; } = new();
	
	public List<EventTypeSummary>  Items   { get; set; } = new();

	private DailyReport _day;

	public DailyEventsViewModel( DailyReport day )
	{
		_day = day;
		
		var events = day.Events;
		var types  = events.Select( x => x.Type ).Distinct();
		
		foreach( var type in types )
		{
			var summary = new EventTypeSummary( type, day.UsageTime, events );
			
			Items.Add( summary );
		}

		TotalCount = events.Count;
		TotalTime  = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );
		IndexValue = TotalCount / day.UsageTime.TotalHours;

		// Sort the top level nodes alphabetically
		Items.Sort( ( lhs, rhs ) => String.Compare( lhs.Type.ToString(), rhs.Type.ToString(), StringComparison.Ordinal ) );
	}

	public DailyEventsViewModel( DailyReport day, params EventType[] filter )
	{
		_day = day;
		
		var events = day.Events.Where( x => filter.Contains( x.Type ) ).ToList();
		var types  = events.Select( x => x.Type ).Distinct();
		
		foreach( var type in types )
		{
			var summary = new EventTypeSummary( type, day.UsageTime, events );

			Items.Add( summary );
		}

		TotalCount = events.Count;
		TotalTime  = TimeSpan.FromSeconds( events.Sum( x => x.Duration.TotalSeconds ) );
		IndexValue = TotalCount / day.UsageTime.TotalHours;

		Items.Sort( ( lhs, rhs ) => String.Compare( lhs.Type.ToString(), rhs.Type.ToString(), StringComparison.Ordinal ) );
	}

	public void AddGroupSummary( string name, EventType[] groupFilter )
	{
		Indexes.Add( new EventGroupSummary( name, groupFilter, _day.UsageTime, _day.Events ) );
	}
}

public class EventGroupSummary
{
	public string   Name         { get; set; }
	public int      TotalCount   { get; set; }
	public double   IndexValue   { get; set; }
	public double   PercentTotal { get; set; }
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

		PercentTotal = (totalSeconds / dailyTotalTime.TotalSeconds);
		TotalTime    = TimeSpan.FromSeconds( totalSeconds );
		IndexValue   = TotalCount / dailyTotalTime.TotalHours;
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
	public double    PercentTotal { get; set; }
	public TimeSpan  TotalTime    { get; set; }

	public List<ReportedEvent> Events { get; } = new List<ReportedEvent>();

	public EventTypeSummary( EventType type, TimeSpan dailyTotalTime, IEnumerable<ReportedEvent> events )
	{
		Type = type;

		double totalSeconds = 0;

		foreach( var evt in events )
		{
			if( evt.Type == type )
			{
				Events.Add( evt );
				TotalCount   += 1;
				totalSeconds += evt.Duration.TotalSeconds;
			}
		}

		PercentTotal = (totalSeconds / dailyTotalTime.TotalSeconds);
		TotalTime    = TimeSpan.FromSeconds( totalSeconds );
		IndexValue   = TotalCount / dailyTotalTime.TotalHours;
	}

	public override string ToString()
	{
		return $"{Type.ToName()} ({TotalCount})";
	}
}
