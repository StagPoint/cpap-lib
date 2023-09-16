using System;
using System.Collections.Generic;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public enum EventType
	{
		/// <summary>
		/// Indicates the time when recording starts 
		/// </summary>
		RecordingStarts,
		/// <summary>
		/// Indicates the time when recording ends
		/// </summary>
		RecordingEnds,
		/// <summary>
		/// An apnea event whose classification could not be determined (neither obstructive nor central)
		/// </summary>
		Unclassified,
		/// <summary>
		/// An apnea caused by an airway obstruction 
		/// </summary>
		ObstructiveApnea,
		/// <summary>
		/// An apnea caused by a partially obstructed airway
		/// </summary>
		Hypopnea,
		/// <summary>
		/// An apnea where the airway is unobstructed. Also often called a "Central Apnea", it indicates a
		/// lack of breathing despite the lack of a detectable obstruction. 
		/// </summary>
		ClearAirway,
		/// <summary>
		/// A full or partial arousal not related to respiratory effort
		/// </summary>
		Arousal,
		/// <summary>
		/// Respiratory Effort Related Arousal (RERA) is a period of increased respiratory effort leading to an arousal
		/// </summary>
		RERA,
		/// <summary>
		/// A period of Cheyne-Stokes Respiration
		/// </summary>
		CSR,
		/// <summary>
		/// A reduction in airflow of less than 50% and lasting for at least 1 second
		/// </summary>
		FlowLimitation,
		/// <summary>
		/// A period of low blood oxygen saturation (less than 90%) lasting for at least 1 second
		/// </summary>
		Hypoxemia,
		/// <summary>
		/// A drop in blood oxygen saturation of at least 4% below baseline, lasting at least 10 seconds
		/// </summary>
		Desaturation,
		/// <summary>
		/// A period of pulse rate below 50 beats/minute
		/// </summary>
		Bradycardia,
		/// <summary>
		/// A period of pulse rate above 100 beat/minute
		/// </summary>
		Tachycardia,
		/// <summary>
		/// A period of pressure leakage above 20 L/min lasting at least one second 
		/// </summary>
		LargeLeak,
		/// <summary>
		/// A period of three or more episodes of Central Apnea lasting more than three seconds,
		/// separated by no more than 20 seconds of normal breathing. 
		/// </summary>
		PeriodicBreathing,
		/// <summary>
		/// The vibratory snore index measures the strength of the snoring vibrations
		/// </summary>
		VibratorySnore,
	}

	internal enum EventMarkerPosition
	{
		AfterDuration = 0,
		BeforeDuration,
	}

	public class ReportedEvent
	{
		#region Public properties 
		
		/// <summary>
		/// The descriptive name of the event being flagged 
		/// </summary>
		public EventType Type { get; internal set; }

		/// <summary>
		/// Gets or sets whether the Event's position occurs logically before or after the Duration 
		/// </summary>
		internal EventMarkerPosition MarkerPosition { get => GetOnsetPositionType( Type ); }

		/// <summary>
		/// The time when the event occurred 
		/// </summary>
		public DateTime StartTime { get; internal set; }

		/// <summary>
		/// The duration of the event being flagged, in seconds
		/// </summary>
		public TimeSpan Duration { get; internal set; }

		#endregion
		
		#region Public functions

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
				case EventType.FlowLimitation:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				case EventType.VibratorySnore:
					markerPosition = EventMarkerPosition.AfterDuration;
					break;
				default:
					markerPosition = EventMarkerPosition.BeforeDuration;
					break;
			}

			return markerPosition;
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
			{ "Hypopnea", EventType.Hypopnea },
			{ "Recording starts", EventType.RecordingStarts },
			{ "Recording ends", EventType.RecordingEnds },
			{ "Obstructive Apnea", EventType.ObstructiveApnea },
			{ "Clear Airway", EventType.ClearAirway },
			{ "Central Apnea", EventType.ClearAirway },
			{ "Arousal", EventType.RERA },
			{ "RERA", EventType.RERA },
			{ "Unclassified", EventType.Unclassified },
		};

		static EventTypeUtil()
		{
			// Ensure that all of the EventType enumeration values are included 
			foreach( var value in Enum.GetValues( typeof( EventType ) ) )
			{
				var key = NiceNames.Format( value.ToString() );
				_textToEventTypeMap[ key ] = (EventType)value;
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
