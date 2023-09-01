namespace cpaplib;


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

	public static string ToEdfDate( this DateTime date )
	{
		return date.ToString( "dd.MM.yy" );
	}

	public static string ToEdfTime( this DateTime date )
	{
		return date.ToString( "HH.mm.ss" );
	}
}

