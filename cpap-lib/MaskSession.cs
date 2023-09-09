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

		public TimeSpan Duration { get => EndTime - StartTime; }

		#region Public functions

		public Signal GetSignalByName( string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase )
		{
			for( int i = 0; i < Signals.Count; i++ )
			{
				if( Signals[ i ].Name.Equals( name, comparison ) )
				{
					return Signals[ i ];
				}
			}

			return null;
		}

		internal void AddSignal( DateTime startTime, DateTime endTime, EdfStandardSignal fileSignal )
		{
			// Rename signals to their "standard" names. Among other things, this lets us standardize the 
			// data even when there might be slight differences in signal names among various machine models.
			var signalName = SignalNames.GetStandardName( fileSignal.Label.Value );

			Signal signal = GetSignalByName( signalName, StringComparison.Ordinal );

			if( signal != null )
			{
				throw new Exception( $"The session starting at {StartTime:g} already contains a Signal named '{signalName}'" );
			}
			
			signal = new Signal
			{
				Name              = signalName,
				StartTime         = startTime,
				EndTime           = endTime,
				FrequencyInHz     = fileSignal.FrequencyInHz,
				MinValue          = fileSignal.PhysicalMinimum,
				MaxValue          = fileSignal.PhysicalMaximum,
				UnitOfMeasurement = fileSignal.PhysicalDimension,
			};

			signal.Samples.AddRange( fileSignal.Samples );

			Signals.Add( signal );
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
