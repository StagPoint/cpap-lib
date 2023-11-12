using System;

using Avalonia.Input;
using Avalonia.Interactivity;

using cpaplib;

namespace cpap_app.Events;

public class ReportedEventTypeArgs : RoutedEventArgs
{
	public EventType Type { get; set; }
}

public class EventSelection
{
	public static readonly RoutedEvent<ReportedEventTypeArgs> EventTypeSelectedEvent =
		RoutedEvent.Register<EventSelection, ReportedEventTypeArgs>( "EventTypeSelected", RoutingStrategies.Bubble | RoutingStrategies.Tunnel ); 

	public static void AddEventTypeSelectedHandler( IInputElement element, EventHandler<ReportedEventTypeArgs> handler )
	{
		element.AddHandler( EventTypeSelectedEvent, handler );
	}
}
