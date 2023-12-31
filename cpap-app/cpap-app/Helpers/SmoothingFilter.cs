﻿using System.Collections.Generic;
using System.Diagnostics;

namespace cpap_app.Helpers;

public class SmoothingFilter
{
	#region Private fields 
	
	private double[] _Y = new double[ 3 ];
	private double[] _X = new double[ 2 ];

	private int    _order;
	private double _beta;
	private int    _count;
	
	#endregion 
	
	#region Constructor

	public SmoothingFilter( int order, double beta = 0.5 )
	{
		Debug.Assert( order >= 1 && order <= 3, $"Order value {order} is out of range (must be 1, 2, or 3)" );
		Debug.Assert( beta > 0 && beta <= 1,    $"Beta value {beta:F2} is out of range (must be > 0 and <= 1)" );
		
		_order = order;
		_beta  = beta;
		_count = 0;
	}
	
	#endregion 
	
	#region Public functions

	public double Filter( double sample )
	{
		double result = _order switch
		{
			1 => firstOrder( sample, _count < 1 ? 1.0 : _beta ),
			2 => secondOrder( sample, _count < 2 ? 1.0 : _beta ),
			3 => thirdOrder( sample, _count < 3 ? 1.0 : _beta ),
			_ => sample
		};

		_count += 1;

		return result;
	}
	
	public static double[] Filter( List<double> data, int order, double beta = 0.5 )
	{
		var filter = new SmoothingFilter( order, beta );
		var result = new double[ data.Count ];

		for( int i = 0; i < result.Length; i++ )
		{
			result[ i ] = filter.Filter( data[ i ] );
		}

		return result;
	}

	#endregion
	
	#region Private functions 

	private double firstOrder( double sample, double beta )
	{
		double filtered =
			beta * sample +
			(1 - beta) * _Y[ 0 ];

		_Y[ 0 ] = filtered;

		return filtered;
	}

	private double secondOrder( double sample, double beta )
	{
		var filtered =
			beta * sample +
			beta * (1 - beta) * _X[ 0 ] +
			(1 - beta) * (1 - beta) * _Y[ 1 ];

		_X[ 0 ] = sample;

		_Y[ 1 ] = _Y[ 0 ];
		_Y[ 0 ] = filtered;

		return filtered;
	}

	private double thirdOrder( double sample, double beta )
	{
		double filtered =
			beta * sample +
			beta * (1 - beta) * _X[ 0 ] +
			beta * (1 - beta) * (1 - beta) * _X[ 0 ] +
			(1 - beta) * (1 - beta) * (1 - beta) * _Y[ 2 ];

		_X[ 1 ] = _X[ 0 ];
		_X[ 0 ] = sample;

		_Y[ 2 ] = _Y[ 1 ];
		_Y[ 1 ] = _Y[ 0 ];
		_Y[ 0 ] = filtered;

		return filtered;
	}
	
	#endregion 
}
