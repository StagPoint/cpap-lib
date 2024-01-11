using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace cpap_app.Converters;

/// <summary>
/// For use with NumericUpDown controls to display normalized [0.0, 1.0] double-precision
/// floating point values as percentage. 
/// </summary>
public class NormalizedPercentConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return (decimal)((double)(value ?? throw new ArgumentNullException( nameof( value ) )) * 100);
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return (double)((decimal)(value ?? throw new ArgumentNullException( nameof( value ) )) / 100);
	}
}
