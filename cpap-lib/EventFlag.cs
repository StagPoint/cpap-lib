using System;
using System.Collections.Generic;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public enum EventType : int
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
	}

	public class EventFlag
	{
		#region Public properties 
		
		/// <summary>
		/// The number of seconds since session start when this event occurred 
		/// </summary>
		public double Onset { get; internal set; }

		/// <summary>
		/// The duration of the event being flagged, in seconds
		/// </summary>
		public double Duration { get; internal set; }

		/// <summary>
		/// The descriptive name of the event being flagged 
		/// </summary>
		public EventType Type { get; internal set; }
		
		/// <summary>
		/// A text description of the event 
		/// </summary>
		public string Description { get; internal set; }
		
		#endregion
		
		#region Internal functions and fields

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

		internal static EventFlag FromEdfAnnotation( EdfAnnotation annotation )
		{
			var flag = new EventFlag
			{
				Onset       = annotation.Onset,
				Duration    = annotation.Duration ?? 0.0,
				Description = annotation.Annotation
			};

			if( _textToEventTypeMap.TryGetValue( annotation.Annotation, out EventType type ) )
			{
				flag.Type = type;
			}
			else
			{
				flag.Type = EventType.Unclassified;
			}

			return flag;
		}
		
		#endregion 
		
		#region Base class overrides

		public override string ToString()
		{
			return $"Onset: {new TimeSpan( 0, 0, 0, (int)Onset )}  Duration: {Duration:F2}  Description: {Description}";
		}

		#endregion 
	}
}
