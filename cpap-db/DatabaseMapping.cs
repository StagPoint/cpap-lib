using System;
using System.Reflection;
using System.Collections.Generic;
using System.Data.Common;
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

	public string SelectByIdQuery
	{
		get
		{
			if( string.IsNullOrEmpty( _selectByIdQuery ) )
			{
				_selectByIdQuery = GenerateSelectByIdQuery();
			}

			return _selectByIdQuery;
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
	
	private string _createTableQuery = String.Empty;
	private string _insertQuery = String.Empty;
	private string _deleteQuery = String.Empty;
	private string _selectAllQuery = String.Empty;
	private string _selectByIdQuery = String.Empty;
	
	#endregion 
	
	#region Constructors 

	public DatabaseMapping( string tableName )
	{
		TableName = tableName;
	}

	public DatabaseMapping( string tableName, Type dataType ) : this( tableName )
	{
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

		if( PrimaryKey != null && !PrimaryKey.AutoIncrement )
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

		return connection.Execute( query, args );
	}
	
	#endregion 
	
	#region Private functions

	private string GenerateSelectByIdQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return $"SELECT * FROM {TableName} WHERE {PrimaryKey.ColumnName} = ?";
	}

	private string GenerateDeleteQuery()
	{
		if( PrimaryKey == null )
		{
			throw new Exception( $"No primary key has been defined for table {TableName}" );
		}

		return $"DELETE FROM {TableName} WHERE {PrimaryKey.ColumnName} = ?";
	}

	private string GenerateSelectAllQuery()
	{
		return $"SELECT * FROM {TableName}";
	}

	public string GenerateInsertQuery()
	{
		var builder    = new StringBuilder();
		var parameters = new StringBuilder();

		bool hasPreviousColumn = false;

		builder.Append( $"INSERT INTO {TableName} ( " );

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

			builder.Append( column.ColumnName );
			parameters.Append( '?' );

			hasPreviousColumn = true;
		}

		builder.Append( $" ) VALUES ( {parameters} )" );

		return builder.ToString();
	}

	private string GenerateCreateTableQuery()
	{
		var builder = new StringBuilder();

		builder.Append( $"CREATE TABLE IF NOT EXISTS {TableName} (\n" );

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
		
		foreach( var column in Columns )
		{
			if( !first )
			{
				builder.Append( ",\n" );
			}
			first = false;
			
			builder.Append( ' ' );
			builder.Append( column.ColumnName );

			builder.Append( ' ' );
			builder.Append( GetSqlType( column.Type, column.MaxStringLength ) );

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
