using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace cpaplib
{
	public class StatCalculator
	{
		private Sorter _sorter;

		public StatCalculator( int initialCapacity )
		{
			_sorter = new Sorter( initialCapacity );
		}

		public SignalStatistics CalculateStats( string signalName, List<Session> sessions )
		{
			// Reset the _sorter for the next iteration 
			_sorter.Clear();

			string unitOfMeasurement = "";

			// Copy all available samples from all sessions into a single array that can be sorted and
			// used to calculate the statistics. 
			foreach( var session in sessions )
			{
				var signal = session.GetSignalByName( signalName );
				if( signal != null )
				{
					unitOfMeasurement = signal.UnitOfMeasurement;
					_sorter.AddRange( signal.Samples );
				}
			}

			if( _sorter.Count == 0 )
			{
				return null;
			}

			// Sort the aggregated samples and calculate statistics on the results 
			var sortedSamples = _sorter.Sort();
			var bufferLength  = sortedSamples.Count;

			var stats = new SignalStatistics
			{
				SignalName        = signalName,
				UnitOfMeasurement = unitOfMeasurement,
				Minimum           = sortedSamples.Any( x => x > 0 ) ? sortedSamples.Where( x => x > 0 ).Min() : 0,
				Average           = sortedSamples.Average(),
				Maximum           = sortedSamples.Max(),
				Median            = sortedSamples[ bufferLength / 2 ],
				Percentile95      = sortedSamples[ (int)(bufferLength * 0.95) ],
				Percentile99      = sortedSamples[ (int)(bufferLength * 0.995) ],
			};
				
			return stats;
		}
	}
}
