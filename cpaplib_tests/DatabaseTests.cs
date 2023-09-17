using System.Diagnostics;

using cpap_db;

using cpaplib;

namespace cpaplib_tests;

[TestClass]
public class DatabaseTests
{
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

				int rowsAffected = mapping.Insert( db._connection, data );
				
				Assert.AreEqual( 1, rowsAffected );
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
