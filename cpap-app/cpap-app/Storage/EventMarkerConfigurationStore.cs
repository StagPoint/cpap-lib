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
	public static List<EventMarkerConfiguration> GetEventMarkerConfigurations()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move SignalChartConfiguration initialization to application startup
		Initialize( store );

		return store.SelectAll<EventMarkerConfiguration>().OrderBy( x => x.EventType ).ToList();
	}

	public static List<EventType> GetUserEventTypes( int profileID )
	{
		using var store = StorageService.Connect();
		
		var eventMapping = StorageService.GetMapping<ReportedEvent>();
		var dayMapping   = StorageService.GetMapping<DailyReport>();

		var query = $"SELECT DISTINCT Type FROM {eventMapping.TableName} WHERE {eventMapping.ForeignKey.ColumnName} in (SELECT {dayMapping.PrimaryKey.ColumnName} FROM {dayMapping.TableName} WHERE {dayMapping.TableName}.{dayMapping.ForeignKey.ColumnName} = ?)";
		return store.Connection.QueryScalars<EventType>( query, profileID );
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
			var eventType           = eventTypes[ i ];
			var eventTypeLabel      = eventType.ToString();
			var eventMarkerType     = EventMarkerType.Flag;
			var eventMarkerPosition = EventMarkerPosition.AtEnd;
			var eventColor          = DataColors.GetMarkerColor( i ).ToDrawingColor();

			switch( eventType )
			{
				case EventType.ObstructiveApnea:
				case EventType.Hypopnea:
				case EventType.ClearAirway:
				case EventType.RERA:
				case EventType.UnclassifiedApnea:
				case EventType.CSR:
				case EventType.FlowReduction:
					eventMarkerType     = EventMarkerType.Flag;
					eventMarkerPosition = EventMarkerPosition.AtEnd;
					break;
				case EventType.Arousal:
					eventMarkerType     = EventMarkerType.TickBottom;
					eventMarkerPosition = EventMarkerPosition.AtEnd;
					break;
				case EventType.FlowLimitation:
				case EventType.LargeLeak:
				case EventType.PeriodicBreathing:
				case EventType.VariableBreathing:
				case EventType.BreathingNotDetected:
					eventMarkerType     = EventMarkerType.Span;
					eventMarkerPosition = EventMarkerPosition.AtEnd;
					break;
				case EventType.VibratorySnore:
					eventMarkerType     = EventMarkerType.TickTop;
					eventMarkerPosition = EventMarkerPosition.AtBeginning;
					break;
				case EventType.Desaturation:
					eventMarkerType     = EventMarkerType.ArrowBottom;
					eventColor          = Color.OrangeRed;
					eventMarkerPosition = EventMarkerPosition.InCenter;
					break;
				case EventType.PulseRateChange:
					eventMarkerType     = EventMarkerType.TickBottom;
					eventColor          = Color.Red;
					eventMarkerPosition = EventMarkerPosition.AtBeginning;
					break;
				case EventType.Hypoxemia:
				case EventType.Tachycardia:
				case EventType.Bradycardia:
				case EventType.PulseOximetryFault:
					eventMarkerType     = EventMarkerType.Span;
					eventMarkerPosition = EventMarkerPosition.AtBeginning;
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
				Color           = eventColor,
				MarkerPosition  = eventMarkerPosition,
			};

			store.Insert( config, primaryKeyValue: config.EventType );
		}
	}
}
