using System.Diagnostics;
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
	public Type   Type            { get; set; }
	public int?   MaxStringLength { get; set; }
	public bool   IsUnique        { get; set; }
	public bool   IsNullable      { get; set; }

	private PropertyInfo _property;

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
		return _property.GetValue( obj );
	}

	public void SetValue( object obj, object value )
	{
		_property.SetValue( obj, value );
	}

	public override string ToString()
	{
		return _property.ToString();
	}
}
