using System;
using System.Globalization;

using Avalonia.Data.Converters;
// ReSharper disable ReturnTypeCanBeNotNullable

namespace cpap_app.Converters;

/// <summary>
/// For use with NumericUpDown controls (when bound to Double-precision properties) to automatically convert a NULL value to a default value
/// </summary>
public class NumericNonNullableConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		// TODO: Make this converter class work for more than double-precision float properties?
		return (decimal)(double)(value ?? parameter ?? 0.0);
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return System.Convert.ToDouble( value ?? parameter ?? 0.0 );
	}
}
