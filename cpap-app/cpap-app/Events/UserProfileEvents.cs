using System;

using Avalonia.Input;
using Avalonia.Interactivity;

using cpaplib;

namespace cpap_app.Events;

public class UserProfileEventArgs : RoutedEventArgs
{
	public UserProfile   Profile { get; set; }
	public UserProfileAction Action  { get; set; }
}

public enum UserProfileAction
{
	Added,
	Deleted,
	Modified,
	Activated,
}

public class UserProfileEvents
{
	public static readonly RoutedEvent<UserProfileEventArgs> UserProfileChangedEvent =
		RoutedEvent.Register<UserProfileEvents, UserProfileEventArgs>( "UserProfileChanged", RoutingStrategies.Bubble | RoutingStrategies.Tunnel ); 

	public static void AddUserProfileChangedHandler( IInputElement element, EventHandler<SignalSelectionArgs> handler )
	{
		element.AddHandler( UserProfileChangedEvent, handler );
	}
}
