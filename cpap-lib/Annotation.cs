﻿using System;

namespace cpaplib
{
	public class Annotation
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
		/// A description and any notes supplied by the user for this <see cref="Annotation"/>
		/// </summary>
		public string Notes { get; set; } = string.Empty;

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			return $"{StartTime:f} - {Notes}";
		}

		#endregion
	}
}