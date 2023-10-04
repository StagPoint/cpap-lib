using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class StatCalculator
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
    		var totalCount = _signals.Sum( x => x.Samples.Count );
    		var windowSize = (int)Math.Ceiling( totalCount * 0.05 );
    		var window     = new BinaryHeap( windowSize );
    
    		var result = new SignalStatistics
    		{
    			SignalName        = _signals[0].Name,
    			UnitOfMeasurement = _signals[0].UnitOfMeasurement,
    			Minimum           = double.MaxValue,
    			Average           = double.MaxValue,
    			Percentile95      = double.MaxValue,
    			Percentile99      = double.MaxValue,
    			Maximum           = double.MinValue
    		};
    
    		decimal sum = 0;
    
    		foreach( var signal in _signals )
    		{
    			var data = signal.Samples;
    			for( int i = 0; i < data.Count; i++ )
    			{
    				var sample = data[ i ];

				    if( sample > 0 )
				    {
					    result.Minimum = Math.Min( result.Minimum, sample );
				    }

				    result.Maximum =  Math.Max( result.Maximum, sample );

				    sum += (decimal)sample;
    				
    				if( window.Count < window.Capacity )
    				{
    					window.Enqueue( sample );
    					continue;
    				}
    
    				if( sample > window.Peek() )
    				{
    					window.Dequeue();
    					window.Enqueue( sample );
    				}
    			}
    		}
    
    		result.Median       = (result.Minimum + result.Maximum) * 0.5;
    		result.Average      = (double)(sum / totalCount);
    		result.Percentile95 = window.Peek();
    
    		int nn = (int)(totalCount * 0.01);
    		while( window.Count > nn )
    		{
    			window.Dequeue();
    		}
    		result.Percentile99 = window.Peek();
    
    		return result;
    	}
    }
}
