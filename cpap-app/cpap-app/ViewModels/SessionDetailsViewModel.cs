using System.Collections.Generic;

using cpaplib;

namespace cpap_app.ViewModels;

public class SessionDetailsViewModel
{
	public DailyReport Day     { get; set; }
	public Session     Session { get; set; }
	
	public DailyStatisticsViewModel Statistics { get; set; }
	
	public EventSummaryViewModel Events { get; set; }
	
	public SessionDetailsViewModel( DailyReport day, Session session )
	{
		Day     = day;
		Session = session;
		Events  = new EventSummaryViewModel( day, session );
		Statistics = GenerateStatistics( session );
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
