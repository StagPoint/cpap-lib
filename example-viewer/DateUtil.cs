using System;

namespace example_viewer;

public static class DateTimeExtensions
{
	public static DateTime Trim( this DateTime date, long ticks = TimeSpan.TicksPerSecond )
	{
		return new DateTime( date.Ticks - (date.Ticks % ticks), date.Kind );
	}

	public static long ToTimeCode( this DateTime date )
	{
		return date.ToFileTime() / TimeSpan.TicksPerSecond;
	}
}

