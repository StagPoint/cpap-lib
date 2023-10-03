using System;
using System.Collections.Generic;
using System.Linq;

namespace cpap_app.Helpers;

public class ZScoreOutput
{
	public List<double> input;
	public List<int>    signals;
	public List<double> avgFilter;
	public List<double> filtered_stddev;
}

// TODO: I think this class is pretty broken. That's what I get for looking on StackOverflow, lol. Remove it?
public static class ZScore
{
	public static ZScoreOutput GenerateScore( List<double> input, int lag, double threshold, double influence )
	{
		// init variables!
		int[]    signals   = new int[ input.Count ];
		double[] filteredY = new List<double>( input ).ToArray();
		double[] avgFilter = new double[ input.Count ];
		double[] stdFilter = new double[ input.Count ];

		var initialWindow = new List<double>( filteredY ).Skip( 0 ).Take( lag ).ToList();

		avgFilter[ lag - 1 ] = initialWindow.Average();
		stdFilter[ lag - 1 ] = StdDev( initialWindow );

		for( int i = lag; i < input.Count; i++ )
		{
			if( Math.Abs( input[ i ] - avgFilter[ i - 1 ] ) > threshold * stdFilter[ i - 1 ] )
			{
				signals[ i ]   = (input[ i ] > avgFilter[ i - 1 ]) ? 1 : -1;
				filteredY[ i ] = influence * input[ i ] + (1 - influence) * filteredY[ i - 1 ];
			}
			else
			{
				signals[ i ]   = 0;
				filteredY[ i ] = input[ i ];
			}

			// Update rolling average and deviation
			var slidingWindow = new List<double>( filteredY ).Skip( i - lag ).Take( lag + 1 ).ToList();

			avgFilter[ i ] = slidingWindow.Average();
			stdFilter[ i ] = StdDev( slidingWindow );
		}

		// Copy to convenience class 
		var result = new ZScoreOutput();
		result.input           = input;
		result.avgFilter       = new List<double>( avgFilter );
		result.signals         = new List<int>( signals );
		result.filtered_stddev = new List<double>( stdFilter );

		return result;
	}

	private static double StdDev( List<double> values )
	{
		double ret = 0;
		
		if( values.Count > 0 )
		{
			double avg = values.Average();
			double sum = values.Sum( d => Math.Pow( d - avg, 2 ) );
			ret = Math.Sqrt( sum / values.Count );
		}
		
		return ret;
	}
}
