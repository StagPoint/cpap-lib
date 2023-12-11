using System;

using Avalonia.Media;

using cpap_db.Converters;

namespace cpap_app.Converters;

public class ColorBlobConverter : IBlobTypeConverter
{
	public byte[] ConvertToBlob( object value )
	{
		if( value is not Color color )
		{
			throw new InvalidCastException( $"{value} is not a {nameof( Color )} type" );
		}

		return new byte[] { color.A, color.R, color.G, color.B };
	}

	public object ConvertFromBlob( byte[] data )
	{
		if( data.Length != 4 )
		{
			throw new IndexOutOfRangeException( $"Argument {nameof( data )} is not the correct length" );
		}
		
		return Color.FromArgb( data[ 0 ], data[ 1 ], data[ 2 ], data[ 3 ] );
	}
}
