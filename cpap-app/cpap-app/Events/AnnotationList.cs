using System;

using Avalonia.Input;
using Avalonia.Interactivity;

using cpaplib;

namespace cpap_app.Events;

public enum AnnotationListEventType
{
	Added,
	Removed,
	Changed,
}

public class AnnotationListEventArgs : RoutedEventArgs
{
	public          AnnotationListEventType Change     { get; set; }
	public required Annotation              Annotation { get; set; }
}

public class AnnotationList
{
	public static readonly RoutedEvent<AnnotationListEventArgs> AnnotationListChangedEvent =
		RoutedEvent.Register<AnnotationList, AnnotationListEventArgs>( "AnnotationListChanged", RoutingStrategies.Bubble | RoutingStrategies.Tunnel | RoutingStrategies.Direct );

	public static void AddAnnotationListChangedHandler( IInputElement element, EventHandler<AnnotationListEventArgs> handler )
	{
		element.AddHandler( AnnotationListChangedEvent, handler );
	}
}
