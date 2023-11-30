using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Text;

using SQLite;

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

	internal string CreateTableQuery
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

	internal string DeleteQuery
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

	internal string SelectAllQuery
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

	internal string SelectByPrimaryKeyQuery
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

	internal string SelectByForeignKeyQuery
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

	internal string InsertQuery
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

	internal string UpdateQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _updateQuery ) )
			{
				_updateQuery = GenerateUpdateQuery();
			}

			return _updateQuery;
		}
	}

	#endregion

	#region Private fields

	private string _createTableQuery        = string.Empty;
	private string _insertQuery             = string.Empty;
	private string _updateQuery             = string.Empty;
	private string _deleteQuery             = string.Empty;
	private string _selectAllQuery          = string.Empty;
	private string _selectByPrimaryKeyQuery = string.Empty;
	private string _selectByForeignKeyQuery = string.Empty;

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

	internal bool CreateTable( SQLiteConnection connection, bool updateTableSchema = true )
	{
		List<SQLiteConnection.ColumnInfo> tableInfo = connection.GetTableInfo( TableName );
		if( tableInfo.Count == 0 )
		{
			connection.Execute( CreateTableQuery );

			return true;
		}

		if( !updateTableSchema )
		{
			return false;
		}

		bool tableAltered = false;

		// If the mapping contains columns that do not (yet) exist in the database, we will
		// automatically issue ALTER TABLE commands to add them. We do not add primary or 
		// foreign keys here, and any NOT NULL column must have a default value that can 
		// be specified 
		if( tableInfo.Count != ColumnCount )
		{
			foreach( var column in Columns )
			{
				// check to see if the mapped column exists in the tableInfo list 
				if( !tableInfo.Any( x => x.Name.Equals( column.ColumnName, StringComparison.OrdinalIgnoreCase ) ) )
				{
					var sqlType = GetSqlType( column.Type, column.MaxStringLength );
					var sql     = $"ALTER TABLE [{TableName}] ADD COLUMN [{column.ColumnName}] {sqlType}";

					if( !column.IsNullable )
					{
						string defaultValue = column.Type == typeof( string ) ? "''" : "0"; 
						sql += $" NOT NULL DEFAULT ({defaultValue})";
					}

					connection.Execute( sql );
					tableAltered = true;
				}
			}
			
			#region DROP COLUMN is not supported by all versions of SQLite 
			
			// TODO: Consider whether it's worthwhile to alter the table using the "SELECT INTO TEMP, DROP TABLE, RECREATE TABLE, REPOPULATE FROM TEMP" pattern to alter table structure.

			// // Conversely, if there are columns in the database that no longer appear in the mapping, 
			// // we will automatically issue ALTER TABLE commands to remove them. 
			// foreach( var column in tableInfo )
			// {
			// 	if( Columns.Any( x => x.ColumnName.Equals( column.Name, StringComparison.OrdinalIgnoreCase ) ) )
			// 	{
			// 		continue;
			// 	}
			//
			// 	if( PrimaryKey != null && PrimaryKey.ColumnName.Equals( column.Name, StringComparison.OrdinalIgnoreCase ) )
			// 	{
			// 		continue;
			// 	}
			//
			// 	if( ForeignKey != null && ForeignKey.ColumnName.Equals( column.Name, StringComparison.OrdinalIgnoreCase ) )
			// 	{
			// 		continue;
			// 	}
			// 	
			// 	var sql = $"ALTER TABLE [{TableName}] DROP [{column.Name}]";
			// 	
			// 	connection.Execute( sql );
			// 	
			// 	tableAltered = true;
			// }
			
			#endregion 
		}
		
		return tableAltered;
	}

	#endregion 
	
	#region Private functions

	private string GenerateSelectByPrimaryKeyQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return SelectAllQuery + $"\n WHERE \n\t[{PrimaryKey.ColumnName}] = ?";
	}

	private string GenerateSelectByForeignKeyQuery()
	{
		if( ForeignKey == null )
		{
			throw new Exception( $"No foreign key has been defined for table {TableName}" );
		}

		return SelectAllQuery + $"\n WHERE \n\t[{ForeignKey.ColumnName}] = ?";
	}

	private string GenerateDeleteQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return $"DELETE FROM [{TableName}] WHERE [{PrimaryKey.ColumnName}] = ?";
	}

	private string GenerateSelectAllQuery()
	{
		var builder = new StringBuilder();
		builder.Append( "SELECT " );

		bool firstColumn = true;

		if( PrimaryKey != null )
		{
			builder.Append( $"\n\t[{PrimaryKey.ColumnName}]" );
			firstColumn = false;
		}

		if( ForeignKey != null )
		{
			if( !firstColumn )
			{
				builder.Append( ',' );
			}
			
			builder.Append( $"\n\t[{ForeignKey.ColumnName}]" );
			firstColumn = false;
		}

		foreach( var column in Columns )
		{
			if( !firstColumn )
			{
				builder.Append( ',' );
			}
			
			builder.Append( $"\n\t[{column.ColumnName}]" );
			firstColumn = false;
		}

		builder.Append( $"\nFROM \n\t[{TableName}]" );

		return builder.ToString();
	}

	private string GenerateInsertQuery()
	{
		var builder    = new StringBuilder();
		var parameters = new StringBuilder();

		bool hasPreviousColumn = false;

		builder.Append( $"INSERT INTO [{TableName}] ( " );

		if( PrimaryKey != null && !PrimaryKey.AutoIncrement )
		{
			builder.Append( $"[{PrimaryKey.ColumnName}]" );
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

			builder.Append( $"[{ForeignKey.ColumnName}]" );
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

			builder.Append( $"[{column.ColumnName}]" );
			parameters.Append( '?' );

			hasPreviousColumn = true;
		}

		builder.Append( $" ) VALUES ( {parameters} )" );

		return builder.ToString();
	}

	private string GenerateUpdateQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"Cannot execute an UPDATE statement without a Primary Key for {ObjectType}" );
		}
		
		var builder    = new StringBuilder();

		builder.Append( $"UPDATE [{TableName}] \nSET\n" );

		for( int i = 0; i < Columns.Count; i++ )
		{
			var column = Columns[ i ];
			
			builder.Append( $"    [{column.ColumnName}] = ?" );

			if( i < Columns.Count - 1 )
			{
				builder.Append( ',' );
			}

			builder.Append( '\n' );
		}

		builder.Append( $"WHERE [{PrimaryKey.ColumnName}] = ?\n" );

		return builder.ToString();
	}

	private string GenerateCreateTableQuery()
	{
		var builder = new StringBuilder();

		builder.Append( $"CREATE TABLE IF NOT EXISTS [{TableName}] (\n" );

		var first = true;

		if( PrimaryKey != null )
		{
			builder.Append( $"[{PrimaryKey.ColumnName}] {PrimaryKey.DbType} PRIMARY KEY" );

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
			builder.Append( $"[{ForeignKey.ColumnName}]" );

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
			builder.Append( $"[{column.ColumnName}]" );

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

		if( ForeignKey != null && ForeignKey.IsForeignKeyConstraintEnforced )
		{
			if( !first )
			{
				builder.Append( ", " );
			}

			builder.Append( $"\nFOREIGN KEY( [{ForeignKey.ColumnName}] ) REFERENCES [{ForeignKey.ReferencedTable}]( [{ForeignKey.ReferencedField}] )" );

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
		var underlyingType = Nullable.GetUnderlyingType( columnType );
		if( underlyingType != null )
		{
			columnType = underlyingType;
		}

		var isIntegerType = columnType == typeof( bool ) || columnType == typeof( byte ) || columnType == typeof( ushort ) ||
		                    columnType == typeof( sbyte ) || columnType == typeof( short ) || columnType == typeof( int ) ||
		                    columnType == typeof( uint ) || columnType == typeof( long ) || columnType == typeof( Color );
		if( isIntegerType )
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
	where T: class, new()
{
	#region Constructors

	public DatabaseMapping( string tableName ) 
	{
		ObjectType = typeof( T );
		TableName  = tableName;

		var properties = ObjectType.GetProperties( BindingFlags.Instance | BindingFlags.Public );

		foreach( var property in properties )
		{
			if( property.GetCustomAttribute<IgnoreAttribute>() != null )
			{
				continue;
			}
			
			if( property.CanRead && property.CanWrite )
			{
				if( property.PropertyType.IsValueType || property.PropertyType == typeof( string ) )
				{
					var fitsPrimaryKeyColumnNamingPattern =
						property.Name.Equals( $"{ObjectType.Name}ID", StringComparison.OrdinalIgnoreCase ) ||
						property.Name.Equals( "_id",                  StringComparison.OrdinalIgnoreCase ) ||
						property.Name.Equals( "id",                   StringComparison.OrdinalIgnoreCase );
					
					if( fitsPrimaryKeyColumnNamingPattern )
					{
						var isIntegerProperty = property.PropertyType.GetTypeInfo().GetInterfaces().Any( i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof( INumber<> )) );

						PrimaryKey                  = new PrimaryKeyColumn( property.Name, property.PropertyType, isIntegerProperty );
						PrimaryKey.PropertyAccessor = property;
					}
					else
					{
						// TODO: This doesn't seem to handle nullable strings properly? Noticed at one point, don't have a repro unfortunately.
						
						var newColumn = new ColumnMapping( property.Name, property );
						newColumn.IsNullable = Nullable.GetUnderlyingType( property.PropertyType ) != null;
						
						Columns.Add( newColumn );
					}
				}
			}
		}
	}

	#endregion

	#region Public functions

	public T ExtractFromDataRow( StorageService.DataRow data )
	{
		var obj = new T();
		var map = data.ToDictionary();

		foreach( var column in Columns )
		{
			if( map.TryGetValue( column.ColumnName, out object value ) )
			{
				value = StorageService.ConvertValue( value, data.GetColumnType( column.ColumnName ), column.Type );				
				column.SetValue( obj, value );
			}
		}

		return obj;
	}

	public List<T> ExtractFromDataRows( List<StorageService.DataRow> rows )
	{
		var list = new List<T>( rows.Count );

		foreach( var row in rows )
		{
			list.Add( ExtractFromDataRow( row ) );
		}

		return list;
	}

	internal List<T> ExecuteQuery<P>( SQLiteConnection connection, string query, IList<P> primaryKeys, params object[] arguments ) 
		where P : struct
	{
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
					
					var columnMapping = GetColumnByName( columnName );
					if( columnMapping != null )
					{
						var val = StorageService.ReadColumn( stmt, index, columnType, columnMapping.Type );

						columnMapping.SetValue( obj, val );
					}
					else if( PrimaryKey != null && primaryKeys != null && columnName.Equals( PrimaryKey.ColumnName, StringComparison.OrdinalIgnoreCase ) )
					{
						var val = StorageService.ReadColumn( stmt, index, columnType, PrimaryKey.Type );
						primaryKeys.Add( (P)val );

						if( PrimaryKey.PropertyAccessor != null )
						{
							PrimaryKey.PropertyAccessor.SetValue( obj, val );
						}
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

	internal List<T> ExecuteQuery( SQLiteConnection connection, string query, params object[] arguments )
	{
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
					var columnName  = SQLite3.ColumnName( stmt, index );
					var columnType  = SQLite3.ColumnType( stmt, index );
					
					var columnMapping  = GetColumnByName( columnName );
					if( columnMapping != null )
					{
						var columnValue = StorageService.ReadColumn( stmt, index, columnType, columnMapping.Type );
						columnMapping.SetValue( obj, columnValue );
					}
					else if( PrimaryKey != null && PrimaryKey.ColumnName.Equals( columnName, StringComparison.OrdinalIgnoreCase ) )
					{
						if( PrimaryKey.PropertyAccessor != null )
						{
							var columnValue = StorageService.ReadColumn( stmt, index, columnType, PrimaryKey.Type );
							PrimaryKey.PropertyAccessor.SetValue( obj, columnValue );
						}
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

	internal bool Delete( SQLiteConnection connection, object primaryKeyValue )
	{
		var rows = connection.Execute( DeleteQuery, primaryKeyValue );
		
		return rows > 0;
	}

	internal List<T> SelectAll( SQLiteConnection connection )
	{
		return ExecuteQuery( connection, SelectAllQuery );
	}

	internal T SelectByPrimaryKey( SQLiteConnection connection, object primaryKeyValue )
	{
		var rows = ExecuteQuery( connection, SelectByPrimaryKeyQuery, primaryKeyValue );

		if( rows == null || rows.Count == 0 )
		{
			return null;
		}

		return rows[ 0 ];
	}

	internal List<T> SelectAllByPrimaryKey( SQLiteConnection connection, object primaryKeyValue )
	{
		return ExecuteQuery( connection, SelectByPrimaryKeyQuery, primaryKeyValue );
	}

	internal List<T> SelectByForeignKey<P>( SQLiteConnection connection, object foreignKeyValue, IList<P> primaryKeys = null ) 
		where P: struct
	{
		return ExecuteQuery( connection, SelectByForeignKeyQuery, primaryKeys, foreignKeyValue );
	}

	internal List<T> SelectByForeignKey( SQLiteConnection connection, object foreignKeyValue ) 
	{
		return ExecuteQuery( connection, SelectByForeignKeyQuery, foreignKeyValue );
	}

	internal int Update( SQLiteConnection connection, object data, object primaryKeyValue )
	{
		object[] args = new object[ Columns.Count + 1 ];

		int nextColumnIndex = 0;
		
		foreach( var column in Columns )
		{
			args[ nextColumnIndex++ ] = column.GetValue( data );
		}

		args[ nextColumnIndex ] = primaryKeyValue;

		string query = UpdateQuery;

		int rowCount = connection.Execute( query, args );

		return rowCount;
	}

	internal int Insert( SQLiteConnection connection, object data, object primaryKeyValue = null, object foreignKeyValue = null )
	{
		int columnCount = Columns.Count;
		if( PrimaryKey is { AutoIncrement: false } )
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
}
