using System.Collections.Generic;

using cpap_app.Configuration;

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
		// TODO: Save changes to event marker configurations 
	}
}
