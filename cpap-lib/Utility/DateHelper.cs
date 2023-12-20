using System;
using System.Runtime.CompilerServices;

namespace cpaplib
{
	internal static class DateHelper
	{
		public static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );
		
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static DateTime Min( DateTime a, DateTime b )
		{
			return (a < b) ? a : b;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static DateTime Max( DateTime a, DateTime b )
		{
			return (a > b) ? a : b;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool AreRangesDisjoint( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
		{
			return (startB > endA || endB < startA);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool RangesOverlap( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
		{
			return !AreRangesDisjoint( startA, endA, startB, endB );
		}
		
		public static double InverseLerp( DateTime a, DateTime b, DateTime value )
		{
			var numA = (a - UnixEpoch).TotalMilliseconds;
			var numB = (b - UnixEpoch).TotalMilliseconds;
			var numV = (value - UnixEpoch).TotalMilliseconds;

			return MathUtil.InverseLerp( numA, numB, MathUtil.Clamp( numA, numB, numV ) );
		}

		public static DateTime Lerp( DateTime a, DateTime b, double t )
		{
			var numA = (a - UnixEpoch).TotalMilliseconds;
			var numB = (b - UnixEpoch).TotalMilliseconds;

			var lerp = MathUtil.Lerp( numA, numB, t );

			return UnixEpoch.AddMilliseconds( lerp );
		}
	}
}

