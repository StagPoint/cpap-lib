using System;

using Avalonia.Input;
using Avalonia.Interactivity;

using cpaplib;

namespace cpap_app.Events;

public class SessionDataEventArgs : RoutedEventArgs
{
	public required DateTime   Date       { get; set; }
	public required SourceType SourceType { get; set; }
}

public class SessionDataEvents
{
	public static readonly RoutedEvent<SessionDataEventArgs> SessionDeletionRequestedEvent =
		RoutedEvent.Register<SessionDataEvents, SessionDataEventArgs>( "SessionDeletionRequested", RoutingStrategies.Bubble | RoutingStrategies.Tunnel ); 

	public static void AddDeletionRequestedHandler( IInputElement element, EventHandler<SignalSelectionArgs> handler )
	{
		element.AddHandler( SessionDeletionRequestedEvent, handler );
	}
}
