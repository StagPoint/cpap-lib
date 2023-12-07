using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Events;

using cpaplib;
using cpap_db;

namespace cpap_app.ViewModels;

public class UserProfileStore
{
	public static event EventHandler<UserProfile>? UserProfileActivated;
	
	public static UserProfile GetActiveUserProfile()
	{
		using var store = StorageService.Connect();

		var profile = store.SelectAll<UserProfile>().MaxBy( x => x.LastLogin ) ?? new UserProfile();
		UserProfileActivated?.Invoke( null, profile );

		return profile;
	}

	public static void SetActive( UserProfile profile )
	{
		profile.LastLogin = DateTime.Now;
		Update( profile );

		UserProfileActivated?.Invoke( null, profile );
	}

	public static List<UserProfile> SelectAll()
	{
		using var store = StorageService.Connect();

		var list = store.SelectAll<UserProfile>();
		list.Sort();

		return list;
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
