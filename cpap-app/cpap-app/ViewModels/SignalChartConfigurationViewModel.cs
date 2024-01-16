using System.Collections.Generic;

using cpap_app.Configuration;

using cpap_db;
// ReSharper disable ConvertConstructorToMemberInitializers

namespace cpap_app.ViewModels;

public class SignalChartConfigurationViewModel
{
	public List<SignalChartConfiguration> Items { get; set; }

	public SignalChartConfigurationViewModel()
	{
		Items = SignalChartConfigurationStore.GetSignalConfigurations();
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
