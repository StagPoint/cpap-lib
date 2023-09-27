using System;

using Avalonia.Interactivity;

namespace cpap_app.Events;

public class TimeSelectedEventArgs : RoutedEventArgs
{
	public DateTime Time { get; set; }
}

public class TimeRangeSelectedEventArgs : RoutedEventArgs
{
	public DateTime StartTime { get; set; }
	public DateTime EndTime   { get; set; }
}
