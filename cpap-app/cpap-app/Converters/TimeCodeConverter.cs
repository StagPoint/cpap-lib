using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace cpap_app.Converters;

public class TimeCodeConverter: IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		if( value is DateTime date )
		{
			return $"{date:hh:mm:ss}";
		}

		throw new Exception( $"Value to be converted must be a {nameof( DateTime )}" );
	}
	
	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		if( value is string strValue )
		{
			if( TimeSpan.TryParse( strValue, out TimeSpan result ) )
			{
				return DateTime.Today.Add( result );
			}

			throw new DataValidationException( "Not a valid time code" );
		}

		if( value is DateTime time )
		{
			return time;
		}

		throw new Exception( $"Value to be converted must be a string" );
	}
}
