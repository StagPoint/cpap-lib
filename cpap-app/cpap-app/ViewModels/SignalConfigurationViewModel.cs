using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Helpers;
using cpap_app.Views;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class SignalConfigurationViewModel
{
	public List<SignalChartConfiguration> PinnedCharts { get; set; } = new();
	public List<SignalChartConfiguration> UnPinnedCharts { get; set; } = new();

	public SignalConfigurationViewModel()
	{
		using( var store = StorageService.Connect() )
		{
			// TODO: Move SignalChartConfiguration initialization to application startup
			EnsureConfigTableExists( store );
			
			var configs = store.SelectAll<SignalChartConfiguration>();

			PinnedCharts = configs
			               .Where( x => x.IsPinned )
			               .OrderBy( x => x.DisplayOrder )
			               .ToList();

			UnPinnedCharts = configs
			                 .Where( x => !x.IsPinned )
			                 .OrderBy( x => x.DisplayOrder )
			                 .ToList();

			// foreach( var name in signalNames )
			// {
			// 	if( !configs.Any( x => x.SignalName.Equals( name, StringComparison.Ordinal ) ) )
			// 	{
			// 		var newConfig = new SignalChartConfiguration
			// 		{
			// 			SignalName      = name,
			// 			DisplayOrder    = UnPinnedCharts.Count + 1,
			// 			IsPinned        = false,
			// 		};
			// 		
			// 		UnPinnedCharts.Add( newConfig );
			//
			// 		store.Insert( newConfig );
			// 	}
			// }
		}
	}

	private void EnsureConfigTableExists( StorageService store )
	{
		var mapping = StorageService.CreateMapping<SignalChartConfiguration>( "chart_config" );
		mapping.GetColumnByName( nameof( SignalChartConfiguration.PlotColor ) ).Converter = new ColorBlobConverter();

		store.CreateTable<SignalChartConfiguration>();

		var records = store.SelectAll<SignalChartConfiguration>();
		if( records.Count > 0 )
		{
			return;
		}
		
		// The code below is intended to create reasonable defaults for the known signal types 

		var signalNames = typeof( SignalNames ).GetAllPublicConstantValues<string>();
		for( int i = 0; i < signalNames.Count; i++ )
		{
			var signalName = signalNames[ i ];
			var plotColor  = DataColors.GetDataColor( i );

			var config = new SignalChartConfiguration
			{
				Title               = signalName,
				SignalName          = signalName,
				DisplayOrder        = i,
				IsPinned            = false,
				IsVisible           = (signalName != SignalNames.EPAP && signalName != SignalNames.MaskPressureLow),
				FillBelow           = true,
				PlotColor           = plotColor
			};

			switch( signalName )
			{
				case SignalNames.FlowRate:
					config.BaselineHigh = 0;
					break;
				
				case SignalNames.Pressure:
					config.SecondarySignalName = SignalNames.EPAP;
					break;
				
				case SignalNames.SpO2:
					config.BaselineLow = 90;
					break;
				
				case SignalNames.Pulse:
					config.BaselineHigh = 110;
					config.BaselineLow  = 50;
					break;
				
				case SignalNames.LeakRate:
					config.BaselineHigh = 24;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 60;
					break;
				
				case SignalNames.FlowLimit:
					config.BaselineHigh = 0.35;
					break;
				
				case SignalNames.TidalVolume:
					config.BaselineHigh = 500;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 2500;
					break;
				
				case SignalNames.MinuteVent:
					config.BaselineHigh = 12;
					config.BaselineLow  = 4;
					break;
			}

			store.Insert( config );
		}
	}
}
