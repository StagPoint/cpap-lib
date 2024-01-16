using System.Collections.Generic;

using cpap_app.Configuration;

using cpap_db;

using cpaplib;

// ReSharper disable ConvertConstructorToMemberInitializers

namespace cpap_app.ViewModels;

public class SignalChartConfigurationViewModel
{
	public List<SignalChartConfiguration> Items { get; set; }

	public SignalChartConfigurationViewModel()
	{
		var items = SignalChartConfigurationStore.GetSignalConfigurations();

		// Remove any Signals that the user has never encountered (this keeps the list clear of things like TargetVent when the 
		// user is not using a Bilevel or ASV machine, or SpO2 when the user has never imported pulse oximetry data, for example)
		List<string> encounteredSignals = StorageService.Connect().GetStoredSignalNames( UserProfileStore.GetActiveUserProfile().UserProfileID );
		items.RemoveAll( x => !encounteredSignals.Contains( x.SignalName ) );

		Items = items;
	}

	public void SaveChanges()
	{
		foreach( var config in Items )
		{
			SignalChartConfigurationStore.Update( config );
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
