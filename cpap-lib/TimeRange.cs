using System;

namespace cpaplib
{
	public class TimeRange
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime   { get; set; }

		public TimeSpan Duration
		{
			get => EndTime - StartTime;
		}
	}
}
