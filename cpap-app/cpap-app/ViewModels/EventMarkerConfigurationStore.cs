using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using cpap_app.Configuration;
using cpap_app.Helpers;

using cpap_db;

using cpaplib;

using ScottPlot;

namespace cpap_app.ViewModels;

public class EventMarkerConfigurationStore
{
	public static List<EventMarkerConfiguration> GetEventMarkerConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		Initialize( store );

		return store.SelectAll<EventMarkerConfiguration>().OrderBy( x => x.EventType ).ToList();
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
			var eventType       = eventTypes[ i ];
			var eventTypeLabel  = eventType.ToString();
			var eventMarkerType = EventMarkerType.Flag;
			var eventColor      = DataColors.GetMarkerColor( i ).ToDrawingColor();

			switch( eventType )
			{
				case EventType.ObstructiveApnea:
				case EventType.Hypopnea:
				case EventType.ClearAirway:
				case EventType.RERA:
				case EventType.Unclassified:
				case EventType.CSR:
					eventMarkerType = EventMarkerType.Flag;
					break;
				case EventType.Arousal:
					eventMarkerType = EventMarkerType.TickBottom;
					break;
				case EventType.FlowLimitation:
				case EventType.LargeLeak:
				case EventType.PeriodicBreathing:
				case EventType.VariableBreathing:
				case EventType.BreathingNotDetected:
					eventMarkerType = EventMarkerType.Span;
					break;
				case EventType.VibratorySnore:
					eventMarkerType = EventMarkerType.TickTop;
					break;
				case EventType.Desaturation:
					eventMarkerType = EventMarkerType.ArrowBottom;
					eventColor      = Color.Orange;
					break;
				case EventType.PulseRateChange:
					eventMarkerType = EventMarkerType.TickBottom;
					eventColor      = Color.Yellow;
					break;
				case EventType.Hypoxemia:
				case EventType.Tachycardia:
				case EventType.Bradycardia:
					eventMarkerType = EventMarkerType.Span;
					break;
				case EventType.RecordingStarts:
				case EventType.RecordingEnds:
					eventMarkerType = EventMarkerType.None;
					break;
				default:
					throw new Exception( $"{nameof(EventType)} value not handled: {eventType}" );
			}

			var config = new EventMarkerConfiguration
			{
				EventType       = eventType,
				EventMarkerType = eventMarkerType,
				Label           = eventTypeLabel,
				Color           = eventColor
			};

			store.Insert( config, primaryKeyValue: config.EventType );
		}
	}
}
