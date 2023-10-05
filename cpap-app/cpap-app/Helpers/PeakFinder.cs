using System;
using System.Collections.Generic;
using System.Linq;

namespace cpap_app.Helpers;

public static class PeakFinder
{
	public static int[] GenerateSignals( List<double> input, int lag, double threshold, double influence, double minDelta )
	{
		var signals = new int[ input.Count ];

		// Prime the moving average/stdev window first
		var calculator = new MovingAverageCalculator( lag );
		for( var i = 0; i < lag; i++ )
		{
			calculator.AddObservation( input[ i ] );
		}

		for( var i = lag + 1; i < input.Count; i++ )
		{
			var sample = input[ i ];
			var delta  = Math.Abs( sample - calculator.Average );

			if( delta >= minDelta && delta >= threshold * calculator.StandardDeviation )
			{
				signals[ i ] = sample > calculator.Average ? 1 : -1;

				// Linear interpolation between running average and new sample
				calculator.AddObservation( sample * influence + calculator.Average * (1.0 - influence) );
			}
			else
			{
				calculator.AddObservation( sample );
			}
		}

		return signals;
	}
}
