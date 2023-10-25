using System;
using System.Collections.Generic;
using System.Linq;

using cpap_db;
using cpap_db.Converters;

namespace cpap_app.Converters;

public class EnumListBlobConverter<T> : IBlobTypeConverter where T : struct, Enum
{
	public byte[] ConvertToBlob( object value )
	{
		if( value is not IList<T> list )
		{
			throw new InvalidCastException( $"{value} is not a List<{nameof( T )}> instance" );
		}

		var result = new byte[ list.Count ];
		for( int i = 0; i < list.Count; i++ )
		{
			result[ i ] = Convert.ToByte( list[ i ] );
		}

		return result;
	}
	
	public object ConvertFromBlob( byte[] value )
	{
		var result = new List<T>( value.Length );
		
		result.AddRange( from t in value select (T)Convert.ChangeType( t, typeof( int ) ) );

		return result;
	}
}
