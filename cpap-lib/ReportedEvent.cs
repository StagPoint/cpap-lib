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
		/// Respiratory Effort Related Arousal (RERA) is a period of increased respiratory effort leading to an arousal.
		/// </summary>
		RERA,
		/// <summary>
		/// A period of Cheyne-Stokes Respiration
		/// </summary>
		CSR,
	}

	public class ReportedEvent
	{
		#region Public properties 
		
		/// <summary>
		/// The descriptive name of the event being flagged 
		/// </summary>
		public EventType Type { get; internal set; }
		
		/// <summary>
		/// The time when the event occurred 
		/// </summary>
		public DateTime StartTime { get; internal set; }

		/// <summary>
		/// The duration of the event being flagged, in seconds
		/// </summary>
		public double Duration { get; internal set; }

		#endregion
		
		#region Internal functions

		internal static ReportedEvent FromEdfAnnotation( DateTime fileStartTime, EdfAnnotation annotation )
		{
			var flag = new ReportedEvent
			{
				StartTime = fileStartTime.AddSeconds( annotation.Onset ),
				Duration  = annotation.Duration ?? 0.0,
				Type      = EventTypeUtil.FromName( annotation.Annotation, false )
			};

			return flag;
		}
		
		#endregion 
		
		#region Base class overrides

		public override string ToString()
		{
			return $"Start: {StartTime:t}  Duration: {Duration:F2}  Description: {Type.ToName()}";
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
