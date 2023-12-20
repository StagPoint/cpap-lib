using System;
using System.Collections.Generic;
using System.Diagnostics;
// ReSharper disable UseIndexFromEndExpression
// ReSharper disable MergeIntoPattern

namespace cpaplib
{
	public class BreathRecord
	{
		public DateTime StartInspiration { get; set; }
		public DateTime StartExpiration  { get; set; }
		public DateTime EndTime          { get; set; }

		public double MinValue         { get; set; }
		public double MaxValue         { get; set; }
		public double TotalFlow        { get; set; }

		public double Range
		{
			get => MaxValue - MinValue;
		}

		public double InspirationToExpirationRatio
		{
			get => InspirationLength / ExpirationLength;
		}

		public double InspirationLength
		{
			get => (StartExpiration - StartInspiration).TotalSeconds;
		}

		public double ExpirationLength
		{
			get => (EndTime - StartExpiration).TotalSeconds;
		}

		public double TotalCycleTime
		{
			get => (EndTime - StartInspiration).TotalSeconds;
		}

		public double InspiratoryFlow
		{
			get => TotalFlow * (InspirationLength / TotalCycleTime);
		}

		public double ExpiratoryFlow
		{
			get => TotalFlow * (ExpirationLength / TotalCycleTime);
		}

		public void Append( BreathRecord other )
		{
			EndTime   =  other.EndTime;
			TotalFlow += other.TotalFlow;
			MinValue  =  Math.Min( MinValue, other.MinValue );
			MaxValue  =  Math.Max( MaxValue, other.MaxValue );
		}
	}

	public static class BreathDetection
	{
		public static List<BreathRecord> DetectBreaths( Signal flowSignal, double filterCutoff = 1.0, bool useVariableBaseline = false )
		{
			Debug.Assert( flowSignal.Name == SignalNames.FlowRate, $"Expected a signal named {SignalNames.FlowRate}" );
			
			var results = new List<BreathRecord>();

			var filtered = ButterworthFilter.Filter( flowSignal.Samples.ToArray(), flowSignal.FrequencyInHz, filterCutoff );
			
			// There's a good argument to be made for using a variable baseline instead of just assuming a zero baseline,
			// but for now this has been disabled in order to generate results that are as close as possible to other 
			// reference implementations that use a static baseline.
			//
			// The size of the window used to calculate the baseline (10 seconds times the number of samples per second) 
			int baselineWindowSize = (int)(10 * flowSignal.FrequencyInHz); 
			var slidingMean = new MovingAverageCalculator( baselineWindowSize );

			// Always start on an inspiration. This happens by default on ResMed machines as far as I can tell but
			// not on Philips Respironics machines, so better to be certain.
			// By using a threshold above zero, we can skip past any "breathing not detected" sections (Philips
			// Respironics System One machines) sections at the beginning of the signal (any such sections in the
			// middle are still going to be a problem, though). 
			int startIndex = 0;
			while( filtered[ startIndex ] <= 1 )
			{
				startIndex += 1;
			}

			var lastSign = filtered[ startIndex ] >= 0 ? 1 : -1;
			var sign     = lastSign;

			var lastStartIndex    = startIndex;
			var lastSignFlipIndex = startIndex;
			
			var minValue  = 0.0;
			var maxValue  = 0.0;
			var totalFlow = 0.0;

			BreathRecord lastBreath = null;

			for( int i = startIndex; i < filtered.Length; i++ )
			{
				var sample = filtered[ i ];

				if( sample <= minValue )
				{
					minValue = sample;
				}

				if( sample >= maxValue )
				{
					maxValue = sample;
				}
				
				// TODO: Total flow calculation is likely incorrect, needs an error analysis  
				// Keep track of all inspiratory and expiratory flow (unfiltered, not adjusted for baseline)
				totalFlow += Math.Abs( flowSignal[ i ] ) * (30.0 / flowSignal.FrequencyInHz);

				if( useVariableBaseline )
				{
					// What we're really interested in is the flow rate relative to the (moving) baseline, which may not be zero
					// Inspiration is anything above baseline, expiration is baseline or below
					slidingMean.AddObservation( sample );
					sample -= slidingMean.Average;
				}

				// Calculate which side of the baseline the sample is on (with a little built-in hysteresis)
				const double HYSTERESIS_THRESHOLD = 0.5;
				sign = (sign <= 0) ? (sample >= HYSTERESIS_THRESHOLD ? 1 : -1) : (sample <= -HYSTERESIS_THRESHOLD ? -1 : 1);

				// If the signal has not crossed the zero line, keep searching 
				if( sign == lastSign )
				{
					continue;
				}

				// All tracked breaths start during the inspiration phase
				if( sign == 1 )
				{
					var breath = new BreathRecord
					{
						StartInspiration = timeAtIndex( lastStartIndex ),
						StartExpiration  = timeAtIndex( lastSignFlipIndex ),
						EndTime          = timeAtIndex( i ),
						MinValue         = minValue,
						MaxValue         = maxValue,
						TotalFlow        = totalFlow,
					};

					// If the breath was too short to be a real breath then it was probably just a minor
					// fluctuation across the baseline, so just append it to the previous breath. 
					if( breath.TotalCycleTime < 0.5 && lastBreath != null && lastBreath.TotalCycleTime < 10.0 )
					{
						lastBreath.Append( breath );
					}
					// If the breath was too shallow to really qualify as a full breath, append it to the previous breath. 
					else if( breath.Range <= 5 && lastBreath != null && lastBreath.TotalCycleTime < 10.0 )
					{
						lastBreath.Append( breath );
					}
					else
					{
						// Add the new breath to the list 
						results.Add( breath );
						lastBreath = breath;
					}

					lastStartIndex = i;

					minValue  = 0;
					maxValue  = 0;
					totalFlow = 0;
				}

				lastSign          = sign;
				lastSignFlipIndex = i;
			}

			results.Add( new BreathRecord
			{
				StartInspiration = timeAtIndex( lastStartIndex ),
				StartExpiration  = timeAtIndex( lastSignFlipIndex ),
				EndTime          = flowSignal.EndTime,
				MinValue         = minValue,
				MaxValue         = maxValue,
				TotalFlow        = totalFlow,
			} );

			return results;

			DateTime timeAtIndex( int index )
			{
				// TODO: Should probably use interpolation to find the precise time rather than "time of closest sample", which is sensitive to sample rate
				return flowSignal.StartTime.AddSeconds( index / flowSignal.FrequencyInHz );
			}
		}
	}
}
