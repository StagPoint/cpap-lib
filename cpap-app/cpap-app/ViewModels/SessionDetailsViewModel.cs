using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.ViewModels;

public class SessionDetailsViewModel
{
	public DailyReport              Day             { get; set; }
	public Session                  Session         { get; set; }
	public DailyStatisticsViewModel Statistics      { get; set; }
	public EventSummaryViewModel    Events          { get; set; }
	public EventSummaryViewModel?   OxygenEvents    { get; set; } = null;
	public DataDistribution?        OxygenSummary   { get; set; } = null;
	public DataDistribution?        PulseSummary    { get; set; } = null;
	public EventSummaryViewModel?   PulseEvents     { get; set; } = null;
	public bool                     HasOximetryData { get; set; } = false;


	public SessionDetailsViewModel( DailyReport day, Session session )
	{
		var eventTypes = session.SourceType == SourceType.PulseOximetry
			? EventTypes.OxygenSaturation.Concat( EventTypes.Pulse ).ToArray()
			: EventTypes.RespiratoryDisturbance;
			
		Day        = day;
		Session    = session;
		Events     = new EventSummaryViewModel( day, session, eventTypes );
		Statistics = GenerateStatistics( session );

		if( session.SourceType == SourceType.PulseOximetry )
		{
			HasOximetryData = true;
			
			OxygenEvents    = new EventSummaryViewModel( day, session, EventTypes.OxygenSaturation );
			PulseEvents     = new EventSummaryViewModel( day, session, EventTypes.Pulse );

			var sessionList = new List<Session> { session };

			OxygenSummary = DataDistribution.GetDataDistribution(
				sessionList,
				SignalNames.SpO2,
				new[]
				{
					new DataDistribution.RangeDefinition( "< 90 %",    89.5 ),
					new DataDistribution.RangeDefinition( "90% - 94%", 94.5 ),
					new DataDistribution.RangeDefinition( "\u2265 95 %",   double.MaxValue )
				} );

			PulseSummary = DataDistribution.GetDataDistribution(
				sessionList,
				SignalNames.Pulse,
				new[]
				{
					new DataDistribution.RangeDefinition( "< 50 bpm",    49.5 ),
					new DataDistribution.RangeDefinition( "50-99 bpm",   99.5 ),
					new DataDistribution.RangeDefinition( "100-120 bpm", 120.5 ),
					new DataDistribution.RangeDefinition( "\u2265 120 bpm",  double.MaxValue )
				}
			);
		}
	}
	
	private static DailyStatisticsViewModel GenerateStatistics( Session session )
	{
		List<SignalStatistics> stats = new();
		
		foreach( var signal in session.Signals )
		{
			if( signal.MinValue < 0 && signal.MaxValue > 0 )
			{
				// Don't generate statistics for Signals that cross the zero line (such as Flow Rate)
				continue;
			}
			
			var calc = new SignalStatCalculator();
			calc.AddSignal( signal );

			stats.Add( calc.CalculateStats() );
		}

		return new DailyStatisticsViewModel( stats, false );
	}
}
