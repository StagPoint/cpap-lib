using System;
using System.Runtime.CompilerServices;

using Avalonia.Controls;

namespace cpap_app.Helpers;

public static class DateHelper
{
	public static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 ).ToLocalTime();

	/// <summary>
	/// Used to "fix" DateTime values retrieved through Sqlite.Net, which are incorrectly
	/// instantiated and don't contain the correct DateTimeKind value.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static DateTime AsLocalTime( this DateTime value )
	{
		return DateTime.SpecifyKind( value, DateTimeKind.Local );
	}

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

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static DateTime FromMillisecondsSinceEpoch( long milliseconds )
	{
		return UnixEpoch.AddMilliseconds( milliseconds );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToMillisecondsSinceEpoch( this DateTime value )
	{
		return (long)value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static DateTime FromNanosecondsSinceEpoch( long milliseconds )
	{
		return UnixEpoch.AddMilliseconds( milliseconds * 1E-6 );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToNanosecondsSinceEpoch( this DateTime value )
	{
		return (long)(value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds * 1E6);
	}
}
