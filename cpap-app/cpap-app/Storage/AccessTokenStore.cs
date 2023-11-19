using System.Linq;

using cpap_db;

using OAuth;

namespace cpap_app.ViewModels;

public class AccessTokenStore
{
	static AccessTokenStore()
	{
		using var store = StorageService.Connect();

		var mapping = StorageService.CreateMapping<AccessTokenInfo>( "auth_token" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );

		store.CreateTable<AccessTokenInfo>();

		var records = store.SelectAll<AccessTokenInfo>();
		if( records.Count == 0 )
		{
			store.Insert( new AccessTokenInfo(), primaryKeyValue: 0 );
		}
	}

	public static AccessTokenInfo GetAccessTokenInfo()
	{
		using var store = StorageService.Connect();

		return store.SelectAll<AccessTokenInfo>().FirstOrDefault() ?? new AccessTokenInfo();
	}

	public static void SaveAccessTokenInfo( AccessTokenInfo info )
	{
		using var store = StorageService.Connect();
		store.Update( info, primaryKeyValue: 0 );
	}
}
