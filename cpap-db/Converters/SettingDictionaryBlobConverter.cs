namespace cpap_app.Converters;

using System.Text;

using cpap_db.Converters;

public class SettingDictionaryBlobConverter : IBlobTypeConverter
{
	#region IBlobTypeConverter interface implementation 
	
	public byte[] ConvertToBlob( object value )
	{
		if( value is not Dictionary<string, object> dictionary )
		{
			throw new ArgumentException( "Expected a Dictionary<string,object>" );
		}

		var buffer = new MemoryStream();
		var writer = new BinaryWriter( buffer, Encoding.Default );

		foreach( var pair in dictionary )
		{
			writer.Write( pair.Key );
			WriteValue( writer, pair.Value );
		}

		return buffer.ToArray();
	}

	public object ConvertFromBlob( byte[] data )
	{
		// TODO: This workaround was only intended to get me through data conversion, and needs to be removed
		if( data == null )
		{
			return new Dictionary<string, object>();
		}
		
		var buffer = new MemoryStream( data );
		var reader = new BinaryReader( buffer, Encoding.Default );

		var result = new Dictionary<string, object>();

		while( buffer.Position < buffer.Length )
		{
			var key   = reader.ReadString();
			var value = ReadValue( reader );

			result[ key ] = value!;
		}

		return result;
	}
	
	#endregion 
	
	#region Private functions

	private object? ReadValue( BinaryReader reader )
	{
		var code = (SerializedTypeCode)reader.ReadByte();
		switch( code )
		{
			case SerializedTypeCode.Byte:
				return reader.ReadByte();
			case SerializedTypeCode.Int32:
				return reader.ReadInt32();
			case SerializedTypeCode.Boolean:
				return reader.ReadByte() != 0;
			case SerializedTypeCode.Enumeration:
				return reader.ReadInt32();
			case SerializedTypeCode.Single:
				return reader.ReadSingle();
			case SerializedTypeCode.Double:
				return reader.ReadDouble();
			case SerializedTypeCode.String:
				return reader.ReadString();
			case SerializedTypeCode.Timespan:
				return TimeSpan.FromMilliseconds( reader.ReadDouble() );
			case SerializedTypeCode.DateTime:
				return DateTime.UnixEpoch.AddMilliseconds( reader.ReadDouble() ).ToLocalTime();
			case SerializedTypeCode.Null:
				return null;
			default:
				throw new ArgumentOutOfRangeException( $"Unhandled type code {code}" );
		}
	}

	private void WriteValue( BinaryWriter writer, object? value )
	{
		switch( value )
		{
			case null:
				writer.Write( (byte)SerializedTypeCode.Null );
				break;
			case byte byteValue:
				writer.Write( (byte)SerializedTypeCode.Byte );
				writer.Write( byteValue );
				break;
			case int intValue:
				writer.Write( (byte)SerializedTypeCode.Int32 );
				writer.Write( intValue );
				break;
			case bool boolValue:
				writer.Write( (byte)SerializedTypeCode.Boolean );
				writer.Write( (byte)(boolValue ? 1 : 0) );
				break;
			case Enum enumValue:
				writer.Write( (byte)SerializedTypeCode.Enumeration );
				writer.Write( (int)value );
				break;
			case float floatValue:
				writer.Write( (byte)SerializedTypeCode.Single );
				writer.Write( floatValue );
				break;
			case double doubleValue:
				writer.Write( (byte)SerializedTypeCode.Double );
				writer.Write( doubleValue );
				break;
			case string stringValue:
				writer.Write( (byte)SerializedTypeCode.String );
				writer.Write( stringValue );
				break;
			case TimeSpan timeValue:
				writer.Write( (byte)SerializedTypeCode.Timespan );
				writer.Write( timeValue.TotalMilliseconds );
				break;
			case DateTime dateValue:
				writer.Write( (byte)SerializedTypeCode.DateTime );
				writer.Write( (dateValue.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds );
				break;
			default:
				throw new NotSupportedException( $"Unsupported data type: {value.GetType()}" );
		}
	}
	
	#endregion 
	
	#region Nested types

	private enum SerializedTypeCode
	{
		INVALID,
		Empty       = 0,  // Null reference
		Null        = 2,  // Null value
		Boolean     = 3,  // Boolean
		Char        = 4,  // Unicode character
		SByte       = 5,  // Signed 8-bit integer
		Byte        = 6,  // Unsigned 8-bit integer
		Int16       = 7,  // Signed 16-bit integer
		UInt16      = 8,  // Unsigned 16-bit integer
		Int32       = 9,  // Signed 32-bit integer
		UInt32      = 10, // Unsigned 32-bit integer
		Int64       = 11, // Signed 64-bit integer
		UInt64      = 12, // Unsigned 64-bit integer
		Single      = 13, // IEEE 32-bit float
		Double      = 14, // IEEE 64-bit double
		Decimal     = 15, // Decimal
		DateTime    = 16, // DateTime stored as the number of milliseconds since Jan 1, 1970 (Unix Epoch)
		String      = 18, // Unicode character string
		Enumeration = 19, // Enumeration value stored as integer
		Timespan    = 20, // Timespan stored as an IEEE 64-bit double
	}

	#endregion 
}
