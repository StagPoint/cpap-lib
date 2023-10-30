using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace cpaplib
{
	internal static class DerivedSignals
	{
		internal static void GenerateApneaIndexSignal( DailyReport day, Session session )
		{
			const double SAMPLE_INTERVAL = 2;

			var signalDuration = Math.Floor( session.Duration.TotalSeconds / SAMPLE_INTERVAL ) * SAMPLE_INTERVAL;
			Debug.Assert( signalDuration <= session.Duration.TotalSeconds, "Incorrect Signal length calculation" );

			// Compile a list of AHI-relevant events that happened during the Session
			var events = day.Events.Where( x => x.StartTime >= session.StartTime && x.StartTime + x.Duration <= session.EndTime ).ToList();
			events.RemoveAll( x => !EventTypes.Apneas.Contains( x.Type ) );
			
			// Events aren't stored in chronological order, but we want chronological order here
			events.Sort( ( a, b ) => a.StartTime.CompareTo( b.StartTime ) );

			// The stack will hold all events that have *started* within the one hour sliding window 
			var stack = new List<ReportedEvent>( events.Count );
			
			var samples = new List<double>( (int)Math.Ceiling( signalDuration / SAMPLE_INTERVAL ) );
			var signal = new Signal
			{
				Name              = SignalNames.AHI,
				FrequencyInHz     = 1.0 / SAMPLE_INTERVAL,
				MinValue          = 0,
				MaxValue          = 100,
				UnitOfMeasurement = "",
				Samples           = samples,
				StartTime         = session.StartTime,
				EndTime           = session.EndTime
			};

			session.Signals.Add( signal );

			// There are much more efficient ways of calculating this Signal, but this is straightforward and efficient enough for the purpose.
			
			var endTime = session.StartTime.AddSeconds( signalDuration  - SAMPLE_INTERVAL );
			for( DateTime time = session.StartTime; time < endTime; time = time.AddSeconds( SAMPLE_INTERVAL ) )
			{
				while( events.Count > 0 && events[ 0 ].StartTime <= time )
				{
					stack.Add( events[ 0 ] );
					events.RemoveAt( 0 );
				}

				while( stack.Count > 0 && stack[ 0 ].StartTime <= time.AddHours( -1 ) )
				{
					stack.RemoveAt( 0 );
				}

				samples.Add( stack.Count );
			}
			
			// Ensure that the Signal always drops to zero at the end. This is mostly cosmetic, tbh. 
			samples.Add( 0 );

			// It's not really a problem if the Signal length exceeds the Session length, except that this isn't a recorded Signal
			// so there's no good reason to have it extend the Session length to accomodate it. 
			Debug.Assert( Math.Abs( samples.Count * SAMPLE_INTERVAL - signalDuration ) <= 0.01, "Signal length exceeds Session length" );
		}
		
		internal static void GenerateMissingRespirationSignals( DailyReport day, Session session )
		{
			// We need the Flow Rate signal's data to calculate respiration rate, minute ventilation, inspiration and expiration times, etc.
			var flowRateSignal = session.GetSignalByName( SignalNames.FlowRate );
			if( flowRateSignal == null )
			{
				return;
			}

			// Extract breath information from the Flow Rate data, which can be used to derive other Signals.
			var breaths = BreathDetection.DetectBreaths( flowRateSignal, 0.75 );
			if( breaths == null || breaths.Count == 0 )
			{
				return;
			}

			// Generate the Respiration Rate signal if it doesn't already exist. This signal may be used to derive other signals as well. 
			if( session.GetSignalByName( SignalNames.RespirationRate ) == null )
			{
				GenerateRespirationRateSignal( session, breaths );
			}

			// Generate the Tidal Volume signal if it doesn't already exist. This signal may be used to derive other signals as well. 
			if( session.GetSignalByName( SignalNames.TidalVolume ) == null )
			{
				var respirationRateSignal = session.GetSignalByName( SignalNames.RespirationRate );
				
				GenerateTidalVolumeSignal( session, breaths, respirationRateSignal );
			}

			// Generate the Minute Ventilation signal if it is not already available.
			if( session.GetSignalByName( SignalNames.MinuteVent ) == null )
			{
				var tidalVolumeSignal     = session.GetSignalByName( SignalNames.TidalVolume );
				var respirationRateSignal = session.GetSignalByName( SignalNames.RespirationRate );
				
				GenerateMinuteVentilationSignal( session, tidalVolumeSignal, respirationRateSignal );
			}

			// Generate the Inspiration Time and Expiration Time signals if they are not available 
			if( session.GetSignalByName( SignalNames.InspirationTime ) == null )
			{
				GenerateRespirationTimeSignals( session, flowRateSignal, breaths );
			}
		}
		
		private static void GenerateMinuteVentilationSignal( Session session, Signal tidalVolume, Signal respirationRate )
		{
			Debug.Assert( tidalVolume.StartTime == respirationRate.StartTime, "Tidal Volume and Respiration Rate signals do not start at the same time" );
			
			Debug.Assert( Math.Abs( tidalVolume.FrequencyInHz - respirationRate.FrequencyInHz ) < float.Epsilon, "Tidal Volume and Respiration Rate signals do not have the same frequency" );
			
			var minuteVentilationSamples = new List<double>( tidalVolume.Samples.Count );
			var minuteVentilationSignal = new Signal
			{
				Name              = SignalNames.MinuteVent,
				FrequencyInHz     = tidalVolume.FrequencyInHz,
				MinValue          = 0,
				MaxValue          = 30.0,
				UnitOfMeasurement = "L/min",
				Samples           = minuteVentilationSamples,
				StartTime         = tidalVolume.StartTime,
				EndTime           = tidalVolume.EndTime,
			};

			for( int i = 0; i < tidalVolume.Samples.Count; i++ )
			{
				minuteVentilationSamples.Add( tidalVolume[ i ] * respirationRate[ i ] / 1000.0 );
			}

			session.AddSignal( minuteVentilationSignal );
		}

		private static void GenerateTidalVolumeSignal( Session session, List<BreathRecord> breaths, Signal respirationRate )
		{
			const double FREQUENCY = 0.5;
			const double INTERVAL  = 1.0 / FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var tidalVolumeSamples = new List<double>( (int)(totalDuration * FREQUENCY) );
			var tidalVolumeSignal = new Signal
			{
				Name              = SignalNames.TidalVolume,
				FrequencyInHz     = FREQUENCY,
				MinValue          = 0,
				MaxValue          = 4000.0,
				UnitOfMeasurement = "ml",
				Samples           = tidalVolumeSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			var currentBreathIndex = 0;

			var smoother = new MovingAverageCalculator( 3 );

			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( INTERVAL ) )
			{
				// Advance to the breath that overlaps the current timestamp
				while( currentBreathIndex < breaths.Count - 1 && breaths[ currentBreathIndex ].EndTime < currentTime )
				{
					currentBreathIndex += 1;
				} 

				var currentBreath = breaths[ currentBreathIndex ];

				var rr      = respirationRate[ tidalVolumeSamples.Count ];
				var vT      = currentBreath.TotalInspiration / currentBreath.InspirationLength / 60 * 1000;
				
				var clamped = MathUtil.Clamp( tidalVolumeSignal.MinValue, tidalVolumeSignal.MaxValue, vT / rr );

				// Using a moving "Root Mean Squares" value seems to get the results closer to the ResMed values,
				// which must indicate an issue somewhere else since the tidal volume calculations do not normally 
				// require this. 
				smoother.AddObservation( clamped * clamped );

				tidalVolumeSamples.Add( (rr < 1) ? 0 : Math.Sqrt( smoother.Average ) );
			}

			session.AddSignal( tidalVolumeSignal );
		}

		private static void GenerateRespirationRateSignal( Session session, List<BreathRecord> breaths )
		{
			const double FREQUENCY = 0.5;
			const double INTERVAL  = 1.0 / FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var respirationSamples = new List<double>( (int)(totalDuration * FREQUENCY) );
			var respirationSignal = new Signal
			{
				Name              = SignalNames.RespirationRate,
				FrequencyInHz     = FREQUENCY,
				MinValue          = 0,
				MaxValue          = 50,
				UnitOfMeasurement = "sec",
				Samples           = respirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			var window             = new List<BreathRecord>();
			int currentBreathIndex = 0;

			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( INTERVAL ) )
			{
				// Remove breaths that ended more than a minute ago
				while( window.Count > 0 && window[ 0 ].EndTime <= currentTime.AddSeconds( -60 ) )
				{
					window.RemoveAt( 0 );
				}

				// Add any breaths that overlap the current time 
				while( currentBreathIndex < breaths.Count - 1 && breaths[ currentBreathIndex ].StartInspiration <= currentTime )
				{
					window.Add( breaths[ currentBreathIndex++ ] );
				}

				if( currentBreathIndex < 3 )
				{
					respirationSamples.Add( 0 );
					continue;
				}

				var multiplier = (window.Count <= 1) ? 1.0 : (60.0 / (window[ window.Count - 1 ].EndTime - window[ 0 ].StartInspiration).TotalSeconds);
				
				// Output the number of breaths that overlap the last minute
				respirationSamples.Add( MathUtil.Clamp( respirationSignal.MinValue, respirationSignal.MaxValue, window.Count * multiplier ) );
			}
			
			// Add the Signal to the Session 
			session.AddSignal( respirationSignal );
		}

		internal static void GenerateRespirationTimeSignals( Session session, Signal flowRate, List<BreathRecord> breaths )
		{
			const double FREQUENCY = 0.5;
			const double INTERVAL  = 1.0 / FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var inspirationSamples = new List<double>( (int)(totalDuration * FREQUENCY) );
			var inspirationSignal = new Signal
			{
				Name              = SignalNames.InspirationTime,
				FrequencyInHz     = FREQUENCY,
				MinValue          = 0,
				MaxValue          = 30,
				UnitOfMeasurement = "sec",
				Samples           = inspirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			var expirationSamples = new List<double>( (int)(totalDuration * FREQUENCY) );
			var expirationSignal = new Signal
			{
				Name              = SignalNames.ExpirationTime,
				FrequencyInHz     = FREQUENCY,
				MinValue          = 0,
				MaxValue          = 30,
				UnitOfMeasurement = "sec",
				Samples           = expirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};
			
			var currentBreathIndex = 0;

			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( INTERVAL ) )
			{
				// Advance to the breath that overlaps the current timestamp
				while( breaths[ currentBreathIndex ].EndTime <= currentTime )
				{
					currentBreathIndex += 1;
				}

				var currentBreath     = breaths[ currentBreathIndex ];
				var inspirationLength = currentBreath.InspirationLength;
				var expirationLength  = currentBreath.ExpirationLength;

				// Because we're mapping a measurement that does not have a constant period to an output that does, 
				// we'll interpolate values from one breath to the next as the timeline progresses. 
				if( currentBreathIndex < breaths.Count - 1 )
				{
					var nextBreath = breaths[ currentBreathIndex + 1 ];

					var t = MathUtil.InverseLerp(
						currentBreath.StartInspiration.ToFileTimeUtc(),
						nextBreath.StartInspiration.ToFileTimeUtc(),
						currentTime.ToFileTimeUtc()
					);

					inspirationLength = MathUtil.Lerp( currentBreath.InspirationLength, nextBreath.InspirationLength, t );
					expirationLength  = MathUtil.Lerp( currentBreath.ExpirationLength,  nextBreath.ExpirationLength,  t );
				}

				inspirationSamples.Add( MathUtil.Clamp( 0, 30, inspirationLength ) );
				expirationSamples.Add( MathUtil.Clamp( 0,  30, expirationLength ) );
			}
		
			// Add the signals to the Session 
			session.AddSignal( inspirationSignal );
			session.AddSignal( expirationSignal );
		}
	}
}
