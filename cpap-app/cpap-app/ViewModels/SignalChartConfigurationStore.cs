using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Configuration;
using cpap_app.Converters;
using cpap_app.Helpers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public static class SignalChartConfigurationStore
{
	public static List<SignalChartConfiguration> GetSignalConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		Initialize( store );

		var configurations = store.SelectAll<SignalChartConfiguration>().ToList();
		configurations.Sort();

		return configurations;
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
				FillBelow           = false,
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
				
				case SignalNames.MaskPressure:
					config.AxisMaxValue = 20;
					break;
				
				case SignalNames.LeakRate:
					config.ShowStepped  = true;
					config.BaselineHigh = 24;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 40;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.LargeLeak
					};
					break;
				
				case SignalNames.FlowLimit:
					config.ShowStepped  = true;
					config.BaselineHigh = 0.3;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.FlowLimitation
					};
					break;
				
				case SignalNames.TidalVolume:
					config.BaselineHigh = 500;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 2000;
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
				
				case SignalNames.SpO2:
					config.BaselineLow     = 88;
					config.DisplayedEvents = EventTypes.OxygenSaturation.ToList();
					break;
				
				case SignalNames.Pulse:
					config.BaselineHigh    = 100;
					config.BaselineLow     = 50;
					config.AxisMinValue    = 40;
					config.AxisMaxValue    = 130;
					config.DisplayedEvents = EventTypes.Pulse.ToList();
					break;
				
				case SignalNames.Movement:
					config.ShowStepped     = true;
					break;
			}

			store.Insert( config );
		}
	}
	
	public static List<SignalChartConfiguration> Update( SignalChartConfiguration config )
	{
		var configurations = GetSignalConfigurations();
		
		// Replace the configuration within the list
		configurations.RemoveAll( x => x.SignalName == config.SignalName );
		configurations.Add( config );
		
		// Re-sort the list so that we can update the DisplayOrder field 
		configurations.Sort();

		using var store = StorageService.Connect();
		try
		{
			store.Connection.BeginTransaction();
			
			int pinnedOrder   = 0;
			int unpinnedOrder = 0;

			foreach( var loop in configurations )
			{
				loop.DisplayOrder = loop.IsVisible ? (loop.IsPinned ? pinnedOrder++ : unpinnedOrder++) : int.MaxValue;
				
				store.Update( loop, loop.ID );
			}
			
			store.Connection.Commit();
		}
		catch( Exception )
		{
			store.Connection.Rollback();
			throw;
		}

		return configurations;
	}
	
	public static List<SignalChartConfiguration> SwapDisplayOrder( SignalChartConfiguration a, SignalChartConfiguration b )
	{
		// Swap the DisplayOrder values
		(a.DisplayOrder, b.DisplayOrder) = (b.DisplayOrder, a.DisplayOrder);
		
		var configurations = GetSignalConfigurations();
		
		// Replace the configurations within the list
		configurations.RemoveAll( x => x.SignalName == a.SignalName || x.SignalName == b.SignalName );
		configurations.Add( a );
		configurations.Add( b );
		
		// Re-sort the list so that we can update the DisplayOrder field 
		configurations.Sort();

		using var store = StorageService.Connect();
		try
		{
			store.Connection.BeginTransaction();
			
			int pinnedOrder   = 0;
			int unpinnedOrder = 0;

			foreach( var loop in configurations )
			{
				loop.DisplayOrder = loop.IsVisible ? (loop.IsPinned ? pinnedOrder++ : unpinnedOrder++) : int.MaxValue;
				
				store.Update( loop, loop.ID );
			}
			
			store.Connection.Commit();
		}
		catch( Exception )
		{
			store.Connection.Rollback();
			throw;
		}

		return configurations;
	}
}
