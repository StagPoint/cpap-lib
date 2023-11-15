using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace cpap_app.Converters;

public class TimeConverter : IValueConverter
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
		// ReSharper disable once ConvertIfStatementToSwitchStatement
		if( value is string strValue )
		{
			if( DateTime.TryParse( strValue, out DateTime result ) )
			{
				return result;
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
