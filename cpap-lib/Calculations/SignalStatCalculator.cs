﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class SignalStatCalculator
    {
    	private List<Signal> _signals = new List<Signal>();
    
    	public void AddSignal( Signal signal )
    	{
    		_signals.Add( signal );
    	}

	    public SignalStatistics CalculateStats( string signalName, List<Session> sessions )
	    {
		    // Copy all available samples from all sessions into a single array that can be sorted and
		    // used to calculate the statistics. 
		    foreach( var session in sessions )
		    {
			    var signal = session.GetSignalByName( signalName );
			    if( signal != null )
			    {
		    		AddSignal( signal );
			    }
		    }

		    return CalculateStats();
	    }
    
    	public SignalStatistics CalculateStats()
    	{
    		var totalCount           = _signals.Sum( x => x.Samples.Count );
		    var percentileWindowSize = (int)( totalCount * (1.0 - 0.95) );
		    var percentileWindow     = new BinaryHeap( percentileWindowSize );

		    var result = new SignalStatistics
		    {
			    SignalName        = _signals[ 0 ].Name,
			    UnitOfMeasurement = _signals[ 0 ].UnitOfMeasurement,
			    Minimum           = double.MaxValue,
			    Average           = double.MaxValue,
			    Percentile95      = double.MaxValue,
			    Percentile995     = double.MaxValue,
			    Maximum           = double.MinValue
		    };

		    int     period         = (int)(60 * _signals[ 0 ].FrequencyInHz);
		    decimal deviationSum   = 0;
		    int     deviationTotal = 0;
    		decimal sum            = 0;
		    bool    foundMinimum   = false;
    
    		foreach( var signal in _signals )
    		{
			    // Restart deviation calculations per session 
			    var deviationCalculator = new MovingAverageCalculator( period );

			    var data = signal.Samples;
    			for( int i = 0; i < data.Count; i++ )
    			{
    				var sample = data[ i ];

				    deviationCalculator.AddObservation( MathUtil.InverseLerp( signal.MinValue, signal.MaxValue, sample ) );
				    if( deviationCalculator.HasFullPeriod )
				    {
					    deviationSum   += (decimal)deviationCalculator.StandardDeviation;
					    deviationTotal += 1;
				    }

				    if( sample > 0 )
				    {
					    result.Minimum = Math.Min( result.Minimum, sample );
					    foundMinimum   = true;
				    }

				    result.Maximum =  Math.Max( result.Maximum, sample );

				    sum += (decimal)sample;
    				
    				if( percentileWindow.Count < percentileWindow.Capacity )
    				{
    					percentileWindow.Enqueue( sample );
    				}
    				else if( sample > percentileWindow.Peek() )
    				{
    					percentileWindow.Dequeue();
    					percentileWindow.Enqueue( sample );
    				}
    			}
    		}

		    // Because minimum is defined in this library as "Minimum value above zero", it's possible that we've never 
		    // found a minimum value. If that's the case, assign zero explicitly (default value is double.MaxValue).
		    if( !foundMinimum )
		    {
			    result.Minimum = 0;
		    }
		    
		    // TODO: The Percentile95 and Percentile995 values should probably use interpolation to get the precise value 
    
    		result.Median        = (result.Minimum + result.Maximum) * 0.5;
    		result.Average       = (double)(sum / totalCount);
		    result.MeanDeviation = (double)(deviationSum / deviationTotal) * 100.0;
		    result.Percentile95  = percentileWindow.Peek();

		    int nn = (int)( totalCount * (1.0 - 0.995) );
    		while( percentileWindow.Count > nn && percentileWindow.Count > 1 )
    		{
    			percentileWindow.Dequeue();
    		}
    		result.Percentile995 = percentileWindow.Peek();
    
    		return result;
    	}
    }
}