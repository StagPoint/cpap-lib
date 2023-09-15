using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class Session
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime   { get; set; }
		public TimeSpan Duration  { get => EndTime - StartTime; }
		
		public string Source { get; set; }

		public List<Signal> Signals { get; set; } = new List<Signal>();

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

		internal void Merge( Session other )
		{
			if( StartTime > other.EndTime || EndTime < other.StartTime )
			{
				throw new Exception( $"Cannot merge sessions which do not overlap in time {StartTime:T} to {EndTime:T}" );
			}
			
			foreach( var signal in other.Signals )
			{
				var existingSignal = GetSignalByName( signal.Name );
				if( existingSignal != null )
				{
					// The code calling Merge should have decided what to do about duplicate signal names before calling this function.
					throw new Exception( $"The {nameof( Session )} already contains a {nameof( Signal )} named {signal.Name}" );
				}

				// We will create a clone and trim that, because it's conceivable (maybe even likely) that the caller
				// will still want the original Signal unchanged when this call is finished. 
				var trimmedSignal = signal.Clone();
				trimmedSignal.TrimToTime( StartTime, EndTime );
				
				Signals.Add( trimmedSignal );
			}
		}

		public void AddSignal( Signal signal )
		{
			if( GetSignalByName( signal.Name ) != null )
			{
				throw new Exception( $"This {nameof( Session )} already contains a signal named {signal.Name}" );
			}

			Signals.Add( signal );
			
			StartTime = DateUtil.Min( StartTime, signal.StartTime );
			EndTime   = DateUtil.Max( EndTime, signal.EndTime );
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
