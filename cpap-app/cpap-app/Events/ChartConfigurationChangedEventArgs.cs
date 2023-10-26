using Avalonia.Interactivity;

using cpap_app.Configuration;

namespace cpap_app.Events;

public class ChartConfigurationChangedEventArgs : RoutedEventArgs
{
	public SignalChartConfiguration ChartConfiguration { get; set; } = null!;
	public string                   PropertyName       { get; set; } = null!;
}
