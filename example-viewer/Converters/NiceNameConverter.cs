using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

using cpapviewer.Helpers;

namespace cpapviewer;

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
