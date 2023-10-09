using System;

using Avalonia.Interactivity;

namespace cpap_app.Events;

public class DateTimeRoutedEventArgs : RoutedEventArgs
{
	public DateTime DateTime { get; set; }
}

public class DateTimeRangeRoutedEventArgs : RoutedEventArgs
{
	public DateTime StartTime { get; set; }
	public DateTime EndTime   { get; set; }
}

