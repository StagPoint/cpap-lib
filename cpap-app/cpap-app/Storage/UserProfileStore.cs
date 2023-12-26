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
	
	public static ActiveUserProfile GetActiveUserProfile()
	{
		using var store = StorageService.Connect();

		var profile = new ActiveUserProfile( store.SelectAll<UserProfile>().MaxBy( x => x.LastLogin ) ?? new UserProfile() );
		if( profile.UserProfileID != -1 )
		{
			LoadHistoricalData( store, profile );
		}
		
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
	
	#region Private functions 
	
	private static void LoadHistoricalData( StorageService store, ActiveUserProfile profile )
	{
		var eventMapping = StorageService.GetMapping<ReportedEvent>();
		var dayMapping   = StorageService.GetMapping<DailyReport>();
		var userMapping  = StorageService.GetMapping<UserProfile>();

		var query = @$"
SELECT DISTINCT {eventMapping.TableName}.Type 
FROM {eventMapping.TableName} 
WHERE {eventMapping.ForeignKey.ColumnName} IN (SELECT {dayMapping.PrimaryKey.ColumnName} FROM {dayMapping.TableName} WHERE {dayMapping.TableName}.UserProfileID = ?) 
ORDER BY Type;";

		profile.HistoricalEvents = store.Connection.QueryScalars<EventType>( query, profile.UserProfileID );

		// TODO: Replace hard-coded table and field names with values retrieved from DataMapping
		query = $@"SELECT DISTINCT signal.Name FROM user_profile
INNER JOIN day ON day.UserProfileID = user_profile.UserProfileID
INNER JOIN session ON session.dayID = day.ID
INNER JOIN signal ON signal.sessionID = session.ID
WHERE user_profile.UserProfileID = ?
";
		profile.HistoricalSignals = store.Connection.QueryScalars<string>( query, profile.UserProfileID );
	}

	#endregion 
}
