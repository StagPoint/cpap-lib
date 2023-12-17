using System;
using System.Collections.Generic;
using System.Diagnostics;
// ReSharper disable UseIndexFromEndExpression

namespace cpaplib
{
	public class BreathRecord
	{
		public DateTime StartInspiration { get; set; }
		public DateTime StartExpiration  { get; set; }
		public DateTime EndTime          { get; set; }

		public DateTime TimeOfPeakInspiration { get; set; }
		public DateTime TimeOfPeakExpiration  { get; set; }

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
		public static List<BreathRecord> DetectBreaths( Signal flowSignal, double filterCutoff = 1.0 )
		{
			Debug.Assert( flowSignal.Name == SignalNames.FlowRate, $"Expected a signal named {SignalNames.FlowRate}" );
			
			var results = new List<BreathRecord>();

			var filtered = ButterworthFilter.Filter( flowSignal.Samples.ToArray(), flowSignal.FrequencyInHz, filterCutoff );
			
			// // There's a good argument to be made for using a variable baseline instead of just assuming a zero baseline,
			// // but for now this has been disabled in order to generate results that are as close as possible to other 
			// // reference implementations that use a static baseline.
			// //
			// // The size of the window used to calculate the baseline (10 seconds times the number of samples per second) 
			// int baselineWindowSize = (int)(10 * flowSignal.FrequencyInHz); 
			// var slidingMean = new MovingAverageCalculator( baselineWindowSize );

			// Always start on an inspiration. This happens by default on ResMed machines as far as I can tell but not on
			// Philips Respironics machines, so better to be certain.  
			int startIndex = 0;
			while( filtered[ startIndex ] <= 0 )
			{
				startIndex += 1;
			}

			var lastSign = filtered[ startIndex ] >= 0 ? 1 : -1;
			
			var lastStartIndex       = startIndex;
			var lastSignFlipIndex    = startIndex;
			var peakExpirationIndex  = 0;
			var peakInspirationIndex = 0;

			var minValue  = 0.0;
			var maxValue  = 0.0;
			var totalFlow = 0.0;
			var sign      = 1;

			for( int i = startIndex; i < filtered.Length; i++ )
			{
				var sample = filtered[ i ];

				if( sample <= minValue )
				{
					minValue            = sample;
					peakExpirationIndex = i;
				}

				if( sample >= maxValue )
				{
					maxValue             = sample;
					peakInspirationIndex = i;
				}
				
				// Keep track of all inspiratory and expiratory flow (unfiltered, not adjusted for baseline)
				totalFlow += Math.Abs( flowSignal[ i ] );

				// // What we're really interested in is the flow rate relative to the (moving) baseline, which may not be zero
				// // Inspiration is anything above baseline, expiration is baseline or below
				// slidingMean.AddObservation( sample );
				// sample -= slidingMean.Average;
				
				// Calculate which side of the baseline the sample is on (with a little built-in hysteresis)
				sign = (sign < 0) ? (sample > 0.5 ? 1 : -1) : (sample < -0.5 ? -1 : 1);

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
						StartInspiration      = timeAtIndex( lastStartIndex ),
						StartExpiration       = timeAtIndex( lastSignFlipIndex ),
						EndTime               = timeAtIndex( i ),
						TimeOfPeakInspiration = timeAtIndex( peakInspirationIndex ),
						TimeOfPeakExpiration  = timeAtIndex( peakExpirationIndex ),
						MinValue              = minValue,
						MaxValue              = maxValue,
						TotalFlow             = totalFlow,
					};

					// Add the new breath to the list 
					results.Add( breath );

					lastStartIndex       = i;
					peakInspirationIndex = i;
					peakExpirationIndex  = i;

					minValue  = 0;
					maxValue  = 0;
					totalFlow = 0;
				}

				lastSign          = sign;
				lastSignFlipIndex = i;
			}

			results.Add( new BreathRecord
			{
				StartInspiration      = timeAtIndex( lastStartIndex ),
				StartExpiration       = timeAtIndex( lastSignFlipIndex ),
				EndTime               = flowSignal.EndTime,
				TimeOfPeakInspiration = timeAtIndex( peakInspirationIndex ),
				TimeOfPeakExpiration  = timeAtIndex( peakExpirationIndex ),
				MinValue              = minValue,
				MaxValue              = maxValue,
				TotalFlow             = totalFlow,
			} );

			// // Extend the time of the last breath to match the length of the signal
			// if( lastBreath != null )
			// {
			// 	lastBreath.EndTime = flowSignal.EndTime;
			// }

			return results;

			DateTime timeAtIndex( int index )
			{
				return flowSignal.StartTime.AddSeconds( index / flowSignal.FrequencyInHz );
			}
		}
	}
}
