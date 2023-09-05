using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			// Rename signals to their "standard" names. Among other things, this lets us standardize the 
			// data even when there might be slight differences in signal names among various machine models.
			var signalName = SignalNames.GetStandardName( fileSignal.Label.Value );

			Signal signal = Signals.FirstOrDefault( x => x.Name.Equals( signalName, StringComparison.Ordinal ) );

			if( signal == null )
			{
				signal = new Signal
				{
					Name              = SignalNames.GetStandardName( fileSignal.Label.Value ),
					StartTime         = startTime,
					EndTime           = endTime,
					SampleInterval    = 1.0 / fileSignal.FrequencyInHz,
					MinValue          = fileSignal.PhysicalMaximum,
					MaxValue          = fileSignal.PhysicalMinimum,
					UnitOfMeasurement = fileSignal.PhysicalDimension,
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
