using System;
using System.Collections.Generic;
using System.Linq;

namespace cpap_app.Helpers;

public class MovingAverageCalculator
{
    #region Public properties

	public double Average
	{
		get => _average;
	}

	public double StandardDeviation
	{
		get
		{
			var variance = Variance;
			if( variance >= double.Epsilon )
			{
				var sd = Math.Sqrt( variance );
				return double.IsNaN( sd ) ? 0.0 : sd;
			}
			return 0.0;
		}
	}

	public double Variance
	{
		get
		{
			var n = Count;
			return n > 1 ? _varianceSum / (n - 1) : 0.0;
		}
	}

	public bool HasFullPeriod
	{
		get => _count >= _period;
	}

	public int Count
	{
		get => Math.Min( _count, _period );
	}

    #endregion

    #region Private fields

	private readonly int      _period;
	private readonly double[] _window;
	private          int      _count;
	private          double   _average;
	private          double   _varianceSum;

    #endregion

    #region Constructor

	public MovingAverageCalculator( int period )
	{
		_period = period;
		_window = new double[ period ];
	}

    #endregion

    #region Public functions

	public void AddObservation( double observation )
	{
		// Window is treated as a circular buffer.
		var ndx = _count % _period;
		var old = _window[ ndx ];     // get value to remove from window
		_window[ ndx ] = observation; // add new observation in its place.
		_count++;

		// Update average and standard deviation using deltas
		var old_avg = _average;
		if( _count <= _period )
		{
			var delta = observation - old_avg;
			_average     += delta / _count;
			_varianceSum += (delta * (observation - _average));
		}
		else // use delta vs removed observation.
		{
			var delta = observation - old;
			_average     += delta / _period;
			_varianceSum += (delta * ((observation - _average) + (old - old_avg)));
		}
	}

    #endregion
}
