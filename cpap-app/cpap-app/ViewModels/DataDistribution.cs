using System;
using System.Collections.Generic;
using System.Linq;

using cpaplib;

namespace cpap_app.ViewModels;

public class DistributionGrouping : IComparable<DistributionGrouping>
{
	public string   Label          { get; set; } = "";
	public double   MinValue       { get; set; } = double.MaxValue;
	public double   MaxValue       { get; set; } = double.MinValue;
	public int      TotalCount     { get; set; } = 0;
	public TimeSpan TotalTime      { get; set; }
	public float    PercentOfTotal { get; set; }

	public int CompareTo( DistributionGrouping? other )
	{
		return MinValue.CompareTo( other?.MinValue ) * -1;
	}

	public override string ToString()
	{
		return $"{Label} : {MinValue:F2} to {MaxValue:F2}";
	}
}

public class DataDistribution
{
	public double MinValue { get; set; } = double.MaxValue;
	public double MaxValue { get; set; } = double.MinValue;
	public double Average  { get; set; } = 0;

	public List<DistributionGrouping> Groupings { get; set; } = new();
	
    #region Factory functions

	public static DataDistribution GetDataDistribution( List<Session> sessions, string signalName, RangeDefinition[] ranges )
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
			return GetDataDistribution( data, ranges, representativeSignal.FrequencyInHz );
		}

		return new DataDistribution();
	}

	public static DataDistribution GetDataDistribution( IEnumerable<double> data, RangeDefinition[] ranges, double sampleFrequency )
	{
		if( ranges == null || ranges.Length == 0 )
		{
			throw new ArgumentException( "No range data provided", nameof( ranges ) );
		}

		var result = new DataDistribution();

		Array.Sort( ranges );
		double minimumValue = double.MinValue;

		for( int i = 0; i < ranges.Length; i++ )
		{
			var range = ranges[ i ];

			result.Groupings.Add( new DistributionGrouping() { Label = range.Label, MinValue = minimumValue, MaxValue = range.MaximumValue } );

			minimumValue = range.MaximumValue + double.Epsilon;
		}

		var samples = data.ToArray();

		var sum   = 0.0;
		int count = 0;
		foreach( var sample in samples )
		{
			double floored = Math.Floor( sample );
			
			sum   += floored;
			count += 1;
			
			result.MinValue = Math.Min( result.MinValue, floored );
			result.MaxValue = Math.Max( result.MaxValue, floored );
		}

		result.Average = Math.Round( sum / count );

		count = 0;
		foreach( var reading in samples )
		{
			count += 1;
	        
			foreach( var grouping in result.Groupings )
			{
				if( reading <= grouping.MaxValue )
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
		
		result.Groupings.Sort();

		return result;
	}
	
	#endregion
	
	#region Nested types

	public struct RangeDefinition : IComparable<RangeDefinition>
	{
		public string Label        { get; set; }
		public double MaximumValue { get; set; }

		public RangeDefinition( string label, double maxValue )
		{
			Label        = label;
			MaximumValue = maxValue;
		}
		
		public int CompareTo( RangeDefinition other )
		{
			return MaximumValue.CompareTo( other.MaximumValue );
		}
	}
	
	#endregion 
}

