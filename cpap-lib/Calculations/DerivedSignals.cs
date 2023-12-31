﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
// ReSharper disable UseIndexFromEndExpression

namespace cpaplib
{
	[SuppressMessage( "ReSharper", "MergeConditionalExpression" )]
	public static class DerivedSignals
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

				samples.Add( Math.Min( stack.Count, signal.MaxValue ) );
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
			// Note that it is assumed that the Flow Rate signal's physical unit is "Liters per Minute"
			var flowRateSignal = session.GetSignalByName( SignalNames.FlowRate );
			if( flowRateSignal == null )
			{
				return;
			}

			// Extract breath information from the Flow Rate data, which can be used to derive other Signals.
			var breaths = BreathDetection.DetectBreaths( flowRateSignal, useVariableBaseline: true );
			if( breaths == null || breaths.Count == 0 )
			{
				return;
			}

			// Generate the Respiration Rate signal if it doesn't already exist. This signal may be used to derive other signals as well. 
			var respirationRateSignal = session.GetSignalByName( SignalNames.RespirationRate );
			if( respirationRateSignal == null )
			{
				respirationRateSignal = GenerateRespirationRateSignal( breaths );

				if( respirationRateSignal != null )
				{
					session.AddSignal( respirationRateSignal );
				}
			}

			// Generate the Tidal Volume signal if it doesn't already exist. This signal may be used to derive other signals as well.
			var tidalVolumeSignal = session.GetSignalByName( SignalNames.TidalVolume );
			if( tidalVolumeSignal == null )
			{
				tidalVolumeSignal = GenerateTidalVolumeSignal( breaths );
				
				if( tidalVolumeSignal != null )
				{
					session.AddSignal( tidalVolumeSignal );
				}
			}

			// Generate the Minute Ventilation signal if it is not already available.
			var minuteVentilationSignal = session.GetSignalByName( SignalNames.MinuteVent );
			if( minuteVentilationSignal == null )
			{
				minuteVentilationSignal = GenerateMinuteVentilationSignal( tidalVolumeSignal, respirationRateSignal );

				if( minuteVentilationSignal != null )
				{
					session.AddSignal( minuteVentilationSignal );
				}
			}

			// Generate the Inspiration Time, Expiration Time, and I:E Ratio signals if they are not available 
			(Signal inspirationTimeSignal, Signal expirationTimeSignal, Signal inspirationRatioSignal) = GenerateRespirationTimeSignals( breaths );

			if( session.GetSignalByName( SignalNames.InspirationTime ) == null )
			{
				session.AddSignal( inspirationTimeSignal );
				session.AddSignal( expirationTimeSignal );
			}

