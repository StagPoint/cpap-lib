using System;

namespace cpaplib
{
	public static class DateTimeExtensions
	{
		public static DateTime Trim( this DateTime date, long ticks = TimeSpan.TicksPerSecond )
		{
			return new DateTime( date.Ticks - (date.Ticks % ticks), date.Kind );
		}
	}
}
