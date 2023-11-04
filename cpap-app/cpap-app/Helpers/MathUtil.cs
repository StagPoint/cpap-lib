using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class MathUtil
{
	[MethodImpl( MethodImplOptions.AggressiveInlining)]
	public static double InverseLerp( double a, double b, double v )
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		// Avoid returning Infinity when the numbers exactly match. Note that exact match is required
		// for the calculation to return Infinity, so an exact equality comparison between doubles is 
		// valid for this situation.
		if( a == b )
		{
			return 0;
		}
		
		return (v - a) / (b - a);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining)]
	public static double Lerp( double a, double b, double t )
	{
		Debug.Assert( t is >= 0 and <= 1 );
		return (1.0 - t) * a + b * t;
	}

	public static double Remap( double fromA, double fromB, double toA, double toB, double value, bool clamp = true )
	{
		var t = InverseLerp( fromA, fromB, value );

		if( clamp )
		{
			t = double.Clamp( t, 0, 1 );
		}
		
		return Lerp( toA, toB, t );
	}
}
