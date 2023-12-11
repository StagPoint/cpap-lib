namespace cpap_db.Converters;

public interface IBlobTypeConverter
{
	byte[] ConvertToBlob( object   value );
	object ConvertFromBlob( byte[] data );
}
