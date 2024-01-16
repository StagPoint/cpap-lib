using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Helpers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public static class SignalChartConfigurationStore
{
	private static bool _isSignalStoreInitialized = false;
	
	public static List<SignalChartConfiguration> GetSignalConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		if( !_isSignalStoreInitialized )
		{
			_isSignalStoreInitialized = true;
			Initialize( store );
		}

		var configurations = store.SelectAll<SignalChartConfiguration>().ToList();
		configurations.Sort();

		return configurations;
	}

	private static void Initialize( StorageService store )
	{
		var mapping = StorageService.CreateMapping<SignalChartConfiguration>( "chart_config" );

		// Have to add the DisplayedEvents column manually, as CreateMapping only handles value types and strings. 
		mapping.Columns.Add( new ColumnMapping( nameof( SignalChartConfiguration.DisplayedEvents ), nameof( SignalChartConfiguration.DisplayedEvents ), typeof( SignalChartConfiguration ) )
		{
			Converter = new EnumListBlobConverter<EventType>(),
		} );

		// Note that CreateTable() won't do anything if the table already exists ("CREATE TABLE IF NOT EXISTS...")
		store.CreateTable<SignalChartConfiguration>();

		// Retrieve the list of records that are already in the database (if any already exist)
		var records = store.SelectAll<SignalChartConfiguration>();
		
		// The code below is intended to create reasonable defaults for the known signal types 

		var signalNames = typeof( SignalNames ).GetAllPublicConstantValues<string>();
		for( int i = 0; i < signalNames.Count; i++ )
		{
			var signalName = signalNames[ i ];

			// Skip any configurations that already exist in the database.
			if( records.Any( x => x.SignalName == signalName ) )
			{
				continue;
			}

			var config = new SignalChartConfiguration
			{
				SignalName          = signalName,
				DisplayOrder        = records.Count,
			};

			config.ResetToDefaults();

			records.Add( config );
			store.Insert( config );
		}
	}
	
	public static List<SignalChartConfiguration> Update( SignalChartConfiguration config )
	{
		var configurations = GetSignalConfigurations();
		
		// Replace the configuration within the list
		configurations.RemoveAll( x => x.SignalName == config.SignalName );
		configurations.Add( config );
		
		// Re-sort the list so that we can update the DisplayOrder field and fix up any gaps 
		configurations.Sort();

		using var store = StorageService.Connect();
		try
		{
			store.Connection.BeginTransaction();

			int displayOrder = 0;

			foreach( var loop in configurations )
			{
				// Patch up any gaps in the DisplayOrder values
				loop.DisplayOrder = displayOrder++;
				
				store.Update( loop, loop.ID );
			}
			
			store.Connection.Commit();
		}
		catch( Exception )
		{
			store.Connection.Rollback();
			throw;
		}

		return configurations;
	}
	
	public static List<SignalChartConfiguration> SwapDisplayOrder( SignalChartConfiguration a, SignalChartConfiguration b )
	{
		// Swap the DisplayOrder values
		(a.DisplayOrder, b.DisplayOrder) = (b.DisplayOrder, a.DisplayOrder);
		
		var configurations = GetSignalConfigurations();
		
		// Replace the configurations within the list
		configurations.RemoveAll( x => x.SignalName == a.SignalName || x.SignalName == b.SignalName );
		configurations.Add( a );
		configurations.Add( b );
		
		// Re-sort the list so that we can update the DisplayOrder field 
		configurations.Sort();

		using var store = StorageService.Connect();
		try
		{
			store.Connection.BeginTransaction();

			int displayOrder = 0;
			
			foreach( var loop in configurations )
			{
				// Patch up any gaps in the DisplayOrder values
				loop.DisplayOrder = displayOrder++;
				
				store.Update( loop, loop.ID );
			}
			
			store.Connection.Commit();
		}
		catch( Exception )
		{
			store.Connection.Rollback();
			throw;
		}

		return configurations;
	}
}
