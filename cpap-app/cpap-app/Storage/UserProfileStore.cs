using System.Collections.Generic;
using System.Linq;

using cpaplib;
using cpap_db;

namespace cpap_app.ViewModels;

public class UserProfileStore
{
	public static UserProfile GetLastUserProfile()
	{
		using var store = StorageService.Connect();

		return store.SelectAll<UserProfile>().OrderByDescending( x => x.LastLogin ).First();
	}

	public static List<UserProfile> SelectAll()
	{
		using var store = StorageService.Connect();

		return store.SelectAll<UserProfile>();
	}

	public static bool Insert( UserProfile profile )
	{
		return StorageService.Connect().Insert( profile, primaryKeyValue: profile.UserProfileID ) > 0;
	}

	public static bool Update( UserProfile profile )
	{
		return StorageService.Connect().Update( profile, profile.UserProfileID );
	}

	public static bool Delete( UserProfile profile )
	{
		return StorageService.Connect().Delete( profile, profile.UserProfileID );
	}
}
