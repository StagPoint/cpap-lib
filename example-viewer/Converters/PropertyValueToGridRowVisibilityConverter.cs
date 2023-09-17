using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using cpaplib;

namespace cpapviewer;

[ValueConversion( typeof(bool), typeof(GridLength))]
public class PropertyValueToGridRowVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value.ToString().Equals( (string)parameter, StringComparison.Ordinal ) ? new GridLength(1, GridUnitType.Auto) : new GridLength(0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{ 
		return null;
	}
}

