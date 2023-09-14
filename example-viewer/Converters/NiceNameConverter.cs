using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

using example_viewer.Helpers;

namespace example_viewer;

[ValueConversion( typeof(bool), typeof(GridLength))]
public class NiceNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return NiceNames.Format( value.ToString() );
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{ 
		return null;
	}
}
