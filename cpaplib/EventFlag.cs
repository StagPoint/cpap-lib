namespace cpaplib
{
	public enum EventType : int
	{
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
		/// A full or partial arousal
		/// </summary>
		Arousal,
		/// <summary>
		/// Respiratory Effort Related Arousal (RERA) is a period of increased respiratory effort leading to an arousal.
		/// </summary>
		RERA,
		/// <summary>
		/// Low peripheral blood oxygen saturation 
		/// </summary>
		Hypoxemia,
		/// <summary>
		/// Faster than normal pulse rate (above 100)
		/// </summary>
		Tachycardia,
		/// <summary>
		/// Slower than normal pulse rate (below 50)
		/// </summary>
		Bradycardia,
	}

	public class EventFlag
	{
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
	}
}
