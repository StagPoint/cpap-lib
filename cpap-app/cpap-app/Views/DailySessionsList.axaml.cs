using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySessionsList : UserControl
{
	#region Constructor 
	
	public DailySessionsList()
	{
		InitializeComponent();
	}
	
	#endregion
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

