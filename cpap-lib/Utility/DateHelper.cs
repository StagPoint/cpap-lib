using System;
using System.Runtime.CompilerServices;

namespace cpaplib
{
	internal static class DateHelper
	{
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
	}
}

