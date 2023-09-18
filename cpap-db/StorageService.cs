using System;
using System.Diagnostics;
using System.IO;

using cpaplib;

using SQLite;

namespace cpap_db
{
	public class StorageService : IDisposable
	{
		#region Private fields

		public SQLiteConnection _connection = null;

		private static Dictionary<System.Type, DatabaseMapping> _mappings = new();
		
		#endregion 
		
		#region Class Constructor

		static StorageService()
		{
			var mapping = CreateMapping<DailyReport>( "day" );
			mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

			mapping            = CreateMapping<FaultInfo>( "fault" );
			mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
			mapping.ForeignKey = new ForeignKeyColumn( "day", typeof( DateTime ), "day", "id" );
		}
		
		#endregion
		
		#region Instance Constructor 

		public StorageService( string databasePath )
		{
			// string myDocumentsPath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
			//
			// string applicationDataPath = Path.Combine( myDocumentsPath, databaseName );
			// if( !Directory.Exists( applicationDataPath ) )
			// {
			// 	Directory.CreateDirectory( applicationDataPath );
			// }
			//
			// string databasePath = Path.Combine( applicationDataPath, databaseName );

			_connection = new SQLiteConnection( databasePath );
			
			CreateTables( _connection );
		}

		#endregion
		
		#region Public functions
		
		public static DatabaseMapping CreateMapping<T>( string tableName = null ) where T : new()
		{
			var mapping = new DatabaseMapping<T>( tableName ?? typeof( T ).Name );
			_mappings[ typeof( T ) ] = mapping;

			return mapping;
		}

		public DateTime GetMostRecentDay()
		{
			var value = _connection.ExecuteScalar<long>( "SELECT ReportDate FROM Day ORDER BY ReportDate DESC LIMIT 1" );
			return DateTime.FromFileTimeUtc( value );
		}

		#endregion 
		
		#region IDisposable interface implementation
		
		public void Dispose()
		{
			_connection.Dispose();
			_connection = null;
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
	}
}
