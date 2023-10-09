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

public static class SignalConfigurationStore
{
	public static List<SignalChartConfiguration> GetSignalConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		Initialize( store );

		return store.SelectAll<SignalChartConfiguration>().OrderBy( x => x.DisplayOrder ).ToList();
	}

	private static void Initialize( StorageService store )
	{
		var mapping = StorageService.CreateMapping<SignalChartConfiguration>( "chart_config" );

		// Have to add the DisplayedEvents column manually, as CreateMapping only handles value types and strings. 
		var eventsColumn = new ColumnMapping( nameof( SignalChartConfiguration.DisplayedEvents ), nameof( SignalChartConfiguration.DisplayedEvents ), typeof( SignalChartConfiguration ) );
		eventsColumn.Converter = new EnumListBlobConverter<EventType>();
		mapping.Columns.Add( eventsColumn );

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
				PlotColor           = plotColor.ToDrawingColor()
			};

			switch( signalName )
			{
				case SignalNames.FlowRate:
					config.BaselineHigh    = 0;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.Arousal, 
						EventType.Hypopnea, 
						EventType.Unclassified, 
						EventType.ClearAirway, 
						EventType.ObstructiveApnea, 
						EventType.PeriodicBreathing, 
						EventType.CSR, 
						EventType.RERA
					};
					break;
				
				case SignalNames.Pressure:
					config.SecondarySignalName = SignalNames.EPAP;
					break;
				
				case SignalNames.SpO2:
					config.BaselineLow     = 88;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.Desaturation, 
						EventType.Hypoxemia
					};
					break;
				
				case SignalNames.Pulse:
					config.BaselineHigh = 100;
					config.BaselineLow  = 50;
					config.AxisMinValue = 40;
					config.AxisMaxValue = 140;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.Bradycardia, 
						EventType.Tachycardia,
						EventType.PulseRateChange
					};
					break;
				
				case SignalNames.LeakRate:
					config.BaselineHigh = 24;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 60;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.LargeLeak
					};
					break;
				
				case SignalNames.FlowLimit:
					config.BaselineHigh = 0.3;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.FlowLimitation
					};
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
				
				case SignalNames.RespirationRate:
					config.BaselineHigh = 24;
					config.BaselineLow  = 10;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 40;
					break;
			}

			store.Insert( config );
		}
	}
}
