using System;

using Avalonia.Interactivity;

namespace cpap_app.Events;

public class TimeRoutedEventArgs : RoutedEventArgs
{
	public DateTime Time { get; set; }
}

public class TimeRangeRoutedEventArgs : RoutedEventArgs
{
	public DateTime StartTime { get; set; }
	public DateTime EndTime   { get; set; }
}
