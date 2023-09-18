using System.Diagnostics;

using cpap_db;

using cpaplib;

namespace cpaplib_tests;

[TestClass]
public class DatabaseTests
{
	public class Report
	{
		public DateTime ReportDate { get; set; }
		public double   Hours      { get; set; }
		public DateTime StartTime  { get; set; }
		public DateTime EndTime    { get; set; }
		public TimeSpan Duration   { get; set; }
	}

	public class EventFlag
	{
		public int      Type      { get; set; }
		public DateTime StartTime { get; set; }
		public TimeSpan Duration  { get; set; }
	}
	
	[TestMethod]
	public void CreateTable_Simple()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );

		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var mapping = new DatabaseMapping<FaultInfo>( "Fault" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );

				var tableCreated = mapping.CreateTable( db._connection );
				Assert.IsTrue( tableCreated );

				var columns = db._connection.GetTableInfo( "Fault" );
				Assert.AreEqual( 5, columns.Count );
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( e );
			throw;
		}
		finally
		{
			File.Delete( databasePath );
		}
	}

	[TestMethod]
	public void CreateTableAndInsert()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var mapping = new DatabaseMapping<FaultInfo>( "Fault" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );

				var tableCreated = mapping.CreateTable( db._connection );
				Assert.IsTrue( tableCreated );

				var columns = db._connection.GetTableInfo( "Fault" );
				Assert.AreEqual( 5, columns.Count );

				var data = new FaultInfo
				{
					Device     = 1,
					Alarm      = 2,
					Humidifier = 3,
					HeatedTube = 4
				};

				int rowID = mapping.Insert( db._connection, data );
				
				Assert.AreEqual( 1, rowID );
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( e );
			throw;
		}
		finally
		{
			File.Delete( databasePath );
		}
	}
	
	[TestMethod]
	public void PrimaryKeyAutoIncrements()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var mapping = new DatabaseMapping<FaultInfo>( "Fault" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );

				var tableCreated = mapping.CreateTable( db._connection );
				Assert.IsTrue( tableCreated );

				var columns = db._connection.GetTableInfo( "Fault" );
				Assert.AreEqual( 5, columns.Count );

				var data = new FaultInfo
				{
					Device     = 1,
					Alarm      = 2,
					Humidifier = 3,
					HeatedTube = 4
				};

				for( int i = 1; i < 10; i++ )
				{
					int rowID = mapping.Insert( db._connection, data );
					Assert.AreEqual( i, rowID );
				}
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( e );
			throw;
		}
		finally
		{
			File.Delete( databasePath );
		}
	}

	[TestMethod]
	public void CanStoreDateTimeFields()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var mapping = new DatabaseMapping<Report>( "report" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

				var tableCreated = mapping.CreateTable( db._connection );
				Assert.IsTrue( tableCreated );

				var columns = db._connection.GetTableInfo( "report" );
				Assert.AreEqual( 6, columns.Count );

				var data = new Report
				{
					ReportDate = DateTime.Today,
					Hours      = 5,
					StartTime  = DateTime.Today.AddHours( 8 ),
					EndTime    = DateTime.Today.AddHours( 13 ),
					Duration   = TimeSpan.FromHours( 5 )
				};
				
				int rowCount = mapping.Insert( db._connection, data, primaryKeyValue: DateTime.Today );
				
				Assert.AreEqual( 1, rowCount );
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( e );
			throw;
		}
		finally
		{
			File.Delete( databasePath );
		}
	}

	[TestMethod]
	public void CanRetrieveDateTimeFields()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var mapping = new DatabaseMapping<Report>( "report" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

				var tableCreated = mapping.CreateTable( db._connection );
				Assert.IsTrue( tableCreated );

				var columns = db._connection.GetTableInfo( "report" );
				Assert.AreEqual( 6, columns.Count );

				var primaryKey = DateTime.Today.AddDays( -30 );

				var data = new Report
				{
					ReportDate = primaryKey,
					Hours      = 5,
					StartTime  = primaryKey.AddHours( 8 ),
					EndTime    = primaryKey.AddHours( 13 ),
					Duration   = TimeSpan.FromHours( 5 )
				};
				
				int rowCount = mapping.Insert( db._connection, data, primaryKeyValue: DateTime.Today );
				Assert.AreEqual( 1, rowCount );

				var test = mapping.SelectByPrimaryKey<Report>( db._connection, DateTime.Today );
				Assert.IsNotNull( test );
				Assert.AreEqual( data.ReportDate, test.ReportDate );
			}
		}
		catch( Exception e )
		{
			Console.WriteLine( e );
			throw;
		}
		finally
		{
			File.Delete( databasePath );
		}
	}
}
