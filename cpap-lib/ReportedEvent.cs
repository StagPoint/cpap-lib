using System;
using System.Collections.Generic;

using StagPoint.EDF.Net;
// ReSharper disable MergeIntoLogicalPattern

namespace cpaplib
{
	internal enum EventMarkerPosition
	{
		AfterDuration = 0,
		BeforeDuration,
	}

	public class ReportedEvent : IComparable, IComparable<ReportedEvent>
	{
		#region Public properties 
		
		/// <summary>
		/// The type of event being flagged 
		/// </summary>
		public EventType Type { get; set; }
		
		/// <summary>
		/// The type of device or source that generated this event
		/// </summary>
		public SourceType SourceType { get; set; }

		/// <summary>
		/// Indicates whether the Event's <see cref="Duration"/> refers to the period before or after
		/// the <see cref="StartTime"/>.
		/// For example, for all apnea type events the <see cref="StartTime"/> property refers to the
		/// point where the event ends and the <see cref="Duration"/> property refers to the time period
		/// before the event, whereas with a <see cref="EventType.PeriodicBreathing"/> event the
		/// <see cref="StartTime"/> defines the point at which the event starts and the <see cref="Duration"/>
		/// property defines how long the event continues. 
		/// </summary>
		internal EventMarkerPosition MarkerPosition { get => ReportedEvent.GetOnsetPositionType( Type ); }

		/// <summary>
		/// The time when the event occurred. See <see cref="MarkerPosition"/> for more details about
		/// how to interpret this field, or use the <see cref="GetTimeBounds"/> function to remove
		/// any ambiguity.
		/// </summary>
		/// TODO: This property should be renamed to MarkerTime or something better than the current name
		public DateTime StartTime { get; set; }

		/// <summary>
		/// The duration of the event being flagged, in seconds
		/// </summary>
		public TimeSpan Duration { get; set; }

		#endregion
		
		#region Public functions

		public static bool TimesOverlap( ReportedEvent a, ReportedEvent b )
		{
			var startA = a.StartTime;
			var startB = b.StartTime;
			var endA   = a.StartTime + a.Duration;
			var endB   = b.StartTime + b.Duration;

			// Events like RERA and Hypopnea have an implicit (not recorded) 10 second duration that precedes the recorded event time
			if( a.Type == EventType.RERA || a.Type == EventType.Hypopnea )
			{
				startA = startA.AddSeconds( -10 );
			}

			// Events like RERA and Hypopnea have an implicit (not recorded) 10 second duration that precedes the recorded event time
			if( b.Type == EventType.RERA || b.Type == EventType.Hypopnea )
			{
				startB = startB.AddSeconds( -10 );
			}
			
			return DateHelper.RangesOverlap( startA, endA, startB, endB );
		}

		public TimeRange GetTimeBounds()
		{
			var markerPosition = ReportedEvent.GetOnsetPositionType( Type );

			if( markerPosition == EventMarkerPosition.AfterDuration )
			{
				return new TimeRange()
				{
					StartTime = StartTime - Duration,
					EndTime   = StartTime
				};
			}

			return new TimeRange()
			{
				StartTime = StartTime,
				EndTime   = StartTime + Duration
			};
		}
		
		#endregion 
		
		#region Internal functions

		internal static ReportedEvent FromEdfAnnotation( DateTime fileStartTime, EdfAnnotation annotation )
		{
			var direction = 1;

			EventType eventType = EventTypeUtil.FromName( annotation.Annotation, false );
			
			var flag = new ReportedEvent
			{
				StartTime = fileStartTime.AddSeconds( direction * annotation.Onset ),
				Duration  = TimeSpan.FromSeconds( annotation.Duration ?? 0.0 ),
				Type      = eventType
			};

			return flag;
		}

		private static EventMarkerPosition GetOnsetPositionType( EventType eventType )
		{
			EventMarkerPosition markerPosition = EventMarkerPosition.BeforeDuration;

			switch( eventType )
			{
				case EventType.UnclassifiedApnea:
				case EventType.ObstructiveApnea:
				case EventType.Hypopnea:
				case EventType.ClearAirway:
				case EventType.Arousal:
				case EventType.RERA:
				case EventType.CSR:
				case EventType.FlowReduction:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.VibratorySnore:
				case EventType.FlowLimitation:
				case EventType.PeriodicBreathing:
				case EventType.VariableBreathing:
				case EventType.LargeLeak:
					markerPosition = EventMarkerPosition.BeforeDuration;
					break;
				default:
					markerPosition = EventMarkerPosition.BeforeDuration;
					break;
			}

			return markerPosition;
		}
		
		#endregion 
		
		#region IComparable interface implementation 

		public int CompareTo( ReportedEvent other )
		{
			return StartTime.CompareTo( other.StartTime );
		}
		
		public int CompareTo( object obj )
		{
			if( obj is ReportedEvent other )
			{
				return CompareTo( other );
			}

			return 0;
		}

		#endregion
		
		#region Base class overrides
		
		public override string ToString()
		{
			return $"Start: {StartTime:t}  Duration: {Duration:T}  Description: {Type.ToName()}";
		}

		#endregion 
	}

	public static class EventTypeUtil
	{
		private static Dictionary<string, EventType> _textToEventTypeMap = new Dictionary<string, EventType>()
		{
			// NOTE: The order of keys is important for "Type to name" operations when there is more than one 
			// key that maps to a given EventType, such as RERA and Arousal both mapping to the same value on
			// ResMed devices. 
			{ "Hypopnea", EventType.Hypopnea },
			{ "Recording starts", EventType.RecordingStarts },
			{ "Recording ends", EventType.RecordingEnds },
			{ "Obstructive Apnea", EventType.ObstructiveApnea },
			{ "Clear Airway", EventType.ClearAirway },
			{ "Central Apnea", EventType.ClearAirway },
			{ "RERA", EventType.RERA },
			{ "Arousal", EventType.RERA },
			{ "Unclassified", EventType.UnclassifiedApnea },
		};

		static EventTypeUtil()
		{
			// Ensure that all of the EventType enumeration values are included 
			foreach( var value in Enum.GetValues( typeof( EventType ) ) )
			{
				var key = NiceNames.Format( value.ToString() );
				
				// ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
				if( !_textToEventTypeMap.ContainsKey( key ) )
				{
					_textToEventTypeMap[ key ] = (EventType)value;
				}
			}
		}

		public static EventType FromName( string name, bool throwOnUnknown = true )
		{
			if( string.IsNullOrEmpty( name ) )
			{
				throw new ArgumentNullException( nameof( name ) );
			}
			
			if( _textToEventTypeMap.TryGetValue( name, out EventType value ) )
			{
				return value;
			}

			if( Enum.TryParse( name, true, out EventType result ) )
			{
				return result;
			}

			if( throwOnUnknown )
			{
				throw new Exception( $"{name} is not a valid {nameof( EventType )} value" );
			}
			
			return EventType.UnclassifiedApnea;
		}

		public static string ToName( this EventType type )
		{
			foreach( var pair in _textToEventTypeMap )
			{
				if( pair.Value == type )
				{
					return pair.Key;
				}
			}

			return type.ToString();
		}

	}
}
