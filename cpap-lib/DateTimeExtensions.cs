using System;
using System.Runtime.CompilerServices;

namespace cpaplib
{
	internal static class DateTimeExtensions
	{
		public static DateTime Trim( this DateTime date, long ticks = TimeSpan.TicksPerSecond )
		{
			return new DateTime( date.Ticks - (date.Ticks % ticks), date.Kind );
		}
	}

	internal static class DateUtil
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
	}
}
