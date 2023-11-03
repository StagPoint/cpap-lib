using System;
using System.Collections.Generic;
using System.Diagnostics;

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
	}

	public static class BreathDetection
	{
		public static List<BreathRecord> DetectBreaths( Signal flowSignal, double filterCutoff = 1.0 )
		{
			Debug.Assert( flowSignal.Name == SignalNames.FlowRate, $"Expected a signal named {SignalNames.FlowRate}" );
			
			// The size of the window used to calculate the baseline (10 seconds times the number of samples per second) 
			int baselineWindowSize = (int)(10 * flowSignal.FrequencyInHz); 
				
			var results = new List<BreathRecord>();

			var filtered    = ButterworthFilter.Filter( flowSignal.Samples.ToArray(), flowSignal.FrequencyInHz, filterCutoff );
			var slidingMean = new MovingAverageCalculator( baselineWindowSize );

			// Always start on an inspiration. This happens by default on ResMed machines as far as I can tell, but better to be certain.  
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
				
				var sign = (sample > 0) ? 1 : -1;

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

					if( breath.InspirationLength < 0.1 && results.Count > 1 )
					{
						results[ results.Count - 1 ].EndTime = breath.EndTime;
					}
					else
					{
						results.Add( breath );
					}

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

			// Extend the time of the last breath to match the length of the signal
			if( results.Count > 0 )
			{
				results[ results.Count - 1 ].EndTime = timeAtIndex( filtered.Length - 1 );
			}

			return results;

			DateTime timeAtIndex( int index )
			{
				return flowSignal.StartTime.AddSeconds( index / flowSignal.FrequencyInHz );
			}
		}
	}
}
