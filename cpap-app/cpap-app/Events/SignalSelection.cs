using System;

using Avalonia.Input;
using Avalonia.Interactivity;

namespace cpap_app.Events;

public class SignalSelectionArgs : RoutedEventArgs
{
	public required string SignalName { get; set; }
}

public class SignalSelection
{
	public static readonly RoutedEvent<SignalSelectionArgs> SignalSelectedEvent =
		RoutedEvent.Register<EventSelection, SignalSelectionArgs>( "SignalSelected", RoutingStrategies.Bubble | RoutingStrategies.Tunnel ); 

	public static void AddSignalSelectedHandler( IInputElement element, EventHandler<SignalSelectionArgs> handler )
	{
		element.AddHandler( SignalSelectedEvent, handler );
	}
}
