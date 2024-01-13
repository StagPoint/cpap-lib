using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class CustomEventGenerator
	{
		public static void GenerateEvents( DailyReport day, CpapImportSettings importSettings )
		{
			// Some machines flag some of these events types internally, but our event flagging won't generate
			// any events that overlap an existing event so we can still flag events with a custom threshold
			// level without creating a conflict. 

			if( importSettings.FlagFlowReductions )
			{
				GenerateFlowReductionEvents( day, importSettings );
			}

			if( importSettings.FlagLargeLeaks )
			{
				GenerateLeakEvents( day, importSettings );
			}

			if( importSettings.FlagFlowLimits )
			{
				GenerateFlowLimitEvents( day, importSettings );
			}
		}

		private static void GenerateFlowReductionEvents( DailyReport day, CpapImportSettings importSettings )
		{
			foreach( var session in day.Sessions )
			{
				var signal = session.GetSignalByName( SignalNames.FlowRate );
				if( signal == null )
				{
					return;
				}

				var absFlow      = signal.Samples.Select( Math.Abs ).ToArray();
				var filteredFlow = ButterworthFilter.Filter( absFlow, signal.FrequencyInHz, 1 );
				var calc         = new MovingAverageCalculator( (int)(importSettings.FlowReductionWindowSize * signal.FrequencyInHz) );

				var interval     = 1.0 / signal.FrequencyInHz;
				var state        = 0;
				var startIndex   = 0;
				var threshold    = 0.0;
				var lastRecovery = 0;

				for( int i = 0; i < signal.Samples.Count; i++ )
				{
					var sample = filteredFlow[ i ];

					calc.AddObservation( sample );

					if( !calc.HasFullPeriod )
					{
						continue;
					}

					var rms = calc.Average + calc.StandardDeviation;

					if( sample >= rms * 2 )
					{
						lastRecovery = i;
					}

					switch( state )
					{
						case 0:
							threshold = rms * importSettings.FlowReductionThreshold;
							if( sample <= threshold && (i - lastRecovery) * interval >= importSettings.FlowReductionArousalDelay )
							{
								startIndex = i;
								state      = 1;
							}
							break;

						case 1:
							if( sample >= threshold )
							{
								var duration = (i - startIndex) * interval;

								if( duration >= importSettings.FlowReductionMinimumDuration )
								{
									// Note that all machine-generated events "start" at the end of the event.
									// TODO: It might be better to rename ReportedEvent.StartTime to ReportedEvent.MarkerTime
									var eventStart = signal.StartTime.AddSeconds( i * interval );

									var newEvent = new ReportedEvent
									{
										Type      = EventType.FlowReduction,
										StartTime = eventStart,
										Duration  = TimeSpan.FromSeconds( duration ),
									};

									// Because this is an application-generated event, allow machine-generated events to take
									// precedence by not generating any events that overlap in time. 
									if( !day.Events.Any( x => ReportedEvent.TimesOverlap( x, newEvent ) ) )
									{
										day.Events.Add( newEvent );
									}
								}

								state = 0;
							}
							break;
					}
				}
			}
		}

		private static void GenerateFlowLimitEvents( DailyReport day, CpapImportSettings importSettings )
		{
			List<ReportedEvent> flowLimitEvents = new List<ReportedEvent>();
			
			foreach( var session in day.Sessions )
			{
				var signal = session.GetSignalByName( SignalNames.FlowLimit );
				if( signal != null )
				{
					flowLimitEvents.AddRange( Annotate( EventType.FlowLimitation, signal, importSettings.FlowLimitMinimumDuration, importSettings.FlowLimitThreshold, false ) );
				}
			}
			
			// HACK: Don't add any flow limitation events that coincide with a RERA event, since the two are related
			// and we don't want to double-count the events when calculating the RDI
			var eventTypeList = EventTypes.RespiratoryDisturbance;
			foreach( var flowLimit in flowLimitEvents )
			{
				if( !day.Events.Any( x => eventTypeList.Contains( x.Type ) && ReportedEvent.TimesOverlap( x, flowLimit ) ) )
				{
					day.Events.Add( flowLimit );
				}
			}
		}

		private static void GenerateLeakEvents( DailyReport day, CpapImportSettings importSettings )
		{
			foreach( var session in day.Sessions )
			{
				var signal = session.GetSignalByName( SignalNames.LeakRate );
				if( signal != null )
				{
					day.Events.AddRange( Annotate( EventType.LargeLeak, signal, 1, importSettings.LargeLeakThreshold, false ) );
				}
			}
		}

		private static List<ReportedEvent> Annotate( EventType eventType, Signal signal, double minDuration, double redLine, bool interpolateSamples = true, double rampTime = 60 )
		{
			List<ReportedEvent> events = new List<ReportedEvent>();
			
			int    state      = 0;
			double eventStart = -1;

			var sourceData     = signal.Samples;
			var sampleInterval = 1.0 / signal.FrequencyInHz;
			
			for( int i = 1; i < sourceData.Count; i++ )
			{
				var sample = sourceData[ i ];
				var time   = i * sampleInterval;

				// No events are generated during the first "Ramp Time" seconds of each Session. 
				if( time < rampTime )
				{
					continue;
				}

				switch( state )
				{
					case 0:
					{
						if( sample > redLine )
						{
							var lastSample = sourceData[ i - 1 ];
							var t          = interpolateSamples ? MathUtil.InverseLerp( lastSample, sample, redLine ) : 1.0;

							eventStart = time - (1.0 - t) * sampleInterval;
							state      = 1;
						}
						
						break;
					}

					case 1:
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = interpolateSamples ? MathUtil.InverseLerp( lastSample, sample, redLine ) : 1.0;
						var eventEnd   = time - (1.0 - t) * sampleInterval;
						var duration   = eventEnd - eventStart;

						if( sample <= redLine )
						{
							if( duration >= minDuration )
							{
								var annotation = new ReportedEvent
								{
									Type      = eventType,
									StartTime = signal.StartTime.AddSeconds( eventStart ),
									Duration  = TimeSpan.FromSeconds( duration ),
								};

								events.Add( annotation );
							}

							state = 0;
						}
						
						break;
					}
				}
			}

			if( state == 1 )
			{
				var annotation = new ReportedEvent()
				{
					Type      = eventType,
					StartTime = signal.StartTime.AddSeconds( eventStart ),
					Duration  = TimeSpan.FromSeconds( sourceData.Count * sampleInterval - eventStart ),
				};

				events.Add( annotation );
			}

			return events;
		}
	}
}
