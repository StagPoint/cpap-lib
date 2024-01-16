using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace cpap_app.Converters;

public class EnumToBooleanConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return value != null && parameter != null && ((int)value == (int)parameter);
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return value?.Equals( true ) == true ? parameter : BindingOperations.DoNothing;
	}
}
