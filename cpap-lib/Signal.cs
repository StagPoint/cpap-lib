using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable UseIndexFromEndExpression

namespace cpaplib
{
	public class Signal
	{
		/// <summary>
		/// The name of the channel, such as SaO2, Flow, Mask Pressure, etc.
		/// </summary>
		public string Name { get; internal set; } = "";

		/// <summary>
		/// Returns the number of samples per second contained in this Signal.
		/// </summary>
		public double FrequencyInHz { get; internal set; }

		/// <summary>
		/// The minimum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MinValue { get; internal set; }

		/// <summary>
		/// The maximum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MaxValue { get; internal set; }
		
		/// <summary>
		/// Gets or sets the unit of measurement used for this signal type (examples: mV, m/s^2, cmHO2, etc.)
		/// </summary>
		public string UnitOfMeasurement { get; internal set; }

		/// <summary>
		/// The signal data for this session 
		/// </summary>
		public List<double> Samples { get; internal set; } = new List<double>();
		
		/// <summary>
		/// The time when recording of this signal was started 
		/// </summary>
		public DateTime StartTime { get; internal set; }
		
		/// <summary>
		/// The time when recording of this signal was stopped 
		/// </summary>
		public DateTime EndTime { get; internal set; }
		
		/// <summary>
		/// The duration of this recording session
		/// </summary>
		public TimeSpan Duration { get => EndTime - StartTime; }
		
		#region Public functions

		/// <summary>
		/// Returns the value of the signal at the given time
		/// </summary>
		public double GetValueAtTime( DateTime time )
		{
			if( time <= StartTime )
				return Samples[ 0 ];
			else if( time >= EndTime )
				return Samples[ Samples.Count - 1 ];

			double offset     = (time - StartTime).TotalSeconds;
			int    leftIndex  = (int)Math.Floor( offset * FrequencyInHz );
			int    rightIndex = leftIndex + 1;
			
			double a     = Samples[ leftIndex ];
			double b     = Samples[ rightIndex ];
			double timeA = leftIndex / FrequencyInHz;
			double timeB = rightIndex / FrequencyInHz;
			double t     = (offset - timeA) / (timeB - timeA);
			
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
