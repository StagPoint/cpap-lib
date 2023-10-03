using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class MathUtil
{
	[MethodImpl( MethodImplOptions.AggressiveInlining)]
	public static double InverseLerp( double a, double b, double v )
	{
		// Avoid returning Infinity
		if( a == b )
		{
			return 0;
		}
		
		return (v - a) / (b - a);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining)]
	public static double Lerp( double a, double b, double t )
	{
		Debug.Assert( t >= 0 && t <= 1 );
		return (1.0 - t) * a + b * t;
	}
}
