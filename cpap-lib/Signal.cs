using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UseIndexFromEndExpression

namespace cpaplib
{
	public class Signal
	{
		/// <summary>
		/// The name of the channel, such as SaO2, Flow, Mask Pressure, etc.
		/// </summary>
		public string Name { get; set; } = "";

		/// <summary>
		/// Returns the number of samples per second contained in this Signal.
		/// </summary>
		public double FrequencyInHz { get; set; }

		/// <summary>
		/// The minimum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MinValue { get; set; }

		/// <summary>
		/// The maximum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MaxValue { get; set; }
		
		/// <summary>
		/// Gets or sets the unit of measurement used for this signal type (examples: mV, m/s^2, cmHO2, etc.)
		/// </summary>
		public string UnitOfMeasurement { get; set; }

		/// <summary>
		/// The signal data for this session 
		/// </summary>
		public List<double> Samples { get; internal set; } = new List<double>();
		
		/// <summary>
		/// The time when recording of this signal was started 
		/// </summary>
		public DateTime StartTime { get; set; }
		
		/// <summary>
		/// The time when recording of this signal was stopped 
		/// </summary>
		public DateTime EndTime { get; set; }
		
		/// <summary>
		/// The duration of this recording session
		/// </summary>
		public TimeSpan Duration { get => EndTime - StartTime; }
		
		#region Public functions

		/// <summary>
		/// Creates a full copy (deep copy) of this instance
		/// </summary>
		public Signal Clone()
		{
			var clone = new Signal
			{
				Name              = Name,
				FrequencyInHz     = FrequencyInHz,
				MinValue          = MinValue,
				MaxValue          = MaxValue,
				UnitOfMeasurement = UnitOfMeasurement,
				Samples           = new List<double>( Samples ),
				StartTime         = StartTime,
				EndTime           = EndTime
			};

			return clone;
		}

		public static bool TimesOverlap( Session a, Session b )
		{
			return DateHelper.RangesOverlap( a.StartTime, a.EndTime, b.StartTime, b.EndTime );
		}

		public void TrimToTime( DateTime minTime, DateTime maxTime )
		{
			if( maxTime < StartTime || minTime > EndTime )
			{
				throw new Exception( "The time range given does not overlap this Signal's time range" );
			}
			
			if( minTime > StartTime )
			{
				double secondsToTrim = Math.Abs( (minTime - StartTime).TotalSeconds );
				int    samplesToTrim = (int)Math.Ceiling( secondsToTrim * FrequencyInHz );
			
				Samples.RemoveRange( 0, samplesToTrim );
				StartTime = StartTime.AddSeconds( samplesToTrim * FrequencyInHz );
			}
			
			if( maxTime < EndTime )
			{
				double secondsToTrim = Math.Abs( (maxTime - EndTime).TotalSeconds );
				int    samplesToTrim = (int)Math.Ceiling( secondsToTrim * FrequencyInHz );
			
				Samples.RemoveRange( Samples.Count - samplesToTrim - 1, samplesToTrim );
				EndTime = EndTime.AddSeconds( samplesToTrim * FrequencyInHz * -1 );
			}
		}

		/// <summary>
		/// Returns the value of the signal at the given time
		/// </summary>
		/// <param name="interpolate">If TRUE, will interpolate between the two surrounding signal values to return
		/// the exact value at the given time. Will otherwise return the value that is closest to the given time.</param>
		public double GetValueAtTime( DateTime time, bool interpolate = true )
		{
			if( time <= StartTime )
				return Samples[ 0 ];
			else if( time >= EndTime )
				return Samples[ Samples.Count - 1 ];

			// Determine which two samples to interpolate between 
			double offset     = (time - StartTime).TotalSeconds;
			int    leftIndex  = (int)Math.Floor( offset * FrequencyInHz );
			int    rightIndex = (int)Math.Ceiling( offset * FrequencyInHz );

			// If the time is at (or just barely past) the end of the Signal, return the last sample. 
			if( rightIndex >= Samples.Count )
			{
				return Samples[ Samples.Count - 1 ];
			}
			
			// Grab the samples on either side of the given time
			double a = Samples[ leftIndex ];
			double b = Samples[ rightIndex ];

			// If not interpolating, then just return the sample that is closest to the given time
			if( !interpolate )
			{
				return a;
			}
			
			// Calculate the quantized time of each bounding sample 
			double timeA = leftIndex / FrequencyInHz;
			double timeB = rightIndex / FrequencyInHz;

			// Calculate the distance to interpolate (from 0.0 to 1.0) 
			double t = (offset - timeA) / (timeB - timeA);

			// Return a standard linear interpolation between the samples 
			return (1.0 - t) * a + b * t;
		}
		
		#endregion 

		#region Base class overrides

		public override string ToString()
		{
			return Name;
		}

		#endregion
	}
}
