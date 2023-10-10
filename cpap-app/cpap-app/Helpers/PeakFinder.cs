using System;
using System.Collections.Generic;
using System.Linq;

namespace cpap_app.Helpers;

public static class PeakFinder
{
	public static int[] GenerateSignals( List<double> input, int windowSize, double threshold, double peakInfluence, double minDelta )
	{
		var signals = new int[ input.Count ];

		// Prime the moving average/stdev window first
		var calculator = new MovingAverageCalculator( windowSize );
		for( var i = 0; i < windowSize; i++ )
		{
			calculator.AddObservation( input[ i ] );
		}

		for( var i = windowSize + 1; i < input.Count; i++ )
		{
			var sample = input[ i ];
			var delta  = Math.Abs( sample - calculator.Average );

			if( delta >= minDelta && delta >= threshold * calculator.StandardDeviation )
			{
				signals[ i ] = sample > calculator.Average ? 1 : -1;

				// Linear interpolation between running average and new sample
				calculator.AddObservation( sample * peakInfluence + calculator.Average * (1.0 - peakInfluence) );
			}
			else
			{
				calculator.AddObservation( sample );
			}
		}

		return signals;
	}
}
