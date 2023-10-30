using System;
using System.Collections.Generic;

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
		public double TotalInspiration { get; set; }

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

		public bool CanMerge( BreathRecord other )
		{
			return TotalCycleTime < 10;
		}

		public void Merge( BreathRecord other )
		{
			EndTime = other.EndTime;

			if( other.MinValue < MinValue )
			{
				TimeOfPeakExpiration = other.TimeOfPeakExpiration;
				MinValue             = other.MinValue;
			}
		}
	}

	public static class BreathDetection
	{
		public static List<BreathRecord> DetectBreaths( Signal signal, double filterCutoff = 0.75 )
		{
			var results = new List<BreathRecord>();

			var filtered = ButterworthFilter.Filter( signal.Samples.ToArray(), signal.FrequencyInHz, filterCutoff );

			var lastSign          = filtered[ 0 ] >= 0 ? 1 : -1;
			var lastStartIndex    = 0;
			var lastSignFlipIndex = 0;

			var minValue            = 0.0;
			var peakExpirationIndex = 0;

			var maxValue             = 0.0;
			var peakInspirationIndex = 0;
			var totalInspiration     = double.Epsilon;

			// Always start on an inspiration. As far as I can tell this happens by default on the ResMed AirSense 10 Auto, 
			// but I don't know if that always holds true, or if it's true for other models or manufacturers. 
			var startIndex = 0;
			while( filtered[ startIndex ] <= 0 )
			{
				startIndex += 1;
			}

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

				var sign = filtered[ i ] > 0 ? 1 : -1;

				// Keep track of all flow during the inspiration phase
				if( sign == 1 )
				{
					totalInspiration += sample;
				}
				
				// If the signal has not crossed the zero line, keep searching 
				if( sign == lastSign )
				{
					continue;
				}

				if( sign == 1 )
				{
					var breath = new BreathRecord
					{
						StartInspiration      = signal.StartTime.AddSeconds( lastStartIndex / signal.FrequencyInHz ),
						StartExpiration       = signal.StartTime.AddSeconds( lastSignFlipIndex / signal.FrequencyInHz ),
						EndTime               = signal.StartTime.AddSeconds( i / signal.FrequencyInHz ),
						TimeOfPeakInspiration = signal.StartTime.AddSeconds( peakInspirationIndex / signal.FrequencyInHz ),
						TimeOfPeakExpiration  = signal.StartTime.AddSeconds( peakExpirationIndex / signal.FrequencyInHz ),
						MinValue              = minValue,
						MaxValue              = maxValue,
						TotalInspiration      = totalInspiration,
					};
					
#if true
					results.Add( breath );
#else
					const double MIN_BREATH_LENGTH = 60.0 / 40.0;

					// If the breath is longer than a reasonable amount of time for a very short breath, add it to the list
					if( breath.TotalCycleTime >= MIN_BREATH_LENGTH && breath.MaxValue >= 5.0 )
					{
						results.Add( breath );
					}
					// otherwise, extend the time of the last breath to merge with this "breath"
					else if( results.Count > 0 )
					{
						// Note that this "merge" process effectively just extends the length of the expiratory phase. 
					
						var lastBreath = results[ results.Count - 1 ];
						if( lastBreath.CanMerge( breath ) )
						{
							lastBreath.Merge( breath );
						}
						else
						{
							results.Add( breath );
						}
					}
#endif

					lastStartIndex       = i;
					peakInspirationIndex = i;
					peakExpirationIndex  = i;

					minValue         = 0;
					maxValue         = 0;
					totalInspiration = double.Epsilon;
				}

				lastSign          = sign;
				lastSignFlipIndex = i;
			}

			return results;
		}
	}
}
