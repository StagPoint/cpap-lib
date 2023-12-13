using System.Text;

using cpap_app.Helpers;

using cpap_db.Converters;

namespace cpap_app.Converters;

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
			case SerializedTypeCode.Integer:
				return reader.ReadInt32();
			case SerializedTypeCode.Boolean:
				return reader.ReadByte() != 0;
			case SerializedTypeCode.Enumeration:
				return reader.ReadInt32();
			case SerializedTypeCode.Float:
				return reader.ReadSingle();
			case SerializedTypeCode.Double:
				return reader.ReadDouble();
			case SerializedTypeCode.String:
				return reader.ReadString();
			case SerializedTypeCode.Timespan:
				return TimeSpan.FromMilliseconds( reader.ReadDouble() );
			case SerializedTypeCode.DateTime:
				return DateHelper.UnixEpoch.AddMilliseconds( reader.ReadDouble() ).ToLocalTime();
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
				writer.Write( (byte)SerializedTypeCode.Integer );
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
				writer.Write( (byte)SerializedTypeCode.Float );
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
		Byte,
		Integer,
		Boolean,
		Enumeration,
		Float,
		Double,
		String,
		Timespan,
		DateTime,
		Null,
	}
	
	#endregion 
}
