using Avalonia.Interactivity;

namespace cpap_app.Events;

public class MinMaxEventArgs : RoutedEventArgs
{
	public double Min { get; set; }
	public double Max { get; set; }

	public MinMaxEventArgs( double min, double max, RoutedEvent? routedEvent, object? source ) 
		: base( routedEvent, source )
	{
		Min = min;
		Max = max;
	}
}
