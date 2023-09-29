using System;
using System.Globalization;
using System.Text;

using Avalonia.Data.Converters;

namespace cpap_app.Converters;


public enum TimespanFormatType
{
	Long, Short, Abbreviated
}

public class FormattedTimespanConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if( value is not TimeSpan time )
		{
			throw new Exception( $"{value} is not a TimeSpan" );
		}

		var stringParameter = parameter as string ?? string.Empty;

		bool allowEmpty = stringParameter.EndsWith( ".Empty", StringComparison.OrdinalIgnoreCase );
		if( allowEmpty )
		{
			stringParameter = stringParameter.Replace( ".Empty", "", StringComparison.OrdinalIgnoreCase );
		}

		var format = string.IsNullOrEmpty( stringParameter ) ? TimespanFormatType.Long : Enum.Parse<TimespanFormatType>( stringParameter );

		return FormatTimeSpan( time, format, allowEmpty );
	}
	
	public static string FormatTimeSpan( TimeSpan time, TimespanFormatType format, bool allowEmpty )
	{
		var builder = new StringBuilder();

		if( time.Hours > 0 )
		{
			builder.Append( hoursText( time.Hours ) );
			if( time.Minutes > 0 || time.Seconds > 0 )
			{
				builder.Append( ' ' );
			}
		}

		if( time.Minutes > 0 )
		{
			builder.Append( minutesText( time.Minutes ) );
			if( format == TimespanFormatType.Long || time.Seconds > 0 )
			{
				builder.Append( ' ' );
			}
		}

		if( format == TimespanFormatType.Long || time.Seconds > 0 )
		{
			if( format == TimespanFormatType.Long )
				builder.Append( totalSecondsText( time.TotalSeconds ) );
			else
				builder.Append( secondsText( time.Seconds ) );
		}

		if( builder.Length == 0 && !allowEmpty )
		{
			switch( format )
			{
				case TimespanFormatType.Abbreviated:
					return "0 s";
				case TimespanFormatType.Short:
					return "0 sec";
				case TimespanFormatType.Long:
				default:
					return "0 seconds";
			}
		}

		return builder.ToString();

		string secondsText( int seconds )
		{
			if( seconds < 1 )
				return "";

			switch( format )
			{
				case TimespanFormatType.Abbreviated:
					return $"{seconds} s";
				case TimespanFormatType.Short:
					return $"{seconds} sec";
				case TimespanFormatType.Long:
				default:
					return seconds != 1 ? $"{seconds} seconds" : $"{seconds} second";
			}
		}

		string totalSecondsText( double seconds )
		{
			if( seconds < 1 )
				return "";

			switch( format )
			{
				case TimespanFormatType.Abbreviated:
					return $"{seconds:F2} s";
				case TimespanFormatType.Short:
					return $"{seconds:F2} sec";
				case TimespanFormatType.Long:
				default:
					return $"{seconds:F2} seconds";
			}
		}

		string minutesText( int minutes )
		{
			if( minutes < 1 )
				return "";

			switch( format )
			{
				case TimespanFormatType.Abbreviated:
					return $"{minutes} m";
				case TimespanFormatType.Short:
					return $"{minutes} min";
				case TimespanFormatType.Long:
				default:
					return minutes != 1 ? $"{minutes} minutes" : $"{minutes} minute";
			}
		}

		string hoursText( int hours )
		{
			if( hours < 1 )
				return "";

			switch( format )
			{
				case TimespanFormatType.Abbreviated:
					return $"{hours} h";
				case TimespanFormatType.Short:
					return $"{hours} hr";
				case TimespanFormatType.Long:
				default:
					return hours != 1 ? $"{hours} hours" : $"{hours} hour";
			}
		}
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
