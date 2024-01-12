using System;
using System.Linq;

using cpap_app.Importers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class ImportOptionsStore
{
	static ImportOptionsStore()
	{
		using var store = StorageService.Connect();
		
		var userMapping = StorageService.GetMapping<UserProfile>();
		
		var oximetryImportConfigMapping = StorageService.CreateMapping<PulseOximetryImportOptions>( "oximetry_import_options" );
		oximetryImportConfigMapping.ForeignKey = new ForeignKeyColumn( userMapping );

		store.CreateTable<PulseOximetryImportOptions>();
	}

	public static CpapImportSettings GetCpapImportSettings( int userProfileID )
	{
		using var store = StorageService.Connect();

		var savedOptions = store.SelectByForeignKey<CpapImportSettings>( userProfileID ).FirstOrDefault();
		if( savedOptions == null )
		{
			savedOptions = new CpapImportSettings();

			store.Insert( savedOptions, foreignKeyValue: userProfileID );
		}

		return savedOptions;
	}

	public static void UpdateCpapImportSettings( int userProfileID, CpapImportSettings importSettings )
	{
		StorageService.Connect().Update( importSettings );
	}

	public static PulseOximetryImportOptions GetPulseOximetryImportOptions( int userProfileID )
	{
		using var store = StorageService.Connect();

		var savedOptions = store.SelectByForeignKey<PulseOximetryImportOptions>( userProfileID ).FirstOrDefault();
		if( savedOptions == null )
		{
			savedOptions = new PulseOximetryImportOptions();

			store.Insert( savedOptions, foreignKeyValue: userProfileID );
		}

		return savedOptions;
	}

	public static void UpdatePulseOximetryImportOptions( int userProfileID, PulseOximetryImportOptions importSettings )
	{
		StorageService.Connect().Update( importSettings );
	}
}
