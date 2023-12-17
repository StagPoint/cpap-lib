using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
// ReSharper disable MergeIntoPattern

namespace cpaplib
{
	internal static class MathUtil
	{
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		public static double Clamp( double min, double max, double value )
		{
			return Math.Min( max, Math.Max( min, value ) );
		}
		
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		public static double InverseLerp( double a, double b, double v )
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			// Avoid returning Infinity or DivideByZero when the numbers exactly match. Note that exact
			// match is required for the calculation to return Infinity, so an exact equality comparison
			// between doubles is valid and appropriate for this situation.
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
}
