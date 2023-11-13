using System.Collections.Generic;

using cpaplib;

namespace cpap_app.ViewModels;

public class SessionDetailsViewModel
{
	public DailyReport Day     { get; set; }
	public Session     Session { get; set; }
	
	public List<SignalStatistics> Statistics { get; set; } = new();
	
	public EventSummaryViewModel Events { get; set; }
	
	public SessionDetailsViewModel( DailyReport day, Session session )
	{
		Day     = day;
		Session = session;

		GenerateStatistics( session );
	}
	
	private void GenerateStatistics( Session session )
	{
		foreach( var signal in session.Signals )
		{
			if( signal.MinValue < 0 && signal.MaxValue > 0 )
			{
				// Don't generate statistics for Signals that cross the zero line (such as Flow Rate)
				continue;
			}
			
			var calc = new SignalStatCalculator();
			calc.AddSignal( signal );

			Statistics.Add( calc.CalculateStats() );
		}
	}
}
