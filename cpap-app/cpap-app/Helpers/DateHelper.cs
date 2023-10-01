using System;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class DateHelper
{
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
