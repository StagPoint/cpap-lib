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
		
		#endregion 
		
		#region Class constructor

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

		public DateTime GetMostRecentDay()
		{
			var value = _connection.ExecuteScalar<long>( "SELECT ReportDate FROM Day ORDER BY ReportDate DESC LIMIT 1" );
			return DateTime.FromFileTimeUtc( value );
		}

		public void Save( DailyReport day )
		{
			_connection.InsertOrReplace( new DbDayRecord( day ) );
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
