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
		/// The descriptive name of the event being flagged 
		/// </summary>
		public EventType Type { get; set; }
		
		/// <summary>
		/// The type of device or source that generated this event
		/// </summary>
		public SourceType SourceType { get; set; }

		/// <summary>
		/// Gets or sets whether the Event's position occurs logically before or after the Duration 
		/// </summary>
		internal EventMarkerPosition MarkerPosition { get => GetOnsetPositionType( Type ); }

		/// <summary>
		/// The time when the event occurred 
		/// </summary>
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
			var markerPosition = GetOnsetPositionType( Type );

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

		private EventMarkerPosition GetOnsetPositionType( EventType eventType )
		{
			EventMarkerPosition markerPosition = EventMarkerPosition.BeforeDuration;

			switch( eventType )
			{
				case EventType.Unclassified:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.ObstructiveApnea:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.Hypopnea:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.ClearAirway:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.Arousal:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.RERA:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.CSR:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.VibratorySnore:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.FlowLimitation:
					markerPosition = EventMarkerPosition.BeforeDuration;
					break;
				case EventType.FlowReduction:
					markerPosition = EventMarkerPosition.AfterDuration;
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
			// key that maps to a given EventType, such as RERA and Arousal both mapping to the same value. 
			{ "Hypopnea", EventType.Hypopnea },
			{ "Recording starts", EventType.RecordingStarts },
			{ "Recording ends", EventType.RecordingEnds },
			{ "Obstructive Apnea", EventType.ObstructiveApnea },
			{ "Clear Airway", EventType.ClearAirway },
			{ "Central Apnea", EventType.ClearAirway },
			{ "RERA", EventType.RERA },
			{ "Arousal", EventType.RERA },
			{ "Unclassified", EventType.Unclassified },
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
			
			return EventType.Unclassified;
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
