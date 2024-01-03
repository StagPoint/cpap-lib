using System;
using System.Linq;

using cpap_app.Importers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class OximetryEventGeneratorConfigStore
{
	static OximetryEventGeneratorConfigStore()
	{
		var userMapping = StorageService.GetMapping<UserProfile>();

		var mapping = StorageService.CreateMapping<OximetryEventGeneratorConfig>( "oximetry_import_options" );
		mapping.ForeignKey = new ForeignKeyColumn( userMapping );

		StorageService.Connect().CreateTable<OximetryEventGeneratorConfig>();
	}

	public static OximetryEventGeneratorConfig GetImportOptions( int userProfileID )
	{
		using var store = StorageService.Connect();

		var savedOptions = store.SelectByForeignKey<OximetryEventGeneratorConfig>( userProfileID ).FirstOrDefault();
		if( savedOptions == null )
		{
			savedOptions = new OximetryEventGeneratorConfig();

			store.Insert( savedOptions, foreignKeyValue: userProfileID );
		}

		return savedOptions;
	}

	public static void UpdateImportOptions( int userProfileID, OximetryEventGeneratorConfig importOptions )
	{
		StorageService.Connect().Update( importOptions );
	}
}
