using System;
using System.Collections.Generic;
using System.Linq;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class MaskSession
	{
		public DateTime StartTime { get; internal set; }
		public DateTime EndTime   { get; internal set; }

		public List<Signal> Signals { get; } = new List<Signal>();

		public double duration;

		#region Public functions

		internal void AddSignal( DateTime startTime, DateTime endTime, EdfStandardSignal fileSignal )
		{
			Signal signal = Signals.FirstOrDefault( x => x.Channel.Equals( fileSignal.Label.Value ) );

			if( signal == null )
			{
				signal = new Signal
				{
					StartTime      = startTime,
					EndTime        = endTime,
					Channel        = fileSignal.Label.Value,
					SampleInterval = 1.0 / fileSignal.FrequencyInHz,
					MinValue       = fileSignal.PhysicalMaximum,
					MaxValue       = fileSignal.PhysicalMinimum
				};

				signal.Samples.AddRange( fileSignal.Samples );

				Signals.Add( signal );
			}
		}

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			return $"{StartTime.ToShortDateString()}    {StartTime.ToLongTimeString()} - {EndTime.ToLongTimeString()}";
		}

		#endregion
	}
}
