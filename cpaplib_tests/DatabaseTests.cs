using System.Diagnostics;

using cpap_db;

using cpaplib;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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

	public class FlaggedEvent
	{
		public int      Type      { get; set; }
		public DateTime StartTime { get; set; }
		public TimeSpan Duration  { get; set; }
	}

	public class ValueList
	{
		public double       MinValue { get; set; }
		public double       MaxValue { get; set; }
		public List<double> Values   { get; set; } = new();
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

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
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

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
				Assert.AreEqual( 5, columns.Count );

				var data = new FaultInfo
				{
					Device     = 1,
					Alarm      = 2,
					Humidifier = 3,
					HeatedTube = 4
				};

				int rowID = mapping.Insert( db.Connection, data );
				
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

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
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
					int rowID = mapping.Insert( db.Connection, data );
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

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
				Assert.AreEqual( 6, columns.Count );

				var data = new Report
				{
					ReportDate = DateTime.Today,
					Hours      = 5,
					StartTime  = DateTime.Today.AddHours( 8 ),
					EndTime    = DateTime.Today.AddHours( 13 ),
					Duration   = TimeSpan.FromHours( 5 )
				};
				
				int rowCount = mapping.Insert( db.Connection, data, primaryKeyValue: DateTime.Today );
				
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

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
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
				
				int rowCount = mapping.Insert( db.Connection, data, primaryKeyValue: DateTime.Today );
				Assert.AreEqual( 1, rowCount );

				var test = mapping.SelectByPrimaryKey<Report>( db.Connection, DateTime.Today );
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
	
	[TestMethod]
	public void CanStoreTypesWithForeignKeys()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var reportMapping = new DatabaseMapping<Report>( "report" );
				reportMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

				var eventMapping = new DatabaseMapping<FlaggedEvent>( "event" );
				eventMapping.ForeignKey = new ForeignKeyColumn( "reportID", typeof( DateTime ), "report", "id" );

				Assert.IsTrue( reportMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 6, db.Connection.GetTableInfo( reportMapping.TableName ).Count );

				Assert.IsTrue( eventMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 4, db.Connection.GetTableInfo( eventMapping.TableName ).Count );

				var primaryKey = DateTime.Today.AddDays( -30 );

				var reportData = new Report
				{
					ReportDate = primaryKey,
					Hours      = 5,
					StartTime  = primaryKey.AddHours( 8 ),
					EndTime    = primaryKey.AddHours( 13 ),
					Duration   = TimeSpan.FromHours( 5 )
				};
				
				int rowCount = reportMapping.Insert( db.Connection, reportData, primaryKeyValue: primaryKey );
				Assert.AreEqual( 1, rowCount );

				for( int i = 0; i < 10; i++ )
				{
					var eventData = new FlaggedEvent
					{
						Type      = i,
						StartTime = primaryKey.AddMinutes( i * 10 ),
						Duration  = TimeSpan.FromMinutes( 5 )
					};

					rowCount = eventMapping.Insert( db.Connection, eventData, foreignKeyValue: primaryKey );
					Assert.AreEqual( 1, rowCount );
				}

				rowCount = db.Connection.ExecuteScalar<int>( $"SELECT COUNT(*) FROM {eventMapping.TableName}" );
				Assert.AreEqual( 10, rowCount );
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
	public void ForeignKeysAreEnforced()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var reportMapping = new DatabaseMapping<Report>( "report" );
				reportMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

				var eventMapping = new DatabaseMapping<FlaggedEvent>( "event" );
				eventMapping.ForeignKey = new ForeignKeyColumn( "reportID", typeof( DateTime ), "report", "id" );

				Assert.IsTrue( reportMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 6, db.Connection.GetTableInfo( reportMapping.TableName ).Count );

				Assert.IsTrue( eventMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 4, db.Connection.GetTableInfo( eventMapping.TableName ).Count );

				var eventData = new FlaggedEvent
				{
					Type      = 0,
					StartTime = DateTime.Today,
					Duration  = TimeSpan.FromMinutes( 5 )
				};

				// Attempt to store a record whose foreign key does not match anything in the referenced table
				// This should cause an exception to be thrown 
				eventMapping.Insert( db.Connection, eventData, foreignKeyValue: DateTime.MinValue );

				Assert.Fail( "Expected an exception to be thrown due to a foreign key constraint failing" );
			}
		}
		catch( Exception e )
		{
			Assert.IsTrue( e.Message.Contains( "FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase ) );
		}
		finally
		{
			File.Delete( databasePath );
		}
	}
	
	[TestMethod]
	public void ForeignKeysCascadeDelete()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var reportMapping = new DatabaseMapping<Report>( "report" );
				reportMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );

				var eventMapping = new DatabaseMapping<FlaggedEvent>( "event" );
				eventMapping.ForeignKey = new ForeignKeyColumn( "reportID", typeof( DateTime ), "report", "id" );

				Assert.IsTrue( reportMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 6, db.Connection.GetTableInfo( reportMapping.TableName ).Count );

				Assert.IsTrue( eventMapping.CreateTable( db.Connection ) );
				Assert.AreEqual( 4, db.Connection.GetTableInfo( eventMapping.TableName ).Count );

				var primaryKey = DateTime.Today.AddDays( -30 );

				var reportData = new Report
				{
					ReportDate = primaryKey,
					Hours      = 5,
					StartTime  = primaryKey.AddHours( 8 ),
					EndTime    = primaryKey.AddHours( 13 ),
					Duration   = TimeSpan.FromHours( 5 )
				};
				
				int rowCount = reportMapping.Insert( db.Connection, reportData, primaryKeyValue: primaryKey );
				Assert.AreEqual( 1, rowCount );

				for( int i = 0; i < 10; i++ )
				{
					var eventData = new FlaggedEvent
					{
						Type      = i,
						StartTime = primaryKey.AddMinutes( i * 10 ),
						Duration  = TimeSpan.FromMinutes( 5 )
					};

					rowCount = eventMapping.Insert( db.Connection, eventData, foreignKeyValue: primaryKey );
					Assert.AreEqual( 1, rowCount );
				}

				rowCount = db.Connection.ExecuteScalar<int>( $"SELECT COUNT(*) FROM {eventMapping.TableName}" );
				Assert.AreEqual( 10, rowCount );

				// Delete our Report record. The foreign key constraint should cause all FlaggedEvent records 
				// to be deleted as well. 
				rowCount = db.Execute( $"DELETE FROM {reportMapping.TableName}" );

				// Get a count of how many FlaggedEvent records exist now 
				rowCount = db.Connection.ExecuteScalar<int>( $"SELECT COUNT(*) FROM {eventMapping.TableName}" );
				Assert.AreEqual( 0, rowCount );
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
	public void CanStoreListAsBlob()
	{
		string databasePath = Path.Combine( Path.GetTempPath(), "cpaplib-tests.db" );
		try
		{
			using( var db = new StorageService( databasePath ) )
			{
				var blobColumnMapping = new ColumnMapping( "values", "Values", typeof( ValueList ) );
				blobColumnMapping.Converter = new DoubleListBlobConverter();

				var mapping = new DatabaseMapping<ValueList>( "Values" );
				mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
				mapping.Columns.Add( blobColumnMapping );

				var tableCreated = mapping.CreateTable( db.Connection );
				Assert.IsTrue( tableCreated );

				var columns = db.Connection.GetTableInfo( mapping.TableName );
				Assert.AreEqual( 4, columns.Count );

				var dataObject = new ValueList
				{
					MinValue = short.MinValue,
					MaxValue = short.MaxValue,
					Values   = new List<double>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
				};

				int rowCount = mapping.Insert( db.Connection, dataObject, primaryKeyValue: 1 );
				Assert.AreEqual( 1, rowCount );

				var retrievedObject = mapping.SelectByPrimaryKey<ValueList>( db.Connection, 1 );
				Assert.IsNotNull( retrievedObject );
				Assert.IsNotNull( retrievedObject.Values );
				Assert.AreEqual( dataObject.Values.Count, retrievedObject.Values.Count );

				for( int i = 0; i < dataObject.Values.Count; i++ )
				{
					Assert.AreEqual( dataObject.Values[ i ], retrievedObject.Values[ i ] );
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
}
