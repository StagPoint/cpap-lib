using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using cpaplib;

namespace cpapviewer.ViewModels;

public class DailyEventsViewModel
{
	public List<EventTypeSummary> Items { get; } = new();
	
	public DailyEventsViewModel( DailyReport day )
	{
		var events = day.Events;
		var types  = events.Select( x => x.Type ).Distinct();
		
		foreach( var type in types )
		{
			Items.Add( new EventTypeSummary( type, day.UsageTime, events ) );
		}
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

	public EventTypeSummary( EventType type, TimeSpan dailyTotal, List<ReportedEvent> dailyEvents )
	{
		Type = type;

		double totalSeconds = 0;

		foreach( var evt in dailyEvents )
		{
			if( evt.Type == type )
			{
				Events.Add( evt );
				TotalCount   += 1;
				totalSeconds += evt.Duration.TotalSeconds;
			}
		}

		PercentTotal = (totalSeconds / dailyTotal.TotalSeconds);
		TotalTime    = TimeSpan.FromSeconds( totalSeconds );
		IndexValue   = Events.Count / dailyTotal.TotalHours;
	}

	public override string ToString()
	{
		return $"{Type.ToName()} ({TotalCount})";
	}
}
