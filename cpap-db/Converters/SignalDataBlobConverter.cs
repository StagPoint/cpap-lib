namespace cpap_db.Converters;

public class SignalDataBlobConverter : IBlobTypeConverter
{
	#region IBlobTypeConverter interface implementation 

	public byte[] ConvertToBlob( object value )
	{
		if( value is not List<double> list )
		{
			throw new InvalidCastException( "Expected a value of type List<double>" );
		}

		var byteBuffer = new byte[ list.Count * sizeof( float ) ];

		using var stream = new MemoryStream( byteBuffer, true );
		using var writer = new BinaryWriter( stream );

		foreach( var element in list )
		{
			writer.Write( (float)element );
		}

		return byteBuffer;
	}
	
	public object ConvertFromBlob( byte[] data )
	{
		using var stream = new MemoryStream( data, false );
		using var reader = new BinaryReader( stream );

		var result = new List<double>( data.Length / sizeof( float ) );

		while( stream.Position < data.Length )
		{
			result.Add( (double)reader.ReadSingle() );
		}

		return result;
	}
	
	#endregion 
}
