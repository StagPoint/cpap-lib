using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using cpaplib;

using SQLite;

using SQLitePCL;

namespace cpap_db
{
	public class StorageService : IDisposable
	{
		#region Private fields

		public SQLiteConnection Connection { get; private set; } = null;

		private static Dictionary<System.Type, DatabaseMapping> _mappings = new();
		
		#endregion 
		
		#region Class Constructor

		static StorageService()
		{
			#region Create mappings for cpap-lib types 
			
			var mapping = CreateMapping<DailyReport>( "day" );
			mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

			mapping            = CreateMapping<FaultInfo>( "fault" );
			mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
			mapping.ForeignKey = new ForeignKeyColumn( "day", typeof( DateTime ), "day", "id" );
			
			#endregion 
		}
		
		#endregion
		
		#region Instance Constructor 

		public StorageService( string databasePath )
		{
			Connection = new SQLiteConnection( databasePath );
		}

		#endregion
		
		#region Public functions

		public DatabaseMapping GetMapping<T>()
		{
			if( _mappings.TryGetValue( typeof( T ), out DatabaseMapping mapping ) )
			{
				return mapping;
			}

			return null;
		}
		
		public bool CreateTable<T>()
		{
			var mapping = GetMapping<T>();
			if( mapping == null )
			{
				throw new NotSupportedException( $"No mapping has been defined for {typeof( T ).Name}" );
			}

			return mapping.CreateTable( Connection );
		}
		
		public static DatabaseMapping CreateMapping<T>( string tableName = null ) where T : new()
		{
			var mapping = new DatabaseMapping<T>( tableName ?? typeof( T ).Name );
			_mappings[ typeof( T ) ] = mapping;

			return mapping;
		}

		public DateTime GetMostRecentDay()
		{
			return Connection.ExecuteScalar<DateTime>( "SELECT ReportDate FROM Day ORDER BY ReportDate DESC LIMIT 1" );
		}

		public int Execute( string query, params object[] arguments )
		{
			return Connection.Execute( query, arguments );
		}

		public List<DataRow> Query( string query, params object[] arguments )
		{
			List<DataRow> result = new();

			var stmt = SQLite3.Prepare2( Connection.Handle, query );

			int argumentIndex = 1;
			foreach( var arg in arguments )
			{
				BindParameter( stmt, argumentIndex++, arg );
			}

			try
			{
				while( SQLite3.Step( stmt ) == SQLite3.Result.Row )
				{
					var row = new DataRow();

					int columnCount = SQLite3.ColumnCount( stmt );
					for( int index = 0; index < columnCount; ++index )
					{
						var columnName  = SQLite3.ColumnName( stmt, index );
						var columnType  = SQLite3.ColumnType( stmt, index );
						var columnValue = ReadColumn( Connection, stmt, index, columnType );

						row[ columnName ] = columnValue;
					}

					result.Add( row );
				}
			}
			finally
			{
				SQLite3.Finalize( stmt );
			}

			return result;
		}

		#endregion 
		
		#region Private functions

		internal static void BindParameter( sqlite3_stmt stmt, int index, object value )
		{
			switch( value )
			{
				case null:
					SQLite3.BindNull( stmt, index );
					break;
				case int val:
					SQLite3.BindInt( stmt, index, val );
					break;
				case string s:
					SQLite3.BindText( stmt, index, s, -1, new IntPtr( -1 ) );
					break;
				case byte _:
				case ushort _:
				case sbyte _:
				case short _:
					SQLite3.BindInt( stmt, index, Convert.ToInt32( value ) );
					break;
				case bool flag:
					SQLite3.BindInt( stmt, index, flag ? 1 : 0 );
					break;
				case uint _:
				case long _:
					SQLite3.BindInt64( stmt, index, Convert.ToInt64( value ) );
					break;
				case float _:
				case double _:
				case decimal _:
					SQLite3.BindDouble( stmt, index, Convert.ToDouble( value ) );
					break;
				case TimeSpan span:
					SQLite3.BindInt64( stmt, index, span.Ticks );
					break;
				case DateTime time:
					SQLite3.BindInt64( stmt, index, time.Ticks );
					break;
				case DateTimeOffset dateTimeOffset:
					SQLite3.BindInt64( stmt, index, dateTimeOffset.UtcTicks );
					break;
				case byte[] bytes:
					SQLite3.BindBlob( stmt, index, bytes, bytes.Length, new IntPtr( -1 ) );
					break;
				case Guid guid:
					SQLite3.BindText( stmt, index, guid.ToString(), 72, new IntPtr( -1 ) );
					break;
				case Uri uri:
					SQLite3.BindText( stmt, index, uri.ToString(), -1, new IntPtr( -1 ) );
					break;
				case StringBuilder builder:
					SQLite3.BindText( stmt, index, builder.ToString(), -1, new IntPtr( -1 ) );
					break;
				case UriBuilder builder:
					SQLite3.BindText( stmt, index, builder.ToString(), -1, new IntPtr( -1 ) );
					break;
				default:
					int int32 = Convert.ToInt32( value );
					SQLite3.BindInt( stmt, index, int32 );
					break;
			}
		}

