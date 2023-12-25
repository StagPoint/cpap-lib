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
	private static bool _isSignalStoreInitialized = false;
	
	public static List<SignalChartConfiguration> GetSignalConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		if( !_isSignalStoreInitialized )
		{
			_isSignalStoreInitialized = true;
			Initialize( store );
		}

		var configurations = store.SelectAll<SignalChartConfiguration>().ToList();
		configurations.Sort();

		return configurations;
	}

	private static void Initialize( StorageService store )
	{
		var mapping = StorageService.CreateMapping<SignalChartConfiguration>( "chart_config" );

		// Have to add the DisplayedEvents column manually, as CreateMapping only handles value types and strings. 
		mapping.Columns.Add( new ColumnMapping( nameof( SignalChartConfiguration.DisplayedEvents ), nameof( SignalChartConfiguration.DisplayedEvents ), typeof( SignalChartConfiguration ) )
		{
			Converter = new EnumListBlobConverter<EventType>(),
		} );

		// Note that CreateTable() won't do anything if the table already exists ("CREATE TABLE IF NOT EXISTS...")
		store.CreateTable<SignalChartConfiguration>();

		// Retrieve the list of records that are already in the database (if any already exist)
		var records = store.SelectAll<SignalChartConfiguration>();
		
		// The code below is intended to create reasonable defaults for the known signal types 

		var signalNames = typeof( SignalNames ).GetAllPublicConstantValues<string>();
		for( int i = 0; i < signalNames.Count; i++ )
		{
			var signalName = signalNames[ i ];
			var plotColor  = DataColors.GetDataColor( i );

			// Skip any configurations that already exist in the database.
			if( records.Any( x => x.SignalName == signalName ) )
			{
				continue;
			}

			var config = new SignalChartConfiguration
			{
				Title               = signalName,
				SignalName          = signalName,
				DisplayOrder        = records.Count,
				IsPinned            = false,
				IsVisible           = (signalName != SignalNames.EPAP && signalName != SignalNames.MaskPressureLow),
				FillBelow           = false,
				PlotColor           = plotColor.ToDrawingColor()
			};

			switch( signalName )
			{
				case SignalNames.MaskPressureLow:
					// Do not create a configuration for this Signal
					continue;
				
				case SignalNames.FlowRate:
					config.BaselineHigh = 0;
					config.ShowInTrends = false;
					
					config.DisplayedEvents = new List<EventType>( EventTypes.Apneas );
					config.DisplayedEvents.Add( EventType.RERA );
					break;
				
				case SignalNames.Pressure:
					config.SecondarySignalName = SignalNames.EPAP;
					config.AxisMinValue        = 5;
					config.AxisMaxValue        = 20;
					config.ScalingMode         = AxisScalingMode.Override;
					break;
				
				case SignalNames.MaskPressure:
					config.AxisMinValue = 0;
					config.AxisMaxValue = 20;
					config.ScalingMode  = AxisScalingMode.Override;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.LeakRate:
					config.SecondarySignalName = SignalNames.TotalLeak;
					config.ShowStepped         = true;
					config.BaselineHigh        = 24;
					config.BaselineLow         = 8;
					config.AxisMinValue        = 0;
					config.AxisMaxValue        = 40;
					config.ScalingMode         = AxisScalingMode.Override;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.LargeLeak
					};
					break;
				
				case SignalNames.TotalLeak:
					config.IsVisible    = false;
					config.ShowStepped  = true;
					config.BaselineHigh = 24;
					config.BaselineLow  = 8;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 40;
					config.ScalingMode  = AxisScalingMode.Override;
					break;
				
				case SignalNames.FlowLimit:
					config.ShowStepped  = true;
					config.BaselineLow  = 0.25;
					config.BaselineHigh = 0.5;
					config.DisplayedEvents = new List<EventType>()
					{
						EventType.FlowLimitation
					};
					break;
				
				case SignalNames.TidalVolume:
					config.BaselineHigh = 500;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 2000;
					config.ScalingMode  = AxisScalingMode.Override;
					break;
				
				case SignalNames.MinuteVent:
					config.SecondarySignalName = SignalNames.TargetVent;
					config.BaselineHigh        = 12;
					config.BaselineLow         = 4;
					break;
				
				case SignalNames.RespirationRate:
					config.Title        = "Resp. Rate";
					config.BaselineHigh = 24;
					config.BaselineLow  = 10;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 40;
					config.ScalingMode  = AxisScalingMode.Override;
					break;
				
				case SignalNames.SpO2:
					config.DisplayedEvents = EventTypes.OxygenSaturation.ToList();
					config.BaselineLow     = 88;
					config.AxisMinValue    = 80;
					config.AxisMaxValue    = 100;
					config.ScalingMode     = AxisScalingMode.Override;
					break;
				
				case SignalNames.Pulse:
					config.DisplayedEvents = EventTypes.Pulse.ToList();
					config.BaselineHigh    = 100;
					config.BaselineLow     = 50;
					config.AxisMinValue    = 40;
					config.AxisMaxValue    = 120;
					config.ScalingMode     = AxisScalingMode.Override;
					break;
				
				case SignalNames.Snore:
				case SignalNames.Movement:
					config.IsVisible    = false;
					config.ShowStepped  = true;
					config.ScalingMode  = AxisScalingMode.AutoFit;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.AHI:
					config.Title           = "AHI";
					config.ShowStepped     = true;
					config.ScalingMode     = AxisScalingMode.AutoFit;
					config.ShowInTrends    = false;
					config.IsVisible       = false;
					config.DisplayedEvents = new List<EventType>( EventTypes.Apneas );
					break;
				
				case SignalNames.InspirationTime:
					config.Title        = "Insp. Time";
					config.ScalingMode  = AxisScalingMode.Override;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 12;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.ExpirationTime:
					config.Title        = "Exp. Time";
					config.ScalingMode  = AxisScalingMode.Override;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 10;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.InspToExpRatio:
					config.Title        = "I:E Ratio";
					config.IsVisible    = false;
					config.AxisMinValue = 0;
					config.AxisMaxValue = 4;
					config.ScalingMode  = AxisScalingMode.Override;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.SleepStages:
					config.Title        = "Sleep Stage";
					config.AxisMaxValue = 0;
					config.AxisMaxValue = 5;
					config.InvertAxisY  = true;
					config.IsVisible    = false;
					config.ShowStepped  = true;
					config.ShowInTrends = false;
					break;
				
				case SignalNames.TargetVent:
					config.ShowInTrends = false;
					config.IsVisible    = false;
					break;
			}

			records.Add( config );
			store.Insert( config );
		}
	}
	
	public static List<SignalChartConfiguration> Update( SignalChartConfiguration config )
	{
		var configurations = GetSignalConfigurations();
		
		// Replace the configuration within the list
		configurations.RemoveAll( x => x.SignalName == config.SignalName );
		configurations.Add( config );
		
		// Re-sort the list so that we can update the DisplayOrder field and fix up any gaps 
		configurations.Sort();

		using var store = StorageService.Connect();
		try
		{
			store.Connection.BeginTransaction();

			int displayOrder = 0;

			foreach( var loop in configurations )
			{
				// Patch up any gaps in the DisplayOrder values
				loop.DisplayOrder = displayOrder++;
				
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

			int displayOrder = 0;
			
			foreach( var loop in configurations )
			{
				// Patch up any gaps in the DisplayOrder values
				loop.DisplayOrder = displayOrder++;
				
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
