using System;
using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.Importers;

public static class OximetryEventGenerator
{
	public static List<ReportedEvent> GenerateEvents( Signal oxygen, Signal pulse )
	{
		var events = new List<ReportedEvent>();

		GenerateHypoxemiaEvents( oxygen, events );
		GenerateDesaturationEvents( oxygen, events );
		GeneratePulseEvents( pulse, events );

		return events;
	}
	
	private static void GeneratePulseEvents( Signal pulse, List<ReportedEvent> events )
	{
		//throw new System.NotImplementedException();
	}

	private static void GenerateDesaturationEvents( Signal signal, List<ReportedEvent> events )
	{
		const int    WINDOW_SIZE            = 300;
		const double MAX_EVENT_DURATION     = 120;
		const double MIN_EVENT_DURATION     = 1;
		const double DESATURATION_THRESHOLD = 3;
		
		if( signal.Samples.Count < WINDOW_SIZE * signal.FrequencyInHz )
		{
			return;
		}
		
		int    state         = 0;
		int    windowSize    = (int)(120 * signal.FrequencyInHz);
		double timeInterval  = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var    sourceData  = signal.Samples;
		double baseLine    = sourceData[ 0 ];
		double baselineSum = 0.0;
		
		for( int i = 0; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			// Update the sliding window average used as a baseline 
			{
				if( i >= windowSize )
				{
					baselineSum -= sourceData[ i - windowSize ];
				}

				baselineSum += sample;

				var count = Math.Min( i + 1, windowSize );
				baseLine = baselineSum / count;
			}

			switch( state )
			{
				case 0:
					if( sample <= baseLine - DESATURATION_THRESHOLD )
					{
						eventStart    = time;
						state         = 1;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( eventDuration >= MAX_EVENT_DURATION || (sample > baseLine - DESATURATION_THRESHOLD) )
					{
						if( eventDuration >= MIN_EVENT_DURATION )
						{
							var annotation = new ReportedEvent
							{
								Type      = EventType.Desaturation,
								StartTime = signal.StartTime.AddSeconds( eventStart ),
								Duration  = TimeSpan.FromSeconds( eventDuration )
							};

							if( !events.Any( x => ReportedEvent.TimesOverlap( x, annotation ) ) )
							{
								events.Add( annotation );
							}
						}

						state = 0;
					}
					break;
			}
		}

		if( state == 1 && eventDuration >= MIN_EVENT_DURATION )
		{
			var annotation = new ReportedEvent
			{
				Type      = EventType.Desaturation,
				StartTime = signal.StartTime.AddSeconds( eventStart ),
				Duration  = TimeSpan.FromSeconds( eventDuration )
			};

			if( !events.Any( x => ReportedEvent.TimesOverlap( x, annotation ) ) )
			{
				events.Add( annotation );
			}
		}
	}

	private static void GenerateHypoxemiaEvents( Signal signal, List<ReportedEvent> events )
	{
		const double HYPOXEMIA_THRESHOLD = 88;
		const double MAX_EVENT_DURATION  = 120;

		if( signal.Samples.Count == 0 )
		{
			return;
		}
		
		int    state        = 0;
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var    sourceData  = signal.Samples;
		
		for( int i = 0; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			switch( state )
			{
				case 0:
					if( sample <= HYPOXEMIA_THRESHOLD )
					{
						eventStart    = time - timeInterval;
						state         = 1;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( eventDuration >= MAX_EVENT_DURATION || (sample > HYPOXEMIA_THRESHOLD) )
					{
						var annotation = new ReportedEvent
						{
							Type      = EventType.Hypoxemia,
							StartTime = signal.StartTime.AddSeconds( eventStart ),
							Duration  = TimeSpan.FromSeconds( eventDuration )
						};

						events.Add( annotation );

						state = 0;
					}
					break;
			}
		}

		if( state == 1 )
		{
			var annotation = new ReportedEvent
			{
				Type      = EventType.Hypoxemia,
				StartTime = signal.StartTime.AddSeconds( eventStart ),
				Duration  = TimeSpan.FromSeconds( eventDuration )
			};

			events.Add( annotation );
		}
	}
}
