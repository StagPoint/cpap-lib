using System;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class DateHelper
{
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

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static bool AreRangesDisjoint( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
	{
		return (startB > endA || endB < startA);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static bool RangesOverlap( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
	{
		return !AreRangesDisjoint( startA, endA, startB, endB );
	}
}
