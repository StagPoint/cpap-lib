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
	public static readonly RoutedEvent<ReportedEventTypeArgs> ReportedEventTypeSelectedEvent =
		RoutedEvent.Register<EventSelection, ReportedEventTypeArgs>( "ReportedEventTypeSelected", RoutingStrategies.Bubble | RoutingStrategies.Tunnel ); 

	public static void AddReportedEventTypeSelectedHandler( IInputElement element, EventHandler<ReportedEventTypeArgs> handler )
	{
		element.AddHandler( ReportedEventTypeSelectedEvent, handler );
	}
}
