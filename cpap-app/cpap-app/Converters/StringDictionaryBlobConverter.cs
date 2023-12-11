using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using cpap_db.Converters;

namespace cpap_app.Converters;

public class StringDictionaryBlobConverter : IBlobTypeConverter
{
	public byte[] ConvertToBlob( object value )
	{
		if( value is not Dictionary<string, string> dictionary )
		{
			throw new ArgumentException( "Expected a Dictionary<string,string>" );
		}

		var buffer = new MemoryStream();
		var writer = new BinaryWriter( buffer, Encoding.Default );

		foreach( var pair in dictionary )
		{
			writer.Write( pair.Key );
			writer.Write( pair.Value );
		}

		return buffer.ToArray();
	}

	public object ConvertFromBlob( byte[] data )
	{
		var buffer = new MemoryStream( data );
		var reader = new BinaryReader( buffer, Encoding.Default );

		var result = new Dictionary<string, string>();

		while( buffer.Position < buffer.Length )
		{
			var key   = reader.ReadString();
			var value = reader.ReadString();

			result[ key ] = value;
		}

		return result;
	}
}
