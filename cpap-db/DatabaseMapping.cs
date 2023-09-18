using System.Globalization;
using System.Reflection;
using System.Text;

using SQLite;

using SQLitePCL;

namespace cpap_db;

public class DatabaseMapping
{
	#region Public properties

	public string TableName { get; set; }

	public System.Type ObjectType { get; set; }

	public PrimaryKeyColumn PrimaryKey = null;

	public ForeignKeyColumn ForeignKey = null;

	public List<ColumnMapping> Columns = new List<ColumnMapping>();

	public int ColumnCount
	{
		get => Columns.Count + (PrimaryKey != null ? 1 : 0) + (ForeignKey != null ? 1 : 0);
	}

	public string CreateTableQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _createTableQuery ) )
			{
				_createTableQuery = GenerateCreateTableQuery();
			}

			return _createTableQuery;
		}
	}

	public string DeleteQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _deleteQuery ) )
			{
				_deleteQuery = GenerateDeleteQuery();
			}

			return _deleteQuery;
		}
	}

	public string SelectAllQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _selectAllQuery ) )
			{
				_selectAllQuery = GenerateSelectAllQuery();
			}

			return _selectAllQuery;
		}
	}

	public string SelectByPrimaryKeyQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _selectByPrimaryKeyQuery ) )
			{
				_selectByPrimaryKeyQuery = GenerateSelectByPrimaryKeyQuery();
			}

			return _selectByPrimaryKeyQuery;
		}
	}

	public string SelectByForeignKeyQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _selectByForeignKeyQuery ) )
			{
				_selectByForeignKeyQuery = GenerateSelectByForeignKeyQuery();
			}

			return _selectByForeignKeyQuery;
		}
	}

	public string InsertQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _insertQuery ) )
			{
				_insertQuery = GenerateInsertQuery();
			}

			return _insertQuery;
		}
	}

	#endregion

	#region Private fields

	private string _createTableQuery        = String.Empty;
	private string _insertQuery             = String.Empty;
	private string _deleteQuery             = String.Empty;
	private string _selectAllQuery          = String.Empty;
	private string _selectByPrimaryKeyQuery = String.Empty;
	private string _selectByForeignKeyQuery = String.Empty;

	#endregion

	#region Constructors

	public DatabaseMapping( string tableName )
	{
		TableName = tableName;
	}

	public DatabaseMapping( string tableName, Type dataType ) : this( tableName )
	{
		ObjectType = dataType;

		var properties = dataType.GetProperties( BindingFlags.Instance | BindingFlags.Public );

		foreach( var property in properties )
		{
			if( property.CanRead && property.CanWrite )
			{
				if( property.PropertyType.IsValueType )
				{
					Columns.Add( new ColumnMapping( property.Name, property ) );
				}
			}
		}
	}

	#endregion

	#region Public functions

	public ColumnMapping GetColumnByName( string name )
	{
		foreach( var column in Columns )
		{
			if( string.Compare( column.ColumnName, name, StringComparison.OrdinalIgnoreCase ) == 0 )
			{
				return column;
			}
		}

		return null;
	}

	public bool CreateTable( SQLiteConnection connection )
	{
		List<SQLiteConnection.ColumnInfo> tableInfo = connection.GetTableInfo( TableName );
		if( tableInfo.Count == 0 )
		{
			connection.Execute( CreateTableQuery );

			return true;
		}

		return false;
	}

	public List<T> ExecuteQuery<T>( SQLiteConnection connection, string query, params object[] arguments ) where T : new()
	{
		if( typeof( T ) != ObjectType )
		{
			throw new Exception( $"Type {typeof( T ).Name} does not match the mapping for type {ObjectType.Name}" );
		}

		List<T> result = new();

		var stmt = SQLite3.Prepare2( connection.Handle, query );

		int argumentIndex = 1;
		foreach( var arg in arguments )
		{
			StorageService.BindParameter( stmt, argumentIndex++, arg );
		}

		try
		{
			while( SQLite3.Step( stmt ) == SQLite3.Result.Row )
			{
				var obj = Activator.CreateInstance<T>();

				int columnCount = SQLite3.ColumnCount( stmt );
				for( int index = 0; index < columnCount; ++index )
				{
					var columnName = SQLite3.ColumnName( stmt, index );
					var columnType = SQLite3.ColumnType( stmt, index );
					
					var columnMapping  = GetColumnByName( columnName );
					if( columnMapping != null )
					{
						var val = StorageService.ReadColumn( connection, stmt, index, columnType, columnMapping.Type );

						columnMapping.SetValue( obj, val );
					}
				}

				result.Add( obj );
			}
		}
		finally
		{
			SQLite3.Finalize( stmt );
		}

		return result;
	}

	public T SelectByPrimaryKey<T>( SQLiteConnection connection, object primaryKeyValue ) where T: class, new()
	{
		var rows = ExecuteQuery<T>( connection, SelectByPrimaryKeyQuery, primaryKeyValue );

		if( rows == null || rows.Count == 0 )
		{
			return null;
		}

		return rows[ 0 ];
	}

	public List<T> SelectByForeignKey<T>( SQLiteConnection connection, object foreignKeyValue ) where T: class, new()
	{
		return ExecuteQuery<T>( connection, SelectByForeignKeyQuery, foreignKeyValue );
	}

	public int Insert( SQLiteConnection connection, object data, object primaryKeyValue = null, object foreignKeyValue = null )
	{
		int columnCount = Columns.Count;
		if( PrimaryKey != null && !PrimaryKey.AutoIncrement )
		{
			columnCount += 1;
		}

		if( ForeignKey != null )
		{
			columnCount += 1;
		}

		object[] args = new object[ columnCount ];

		int nextColumnIndex = 0;

		if( PrimaryKey is { AutoIncrement: false } )
		{
			args[ nextColumnIndex++ ] = primaryKeyValue ?? throw new Exception( $"You must provide a primary key value for {TableName}.{PrimaryKey.ColumnName}" );
		}

		if( ForeignKey != null )
		{
			args[ nextColumnIndex++ ] = foreignKeyValue ?? throw new Exception( $"You must provide a foreign key value for table {TableName}.{ForeignKey.ColumnName}" );
		}

		foreach( var column in Columns )
		{
			args[ nextColumnIndex++ ] = column.GetValue( data );
		}

		string query = InsertQuery;

		int rowCount = connection.Execute( query, args );

		if( PrimaryKey is { AutoIncrement: true } )
		{
			return (int)SQLite3.LastInsertRowid( connection.Handle );
		}

		return rowCount;
	}

	#endregion

	#region Private functions

	private string GenerateSelectByPrimaryKeyQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return $"SELECT * FROM \"{TableName}\" WHERE {PrimaryKey.ColumnName} = ?;";
	}

	private string GenerateSelectByForeignKeyQuery()
	{
		if( ForeignKey == null )
		{
			throw new Exception( $"No foreign key has been defined for table {TableName}" );
		}

		return $"SELECT * FROM \"{TableName}\" WHERE {ForeignKey.ColumnName} = ?;";
	}

	private string GenerateDeleteQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return $"DELETE FROM \"{TableName}\" WHERE {PrimaryKey.ColumnName} = ?";
	}

	private string GenerateSelectAllQuery()
	{
		return $"SELECT * FROM \"{TableName}\"";
	}

	private string GenerateInsertQuery()
	{
		var builder    = new StringBuilder();
		var parameters = new StringBuilder();

		bool hasPreviousColumn = false;

		builder.Append( $"INSERT INTO \"{TableName}\" ( " );

		if( PrimaryKey != null && !PrimaryKey.AutoIncrement )
		{
			builder.Append( PrimaryKey.ColumnName );
			parameters.Append( '?' );

			hasPreviousColumn = true;
		}

		if( ForeignKey != null )
		{
			if( hasPreviousColumn )
			{
				builder.Append( ", " );
				parameters.Append( ", " );
			}

			builder.Append( ForeignKey.ColumnName );
			parameters.Append( '?' );

			hasPreviousColumn = true;
		}

		foreach( var column in Columns )
		{
			if( hasPreviousColumn )
			{
				builder.Append( ", " );
				parameters.Append( ", " );
			}

			builder.Append( $"\"{column.ColumnName}\"" );
			parameters.Append( '?' );

			hasPreviousColumn = true;
		}

		builder.Append( $" ) VALUES ( {parameters} )" );

		return builder.ToString();
	}

	private string GenerateCreateTableQuery()
	{
		var builder = new StringBuilder();

		builder.Append( $"CREATE TABLE IF NOT EXISTS \"{TableName}\" (\n" );

		var first = true;

		if( PrimaryKey != null )
		{
			builder.Append( $"{PrimaryKey.ColumnName} {PrimaryKey.DbType} PRIMARY KEY" );

			if( PrimaryKey.AutoIncrement )
			{
				builder.Append( " AUTOINCREMENT" );
			}

			if( !PrimaryKey.IsNullable )
			{
				builder.Append( "  NOT NULL" );
			}

			first = false;
		}

		if( ForeignKey != null )
		{
			if( !first )
			{
				builder.Append( ",\n" );
			}
			first = false;

			builder.Append( ' ' );
			builder.Append( ForeignKey.ColumnName );

			builder.Append( ' ' );
			builder.Append( GetSqlType( ForeignKey.Type, null ) );

			builder.Append( " NOT NULL" );
		}

		foreach( var column in Columns )
		{
			if( !first )
			{
				builder.Append( ",\n" );
			}
			first = false;

			builder.Append( ' ' );
			builder.Append( $"\"{column.ColumnName}\"" );

			builder.Append( ' ' );
			if( column.Converter != null )
			{
				builder.Append( "BLOB" );
			}
			else
			{
				builder.Append( GetSqlType( column.Type, column.MaxStringLength ) );
			}

			if( !column.IsNullable )
			{
				builder.Append( " NOT NULL" );
			}

			if( column.IsUnique )
			{
				builder.Append( " UNIQUE" );
			}
		}

		if( ForeignKey != null )
		{
			if( !first )
			{
				builder.Append( ", " );
			}

			builder.Append( $"\nFOREIGN KEY({ForeignKey.ColumnName}) REFERENCES {ForeignKey.ReferencedTable}({ForeignKey.ReferencedField})" );

			if( !string.IsNullOrEmpty( ForeignKey.OnDeleteAction ) )
			{
				builder.Append( "\n\tON DELETE " );
				builder.Append( ForeignKey.OnDeleteAction );
			}

			if( !string.IsNullOrEmpty( ForeignKey.OnUpdateAction ) )
			{
				builder.Append( "\n\tON UPDATE " );
				builder.Append( ForeignKey.OnUpdateAction );
			}
		}

		builder.Append( " )" );

		return builder.ToString();
	}

	internal static string GetSqlType( System.Type columnType, int? maxStringLength = null )
	{
		if( columnType == typeof( bool ) || columnType == typeof( byte ) || columnType == typeof( ushort ) || columnType == typeof( sbyte ) || columnType == typeof( short ) || columnType == typeof( int ) || columnType == typeof( uint ) || columnType == typeof( long ) )
		{
			return "INTEGER";
		}

		if( columnType == typeof( float ) || columnType == typeof( double ) || columnType == typeof( decimal ) )
		{
			return "FLOAT";
		}

		if( columnType == typeof( string ) || columnType == typeof( Uri ) )
		{
			if( maxStringLength.HasValue )
			{
				return $"VARCHAR({maxStringLength.Value})";
			}

			return "VARCHAR";
		}

		if( columnType == typeof( TimeSpan ) || columnType == typeof( DateTime ) || columnType == typeof( DateTimeOffset ) )
		{
			return "BIGINT";
		}

		if( columnType.GetTypeInfo().IsEnum )
		{
			return "INTEGER";
		}

		if( columnType == typeof( byte[] ) )
		{
			return "BLOB";
		}

		if( columnType == typeof( Guid ) )
		{
			return "varchar(36)";
		}

		throw new NotSupportedException( $"Unhandled CLR type {columnType}" );
	}

	#endregion
}

public class DatabaseMapping<T> : DatabaseMapping
{
	public DatabaseMapping( string tableName ) : base( tableName, typeof( T ) )
	{
	}
}
