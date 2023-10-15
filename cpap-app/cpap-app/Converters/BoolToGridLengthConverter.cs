using System;
using System.Globalization;

using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace cpap_app.Converters;

public class BoolToGridLengthConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return (bool)(value ?? false) ? new GridLength( 1, GridUnitType.Auto ) : new GridLength( 0 );
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		throw new NotImplementedException();
	}
}
