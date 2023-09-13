using System;
using System.Globalization;
using System.Windows.Data;

using cpaplib;

namespace example_viewer;

[ValueConversion( typeof(EventType), typeof(string))]
public class EventTypeToNameConverter : IValueConverter
{
	public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
	{
		if( value is not EventType type )
		{
			throw new Exception( $"{value} is not a valid {nameof( EventType )} value" );
		}

		return type.ToName();
	}
	
	public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
	{
		return EventTypeUtil.FromName( value as string );
	}
}
