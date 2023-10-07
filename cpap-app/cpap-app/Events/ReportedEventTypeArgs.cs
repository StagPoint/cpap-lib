using Avalonia.Interactivity;

using cpaplib;

namespace cpap_app.Events;

public class ReportedEventTypeArgs : RoutedEventArgs
{
	public EventType Type { get; set; }
}
