using System;
using System.Globalization;

using Avalonia.Data.Converters;

using cpap_app.Helpers;

namespace cpap_app.Converters;

public class NiceNameConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value == null ? string.Empty : NiceNames.Format( value.ToString()! );
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{ 
		throw new NotImplementedException();
	}
}
