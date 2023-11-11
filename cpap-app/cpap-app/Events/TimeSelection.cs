using System;

using Avalonia.Input;
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

public class TimeSelection
{
	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> TimeRangeSelectedEvent =
		RoutedEvent.Register<TimeSelection, DateTimeRangeRoutedEventArgs>( "TimeRangeSelected", RoutingStrategies.Bubble | RoutingStrategies.Tunnel );

	public static readonly RoutedEvent<DateTimeRoutedEventArgs> TimeSelectedEvent =
		RoutedEvent.Register<TimeSelection, DateTimeRoutedEventArgs>( "TimeSelected", RoutingStrategies.Bubble | RoutingStrategies.Tunnel );

	public static void AddTimeRangeSelectedHandler( IInputElement element, EventHandler<DateTimeRangeRoutedEventArgs> handler )
	{
		element.AddHandler( TimeRangeSelectedEvent, handler );
	}

	public static void AddTimeSelectedHandler( IInputElement element, EventHandler<DateTimeRoutedEventArgs> handler )
	{
		element.AddHandler( TimeSelectedEvent, handler );
	}
}
