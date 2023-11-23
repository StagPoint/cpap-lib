using System;

using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Controls;

namespace cpap_app.Events;

public class GraphEvents
{
	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> DisplayedRangeChangedEvent = 
		RoutedEvent.Register<GraphEvents, DateTimeRangeRoutedEventArgs>( "DisplayedRangeChanged", RoutingStrategies.Bubble );

	public static void AddDisplayedRangeChangedHandler( IInputElement element, EventHandler<DateTimeRangeRoutedEventArgs> handler )
	{
		element.AddHandler( DisplayedRangeChangedEvent, handler );
	}
	
	public static readonly RoutedEvent<DateTimeRoutedEventArgs> TimeMarkerChangedEvent = 
		RoutedEvent.Register<GraphEvents, DateTimeRoutedEventArgs>( "TimeMarkerChanged", RoutingStrategies.Bubble );

	public static void AddTimeMarkerChangedHandler( IInputElement element, EventHandler<DateTimeRoutedEventArgs> handler )
	{
		element.AddHandler( TimeMarkerChangedEvent, handler );
	}
}
