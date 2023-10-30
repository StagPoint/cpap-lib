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

		public double TotalLength
		{
			get => (EndTime - StartInspiration).TotalSeconds;
		}

		public bool CanMerge( BreathRecord other )
		{
			return (TotalLength < 30);
		}

		public void Merge( BreathRecord other )
		{
			// Note that this "merge" process effectively extends the length of the expiratory phase, which seems
			// perfectly reasonable given the description of ResMed's "fuzzy logic" for phase determination given
			// in https://doi.org/10.2147/MDER.S70062 (although obviously this code is not an implementation of
			// that logic). 

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
		public static List<BreathRecord> DetectBreaths( Signal signal )
		{
			const double MIN_BREATH_LENGTH = 1.5;

			var results = new List<BreathRecord>();

			var filtered = ButterworthFilter.Filter( signal.Samples.ToArray(), signal.FrequencyInHz, 0.75 );

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
				var sample = signal.Samples[ i ];

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

				var sign = filtered[ i ] >= 0 ? 1 : -1;

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

					// If the breath is longer than a reasonable amount of time for a very short breath, add it to the list
					if( breath.TotalLength >= MIN_BREATH_LENGTH )
					{
						results.Add( breath );
					}
					// otherwise, extend the time of the last breath to merge with this "breath"
					else if( results.Count > 0 )
					{
						// Note that this "merge" process effectively extends the length of the expiratory phase, which seems
						// perfectly reasonable given the description of ResMed's "fuzzy logic" for phase determination given
						// in https://doi.org/10.2147/MDER.S70062 (although obviously this code is not an implementation of
						// that logic). 
						// TODO: Implement "fuzzy logic" breath detection? Could use topographical persistence peak finding? Would handle baseline wandering better, I think.

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
