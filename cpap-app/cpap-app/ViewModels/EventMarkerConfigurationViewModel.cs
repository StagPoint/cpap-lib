using System.Collections.Generic;

using cpap_app.Configuration;

using cpap_db;

// ReSharper disable ConvertConstructorToMemberInitializers

namespace cpap_app.ViewModels;

public class EventMarkerConfigurationViewModel
{
	public List<EventMarkerConfiguration> Items { get; set; }

	public EventMarkerConfigurationViewModel()
	{
		Items = EventMarkerConfigurationStore.GetEventMarkerConfigurations();
	}

	public void SaveChanges()
	{
		using var store = StorageService.Connect();

		foreach( var config in Items )
		{
			store.Update( config, primaryKeyValue: config.EventType );
		}
	}
	
	public void ResetAll()
	{
		foreach( var config in Items )
		{
			config.ResetToDefaults();
		}
	}
}
