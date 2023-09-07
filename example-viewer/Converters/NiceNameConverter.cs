using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace example_viewer;

[ValueConversion( typeof(bool), typeof(GridLength))]
public class NiceNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var stringValue = value.ToString();
		var buffer      = new StringBuilder();

		var state = -1;

		for( int i = 0; i < stringValue.Length; i++ )
		{
			var newState = -1;
			var ch       = stringValue[ i ];

			if( char.IsLower( ch ) || char.IsWhiteSpace( ch ) )
			{
				newState = 1;
			}
			else if( char.IsUpper( ch ) )
			{
				newState = 2;
			}
			else if( char.IsDigit( ch ) || char.IsSeparator( ch ) )
			{
				newState = 3;
			}

			if( i > 0 && newState > state )
			{
				buffer.Append( ' ' );
			}

			buffer.Append( ch );

			state = newState;
		}

		return buffer.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{ 
		return null;
	}
}
