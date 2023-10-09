using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class OximetrySessionsList : UserControl
{
	public OximetrySessionsList()
	{
		InitializeComponent();
	}
	
	private void SelectingItemsControl_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( lstSessions.SelectedItem is Session session )
		{
			var eventArgs = new DateTimeRangeRoutedEventArgs
			{
				Route       = RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
				RoutedEvent = DailyReportView.TimeRangeSelectedEvent,
				StartTime   = session.StartTime,
				EndTime     = session.EndTime
			};
			
			RaiseEvent( eventArgs  );
			
			lstSessions.SelectedItem = null;
		}
	}
}

