using System;
using System.Drawing;

namespace cpaplib
{
	public class Annotation : IComparable<Annotation>
	{
		#region Public properties

		/// <summary>
		/// The unique identifier for this <see cref="Annotation"/>
		/// </summary>
		public int AnnotationID { get; set; }

		/// <summary>
		/// Every annotation is associated with a <see cref="Signal"/>
		/// This property contains the name of that <see cref="Signal"/>
		/// </summary>
		public string Signal { get; set; } = string.Empty;

		/// <summary>
		/// The start time of the period that the <see cref="Annotation"/> covers
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// The end time of the period that the <see cref="Annotation"/> covers
		/// </summary>
		public DateTime EndTime { get; set; }
		
		/// <summary>
		/// Indicates whether to show a marker on the <see cref="Signal"/> graph for this <see cref="Annotation"/>
		/// </summary>
		public bool ShowMarker { get; set; }

		/// <summary>
		/// The color of the marker, if shown
		/// </summary>
		public Color? Color { get; set; } = null;

		/// <summary>
		/// A description and any notes supplied by the user for this <see cref="Annotation"/>
		/// </summary>
		public string Notes { get; set; } = string.Empty;
		
		/// <summary>
		/// Returns the span of time between the StartTime and EndTime values
		/// </summary>
		public TimeSpan Duration { get => EndTime - StartTime; }

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			if( Duration.TotalSeconds > 0 )
			{
				return $"{StartTime:g} ({Duration.TrimSeconds():g}) - {Notes}";
			}
			
			return $"{StartTime:g} - {Notes}";
		}

		#endregion
		
		#region IComparable interface implementation 
		
		public int CompareTo( Annotation other )
		{
			return StartTime.CompareTo( other.StartTime );
		}

		#endregion 
	}
}
