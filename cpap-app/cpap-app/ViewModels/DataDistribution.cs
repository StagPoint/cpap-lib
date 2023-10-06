using System;
using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.ViewModels;

public class DistributionGrouping
{
	public double   MinValue       { get; set; } = double.MaxValue;
	public double   MaxValue       { get; set; } = double.MinValue;
	public int      TotalCount     { get; set; } = 0;
	public TimeSpan TotalTime      { get; set; }
	public float    PercentOfTotal { get; set; }
}

public class DataDistribution
{
	public double MinValue { get; set; } = double.MaxValue;
	public double MaxValue { get; set; } = double.MinValue;
	public double Average  { get; set; } = 0;

	public List<DistributionGrouping> Groupings { get; set; } = new();
	
    #region Factory functions

	public static DataDistribution GetDataDistribution( List<Session> sessions, string signalName, int[] limits )
	{
		IEnumerable<double> data = Array.Empty<double>();

		Signal? representativeSignal = null;

		foreach( var session in sessions )
		{
			var signal = session.GetSignalByName( signalName );
			if( signal != null )
			{
				data                 = data.Concat( signal.Samples );
				representativeSignal = signal;
			}
		}

		if( representativeSignal != null )
		{
			return GetDataDistribution( data, limits, representativeSignal.FrequencyInHz );
		}

		return new DataDistribution();
	}

	public static DataDistribution GetDataDistribution( IEnumerable<double> data, int[] limits, double sampleFrequency )
	{
		if( limits == null || limits.Length == 0 )
		{
			throw new ArgumentException( "No limit data provided", nameof( limits ) );
		}

		if( limits.Length % 2 != 0 )
		{
			throw new ArgumentException( $"The {nameof( limits )} array should contain matched pairs of data boundaries. An odd number of values was submitted.", nameof( limits ) );
		}

		var result = new DataDistribution();

		for( int i = 0; i < limits.Length; i += 2 )
		{
			var maxValue = limits[ i ];
			var minValue = limits[ i + 1 ];

			result.Groupings.Add( new DistributionGrouping() { MinValue = minValue, MaxValue = maxValue } );
		}

		var sum   = 0.0;
		int count = 0;
		foreach( var sample in data )
		{
			sum   += sample;
			count += 1;
			
			result.MinValue =  Math.Min( result.MinValue, sample );
			result.MaxValue =  Math.Max( result.MaxValue, sample );
		}

		result.Average = sum / count;

		count = 0;
		foreach( var reading in data )
		{
			count += 1;
	        
			foreach( var grouping in result.Groupings )
			{
				if( reading >= grouping.MinValue && reading <= grouping.MaxValue )
				{
					grouping.TotalCount += 1;
					break;
				}
			}
		}

		foreach( var grouping in result.Groupings )
		{
			grouping.PercentOfTotal = (float)grouping.TotalCount / count;
			grouping.TotalTime      = TimeSpan.FromSeconds( grouping.TotalCount / sampleFrequency );
		}

		return result;
	}
	
	#endregion
}