			if( session.GetSignalByName( SignalNames.InspToExpRatio ) == null )
			{
				session.AddSignal( inspirationRatioSignal );
			}
		}
		
		[Obsolete]
		private static Signal GenerateInspirationToExpirationRatioSignal( Signal flowRate )
		{
			const int    HISTORY_WINDOW_SIZE = 15;
			const double OUTPUT_FREQUENCY    = 0.5;
			const double OUTPUT_INTERVAL     = 1.0 / OUTPUT_FREQUENCY;
			
			double flowSampleFrequency = flowRate.FrequencyInHz;
			double flowSampleInterval  = 1.0 / flowSampleFrequency;
			
			var outputSamples = new List<double>();
			var outputSignal = new Signal
			{
				Name              = SignalNames.InspToExpRatio,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 8.0,
				UnitOfMeasurement = "",
				Samples           = outputSamples,
				StartTime         = flowRate.StartTime,
				EndTime           = flowRate.EndTime,
			};

			int flowWindowSize = (int)(HISTORY_WINDOW_SIZE * flowSampleFrequency);

			double inspiratoryTime = 0.0;
			double expiratoryTime  = 0.0;
			double lastOutputTime  = int.MinValue;
			int    lastSign        = -1;
			int    flipCount       = 0;

			for( int i = 0; i < flowRate.Samples.Count; i++ )
			{
				var sample      = flowRate[ i ];
				var currentTime = i * flowSampleInterval;
				var emitSample  = (currentTime - lastOutputTime) >= OUTPUT_INTERVAL;

				inspiratoryTime += (sample > 0) ? OUTPUT_INTERVAL : 0;
				expiratoryTime  += (sample <= 0) ? OUTPUT_INTERVAL : 0;

				var sign = (sample > 0) ? 1 : -1;
				if( sign != lastSign )
				{
					flipCount += 1;
					lastSign  =  sign;
				}

				// Wait until we have tracked at least one full breath cycle
				if( flipCount < 3 )
				{
					if( emitSample )
					{
						outputSamples.Add( 0 );
						lastOutputTime = currentTime;
					}
					
					continue;
				}

				// Maintain the window by removing old entries 
				if( i > flowWindowSize )
				{
					var historySample = flowRate[ i - flowWindowSize ];
					if( historySample > 0 )
					{
						inspiratoryTime -= (sample > 0) ? OUTPUT_INTERVAL : 0;
						expiratoryTime  -= (sample <= 0) ? OUTPUT_INTERVAL : 0;
					}
				}

				if( !emitSample )
				{	
					continue;
				}

				var output = expiratoryTime / inspiratoryTime;

				outputSamples.Add( output );
				lastOutputTime = currentTime;
			}

			while( flowRate.StartTime.AddSeconds( lastOutputTime ) < outputSignal.EndTime )
			{
				outputSamples.Add( 0 );
				lastOutputTime += OUTPUT_INTERVAL;
			}

			return outputSignal;
		}

		public static Signal GenerateTidalVolumeSignal( List<BreathRecord> breaths )
		{
			const double OUTPUT_FREQUENCY = 0.5;
			const double OUTPUT_INTERVAL  = 1.0 / OUTPUT_FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var outputSamples = new List<double>( (int)(totalDuration * OUTPUT_FREQUENCY) );
			var outputSignal = new Signal
			{
				Name              = SignalNames.TidalVolume,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 4000.0,
				UnitOfMeasurement = "ml",
				Samples           = outputSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			int currentBreathIndex = 0;
			var currentBreath      = breaths[ 0 ];

			// Using a sliding window average allows us to calculate and use the instantaneous "breaths per minute" 
			// value at each given point in time without having a "stepped" output.  
			int smootherPeriod      = (int)(60.0 / OUTPUT_INTERVAL);
			var respirationSmoother = new MovingAverageCalculator( smootherPeriod );
			var flowSmoother        = new MovingAverageCalculator( smootherPeriod );
			
			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( OUTPUT_INTERVAL ) )
			{
				while( currentBreath.EndTime < currentTime )
				{
					currentBreathIndex += 1;
					currentBreath      =  breaths[ currentBreathIndex ];
				}

				var instantaneousBPM = 60.0 / currentBreath.TotalCycleTime;
				respirationSmoother.AddObservation( instantaneousBPM );

				var averageFlowOverBreathCycle = currentBreath.TotalFlow / currentBreath.TotalCycleTime;
				flowSmoother.AddObservation( averageFlowOverBreathCycle );

				var respirationRate = (respirationSmoother.Average + respirationSmoother.StandardDeviation);
				var tidalVolume     = (flowSmoother.Average + flowSmoother.StandardDeviation) / respirationRate / 60 * 1000;

				var outputValue = respirationRate > 0 ? tidalVolume : 0;
				Debug.Assert( !double.IsNaN( outputValue ), "Unexpected NaN value in output" );
				outputSamples.Add( outputValue );

				outputSignal.EndTime = currentTime;
			}
			
			return outputSignal;
		}

		private static Signal GenerateMinuteVentilationSignal( Signal tidalVolume, Signal respirationRate )
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
				var rrIndex = Math.Min( i, respirationRate.Samples.Count - 1 );
				minuteVentilationSamples.Add( tidalVolume[ i ] * respirationRate[ rrIndex ] / 1000.0 );
			}

			return minuteVentilationSignal;
		}

		public static Signal GenerateRespirationRateSignal( List<BreathRecord> breaths )
		{
			const double OUTPUT_FREQUENCY = 0.5;
			const double OUTPUT_INTERVAL  = 1.0 / OUTPUT_FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var respirationSamples = new List<double>( (int)(totalDuration * OUTPUT_FREQUENCY) );
			var respirationSignal = new Signal
			{
				Name              = SignalNames.RespirationRate,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 50,
				UnitOfMeasurement = "sec",
				Samples           = respirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			int currentBreathIndex = 0;
			var currentBreath      = breaths[ 0 ];

			// Using a sliding window average allows us to calculate and use the instantaneous "breaths per minute" 
			// value at each given point in time without having a "stepped" output.  
			int smootherPeriod = (int)(60.0 / OUTPUT_INTERVAL);
			var smoother       = new MovingAverageCalculator( smootherPeriod );
			
			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( OUTPUT_INTERVAL ) )
			{
				while( currentBreath.EndTime < currentTime )
				{
					currentBreathIndex += 1;
					currentBreath      =  breaths[ currentBreathIndex ];
				}

				var currentBreathValue = 60.0 / currentBreath.TotalCycleTime;
				smoother.AddObservation( currentBreathValue );

				if( smoother.Count > smootherPeriod / 4 )
				{
					Debug.Assert( !double.IsNaN( smoother.Average ), "Unexpected NaN value in output" );
					respirationSamples.Add( smoother.Average );
				}
				else
				{
					// Output zero while waiting for the smoother to accumulate enough samples to 
					// have a meaningful average. 
					respirationSamples.Add( 0 );
				}

				respirationSignal.EndTime = currentTime;
			}
			
			return respirationSignal;
		}

		internal static ( Signal, Signal, Signal ) GenerateRespirationTimeSignals( List<BreathRecord> breaths )
		{
			const double OUTPUT_FREQUENCY = 0.5;
			const double OUTPUT_INTERVAL  = 1.0 / OUTPUT_FREQUENCY;
			
			var firstBreath   = breaths[ 0 ];
			var lastBreath    = breaths[ breaths.Count - 1 ];
			var totalDuration = (lastBreath.EndTime - firstBreath.StartInspiration).TotalSeconds;

			var inspirationSamples = new List<double>( (int)(totalDuration * OUTPUT_FREQUENCY) );
			var inspirationSignal = new Signal
			{
				Name              = SignalNames.InspirationTime,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 30,
				UnitOfMeasurement = "sec",
				Samples           = inspirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			var expirationSamples = new List<double>( (int)(totalDuration * OUTPUT_FREQUENCY) );
			var expirationSignal = new Signal
			{
				Name              = SignalNames.ExpirationTime,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 30,
				UnitOfMeasurement = "sec",
				Samples           = expirationSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};

			var ratioSamples = new List<double>( (int)(totalDuration * OUTPUT_FREQUENCY) );
			var ratioSignal = new Signal
			{
				Name              = SignalNames.InspToExpRatio,
				FrequencyInHz     = OUTPUT_FREQUENCY,
				MinValue          = 0,
				MaxValue          = 8.0,
				UnitOfMeasurement = "",
				Samples           = ratioSamples,
				StartTime         = firstBreath.StartInspiration,
				EndTime           = lastBreath.EndTime,
			};


			var currentBreathIndex = 0;

			const int smootherPeriod      = (int)(30.0 / OUTPUT_INTERVAL);
			var       inspirationSmoother = new MovingAverageCalculator( smootherPeriod );
			var       expirationSmoother  = new MovingAverageCalculator( smootherPeriod );

			for( DateTime currentTime = firstBreath.StartInspiration; currentTime < lastBreath.EndTime; currentTime = currentTime.AddSeconds( OUTPUT_INTERVAL ) )
			{
				while( breaths[ currentBreathIndex ].EndTime <= currentTime )
				{
					currentBreathIndex += 1;
				}

				var currentBreath     = breaths[ currentBreathIndex ];
				var inspirationLength = currentBreath.InspirationLength;
				var expirationLength  = currentBreath.ExpirationLength;

				inspirationSmoother.AddObservation( inspirationLength );
				expirationSmoother.AddObservation( expirationLength );
				
				inspirationSamples.Add( inspirationSmoother.Average );
				expirationSamples.Add( expirationSmoother.Average );
				ratioSamples.Add( expirationSmoother.Average / inspirationSmoother.Average );
			}

			return (inspirationSignal, expirationSignal, ratioSignal);
		}
	}
}
