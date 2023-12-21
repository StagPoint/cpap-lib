namespace cpap_db.Converters;

public class DoubleListBlobConverter : IBlobTypeConverter
{
	public byte[] ConvertToBlob( object value )
	{
		if( value is not List<double> list )
		{
			throw new InvalidCastException( "Expected a value of type List<double>" );
		}

		var byteBuffer = new byte[ list.Count * sizeof( double ) ];

		using var stream = new MemoryStream( byteBuffer, true );
		using var writer = new BinaryWriter( stream );
		
		foreach( var element in list )
		{
			writer.Write( element );
		}

		return byteBuffer;
	}
	
	public object ConvertFromBlob( byte[] data )
	{
		using var stream = new MemoryStream( data, true );
		using var reader = new BinaryReader( stream );

		var result = new List<double>( data.Length / sizeof( double ) );

		while( stream.Position < data.Length )
		{
			result.Add( reader.ReadDouble() );
		}

		return result;
	}
}
