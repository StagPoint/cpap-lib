using System.Linq;

using cpap_db;

using OAuth;

namespace cpap_app.ViewModels;

public class AuthorizationConfigStore
{
	static AuthorizationConfigStore()
	{
		using var store = StorageService.Connect();

		var mapping = StorageService.CreateMapping<AuthorizationConfig>( "auth_config" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );

		store.CreateTable<AuthorizationConfig>();

		var records = store.SelectAll<AuthorizationConfig>();
		if( records.Count == 0 )
		{
			store.Insert( new AuthorizationConfig(), primaryKeyValue: 0 );
		}
	}

	public static AuthorizationConfig GetConfig()
	{
		using var store = StorageService.Connect();

		return store.SelectAll<AuthorizationConfig>().FirstOrDefault() ?? new AuthorizationConfig();
	}

	public static void SaveConfig( AuthorizationConfig info )
	{
		using var store = StorageService.Connect();
		store.Update( info, primaryKeyValue: 0 );
	}
}
