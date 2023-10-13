using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class OximetrySessionsList : UserControl
{
	public OximetrySessionsList()
	{
		InitializeComponent();
	}
	
	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			// Filter the day's Sessions to only those that were produced by the CPAP machine
			lstSessions.ItemsSource = day.Sessions.Where( x => x.SourceType == SourceType.CPAP );
		}
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

