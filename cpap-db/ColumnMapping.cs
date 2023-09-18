using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace cpap_db;

public class KeyColumn
{
	public string ColumnName { get; set; }
	public Type   Type       { get; set; }
	public string DbType     { get; set; }
	public object Value      { get; set; }
	public bool   IsNullable { get; set; } = false;

	public KeyColumn( string name, Type type )
	{
		ColumnName = name;
		Type       = type;
		DbType     = DatabaseMapping.GetSqlType( type );
		Value      = null;
	}
}

public class PrimaryKeyColumn : KeyColumn
{
	public bool AutoIncrement { get; set; }

	public PrimaryKeyColumn( string name, Type type, bool autoIncrement = false ) 
		: base( name, type )
	{
		AutoIncrement = autoIncrement;
	}
}

public class ForeignKeyColumn : KeyColumn
{
	public string ReferencedTable { get; set; }
	public string ReferencedField { get; set; }

	public string OnDeleteAction { get; set; } = "CASCADE";

	public string OnUpdateAction { get; set; } = "NO ACTION";

	public ForeignKeyColumn( string name, Type type, string referencedTable, string referencedField ) 
		: base( name, type )
	{
		ReferencedTable = referencedTable;
		ReferencedField = referencedField;
	}
}

public class ColumnMapping
{
	public string ColumnName      { get; set; }
	public int?   MaxStringLength { get; set; }
	public bool   IsUnique        { get; set; }
	public bool   IsNullable      { get; set; }

	public Type Type
	{
		get => Converter != null ? typeof( byte[] ) : _propertyType;
		set => _propertyType = value;
	}
	
	public IBlobTypeConverter Converter { get; set; }

	private PropertyInfo _property;
	private System.Type  _propertyType;

	public ColumnMapping( string columnName, string propertyName, Type owningType )
		: this( columnName, owningType.GetProperty( propertyName, BindingFlags.Instance | BindingFlags.Public ) )
	{
		IsNullable = _property.PropertyType.IsClass;
	}

	public ColumnMapping( string name, PropertyInfo property )
	{
		ColumnName = name;
		_property  = property;
		Type       = property.PropertyType;

		Debug.Assert( _property != null && _property.CanRead && _property.CanWrite );
	}

	public object GetValue( object obj )
	{
		if( Converter != null )
		{
			return Converter.ConvertToBlob( _property.GetValue( obj ) );
		}
		
		return _property.GetValue( obj );
	}

	public void SetValue( object obj, object value )
	{
		if( Converter != null )
		{
			_property.SetValue( obj, Converter.ConvertFromBlob( value as byte[] ) );
			return;
		}
		
		_property.SetValue( obj, value );
	}

	public override string ToString()
	{
		return _property.ToString();
	}
}

public interface IBlobTypeConverter
{
	byte[] ConvertToBlob( object   value );
	object ConvertFromBlob( byte[] value );
}

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
	
	public object ConvertFromBlob( byte[] blob )
	{
		using var stream = new MemoryStream( blob, true );
		using var reader = new BinaryReader( stream );

		var result = new List<double>();

		while( reader.BaseStream.Position < blob.Length )
		{
			result.Add( reader.ReadDouble() );
		}

		return result;
	}
}
