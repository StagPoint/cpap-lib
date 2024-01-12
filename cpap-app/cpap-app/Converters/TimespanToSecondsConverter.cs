using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace cpap_app.Converters;

/// <summary>
/// For use with NumericUpDown controls to allow editing a TimeSpan value as a simple
/// "Number of seconds" value
/// </summary>
public class TimespanToSecondsConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		if( value == null )
		{
			throw new ArgumentNullException( nameof( value ) );
		}
		
		return (decimal)((TimeSpan)value).TotalSeconds;
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		if( value == null )
		{
			throw new ArgumentNullException( nameof( value ) );
		}

		return TimeSpan.FromSeconds( System.Convert.ToDouble( value ) );
	}
}