		internal static object ReadColumn( SQLiteConnection connection, sqlite3_stmt stmt, int index, SQLite3.ColType type )
		{
			return type switch
			{
				SQLite3.ColType.Integer => SQLite3.ColumnInt( stmt, index ),
				SQLite3.ColType.Float   => SQLite3.ColumnDouble( stmt, index ),
				SQLite3.ColType.Text    => SQLite3.ColumnString( stmt, index ),
				SQLite3.ColType.Blob    => SQLite3.ColumnByteArray( stmt, index ),
				SQLite3.ColType.Null    => null,
				_                       => throw new ArgumentOutOfRangeException( nameof( type ), type, null )
			};
		}
		
		internal static object ReadColumn( SQLiteConnection connection, sqlite3_stmt stmt, int index, SQLite3.ColType type, Type clrType )
		{
			if( type == SQLite3.ColType.Null )
			{
				return null;
			}

			TypeInfo typeInfo = clrType.GetTypeInfo();

			if( typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof( Nullable<> ) )
			{
				clrType  = typeInfo.GenericTypeArguments[ 0 ];
				typeInfo = clrType.GetTypeInfo();
			}

			if( clrType == typeof( string ) )
			{
				return SQLite3.ColumnString( stmt, index );
			}

			if( clrType == typeof( int ) )
			{
				return SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( bool ) )
			{
				return (SQLite3.ColumnInt( stmt, index ) == 1);
			}

			if( clrType == typeof( double ) )
			{
				return SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( float ) )
			{
				return (float)SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( TimeSpan ) )
			{
				if( connection.StoreTimeSpanAsTicks )
				{
					return new TimeSpan( SQLite3.ColumnInt64( stmt, index ) );
				}
			}

			if( clrType == typeof( DateTime ) )
			{
				return new DateTime( SQLite3.ColumnInt64( stmt, index ) );
			}

			if( clrType == typeof( DateTimeOffset ) )
			{
				return new DateTimeOffset( SQLite3.ColumnInt64( stmt, index ), TimeSpan.Zero );
			}

			if( typeInfo.IsEnum )
			{
				if( type != SQLite3.ColType.Text )
				{
					return SQLite3.ColumnInt( stmt, index );
				}

				string str = SQLite3.ColumnString( stmt, index );
				return Enum.Parse( clrType, str.ToString(), true );
			}

			if( clrType == typeof( long ) )
			{
				return SQLite3.ColumnInt64( stmt, index );
			}

			if( clrType == typeof( uint ) )
			{
				return (uint)SQLite3.ColumnInt64( stmt, index );
			}

			if( clrType == typeof( Decimal ) )
			{
				return (Decimal)SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( byte ) )
			{
				return (byte)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( ushort ) )
			{
				return (ushort)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( short ) )
			{
				return (short)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( sbyte ) )
			{
				return (sbyte)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( byte[] ) )
			{
				return SQLite3.ColumnByteArray( stmt, index );
			}

			if( clrType == typeof( Guid ) )
			{
				return new Guid( SQLite3.ColumnString( stmt, index ) );
			}

			if( clrType == typeof( Uri ) )
			{
				return new Uri( SQLite3.ColumnString( stmt, index ) );
			}

			throw new NotSupportedException( $"Unhandled type {clrType}" );
		}

		#endregion 
		
		#region IDisposable interface implementation
		
		public void Dispose()
		{
			Connection.Dispose();
			Connection = null;
		}
		
		#endregion 
		
		#region Private functions

		private void CreateTables( SQLiteConnection connection )
		{
			// List<SQLiteConnection.ColumnInfo> columns = connection.GetTableInfo( "Day");
			// if( columns == null || columns.Count == 0 )
			// {
			// 	connection.CreateTable<DbDayRecord>();
			// }
		}
		
		#endregion
		
		#region Nested types

		public class DataRow
		{
			#region Public properties
			
			public int ColumnCount { get => _columns.Count; }
			
			#endregion 
			
			#region Private fields

			private List<KeyValuePair<string, object>> _columns = new();

			#endregion
			
			#region Public functions

			public string GetColumnName( int index )
			{
				return _columns[ index ].Key;
			}

			public object GetValue( int index )
			{
				return _columns[ index ].Value;
			}
			
			#endregion 
			
			#region Indexer functions

			public object this[ int index ]
			{
				get => _columns[ index ].Value;
				set => _columns[ index ] = new KeyValuePair<string, object>( _columns[ index ].Key, value );
			}

			public object this[ string key ]
			{
				get
				{
					foreach( var column in _columns )
					{
						if( column.Key.Equals( key, StringComparison.Ordinal ) )
						{
							return column.Value;
						}
					}

					throw new KeyNotFoundException( $"There is no column named {key}" );
				}
				set
				{
					for( int i = 0; i < _columns.Count; i++ )
					{
						if( _columns[ i ].Key.Equals( key, StringComparison.Ordinal ) )
						{
							_columns[ i ] = new KeyValuePair<string, object>( _columns[ i ].Key, value );
							return;
						}
					}

					_columns.Add( new KeyValuePair<string, object>( key, value ) );
				}
			}
			
			#endregion 
		}
		
		#endregion 
	}
}
