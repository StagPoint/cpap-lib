using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using cpap_app.Configuration;
using cpap_app.Helpers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class EventMarkerConfigurationStore
{
	private static EventType[] UnusedEventTypes = { EventType.RecordingStarts, EventType.RecordingEnds, EventType.FalsePositive };
	
	public static List<EventMarkerConfiguration> GetEventMarkerConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		Initialize( store );

		var list = store.SelectAll<EventMarkerConfiguration>().OrderBy( x => x.EventType ).ToList();
		
		// Remove configurations for event types that are not used by the application 
		list.RemoveAll( x => UnusedEventTypes.Contains( x.EventType ) || x.EventType >= EventType.FalsePositive );

		foreach( var config in list )
		{
			if( string.IsNullOrEmpty( config.Initials ) )
			{
				config.Initials = config.EventType.ToInitials();
			}
		}

		return list;
	}

	private static void Initialize( StorageService store )
	{
		var mapping = StorageService.CreateMapping<EventMarkerConfiguration>( "event_marker_config" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );
		
		store.CreateTable<EventMarkerConfiguration>();
		
		var records = store.SelectAll<EventMarkerConfiguration>();
		if( records.Count > 0 )
		{
			return;
		}
		
		// The code below is intended to create reasonable defaults for the known signal types 

		var eventTypes = (EventType[])typeof( EventType ).GetEnumValues();
		for( int i = 0; i < eventTypes.Length; i++ )
		{
			var eventType = eventTypes[ i ];

			if( eventType >= EventType.FalsePositive )
			{
				break;
			}

			var config = new EventMarkerConfiguration
			{
				EventType = eventType,
			};
			
			config.ResetToDefaults();

			store.Insert( config, primaryKeyValue: config.EventType );
		}
	}
}
