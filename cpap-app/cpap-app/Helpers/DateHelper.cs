﻿using System;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class DateHelper
{
	public static readonly DateTime UnixEpoch = DateTime.SpecifyKind( new DateTime( 1970, 1, 1 ), DateTimeKind.Utc ).ToLocalTime();

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
		var result = UnixEpoch.AddMilliseconds( milliseconds );
		
		var resultIsDST = TimeZoneInfo.Local.IsDaylightSavingTime( result );
		var nowIsDst    = TimeZoneInfo.Local.IsDaylightSavingTime( DateTime.Today );
		
		// Compensate for ToLocalTime() being incorrect for some historical dates
		return resultIsDST switch
		{
			true when !nowIsDst => result.AddHours( 1 ),
			false when nowIsDst => result.AddHours( -1 ),
			_                   => result
		};
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToMillisecondsSinceEpoch( this DateTime value )
	{
		return (long)value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static DateTime FromNanosecondsSinceEpoch( long nanoseconds )
	{
		var result = UnixEpoch.AddMilliseconds( nanoseconds * 1E-6 );

		var resultIsDST = TimeZoneInfo.Local.IsDaylightSavingTime( result );
		var nowIsDst    = TimeZoneInfo.Local.IsDaylightSavingTime( DateTime.Today );
		
		// Compensate for ToLocalTime() being incorrect for some historical dates
		return resultIsDST switch
		{
			true when !nowIsDst => result.AddHours( 1 ),
			false when nowIsDst => result.AddHours( -1 ),
			_                   => result
		};
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToNanosecondsSinceEpoch( this DateTime value )
	{
		return (long)(value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds * 1E6);
	}
}

public static class TimeSpanExtensions
{
	public static TimeSpan TrimSeconds( this TimeSpan value )
	{
		return TimeSpan.FromSeconds( Math.Truncate( value.TotalSeconds ) );
	}
	
	public static TimeSpan RoundToNearestSecond( this TimeSpan value )
	{
		return TimeSpan.FromSeconds( Math.Round( value.TotalSeconds ) );
	}
	
	public static TimeSpan RoundToNearestMinute( this TimeSpan value )
	{
		return TimeSpan.FromMinutes( Math.Round( value.TotalMinutes ) );
	}
}
