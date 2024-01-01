using System;
using System.Linq;

using cpap_app.Importers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class PulseOximetryImportOptionsStore
{
	static PulseOximetryImportOptionsStore()
	{
		var userMapping = StorageService.GetMapping<UserProfile>();

		var mapping = StorageService.CreateMapping<PulseOximetryImportOptions>( "spo2_import_options" );
		mapping.ForeignKey = new ForeignKeyColumn( userMapping );

		StorageService.Connect().CreateTable<PulseOximetryImportOptions>();
	}

	public static PulseOximetryImportOptions GetImportOptions( int userProfileID, string deviceType )
	{
		using var store = StorageService.Connect();

		var savedOptions  = store.SelectByForeignKey<PulseOximetryImportOptions>( userProfileID );
		var deviceOptions = savedOptions.FirstOrDefault( x => string.Compare( x.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase ) == 0 );

		if( deviceOptions == null )
		{
			deviceOptions = new PulseOximetryImportOptions
			{
				DeviceType = deviceType,
			};

			deviceOptions.ID = store.Insert( deviceOptions, foreignKeyValue: userProfileID );
		}

		return deviceOptions;
	}

	public static void InsertImportOptions( int userProfileID, PulseOximetryImportOptions importOptions )
	{
		importOptions.ID = StorageService.Connect().Insert( importOptions, foreignKeyValue: userProfileID );
	}

	public static void UpdateImportOptions( int userProfileID, PulseOximetryImportOptions importOptions )
	{
		StorageService.Connect().Update( importOptions );
	}
}
