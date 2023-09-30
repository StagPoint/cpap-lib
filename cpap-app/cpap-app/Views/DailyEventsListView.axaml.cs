using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyEventsListView : UserControl
{
	public DailyEventsListView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name.Equals( nameof( DataContext ), StringComparison.Ordinal ) )
		{
			if( change.NewValue is DailyReport day )
			{
				DataContext = new DailyEventsViewModel( day );
			}
		}
	}

	private void TvwEvents_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( tvwEvents.SelectedItem is not ReportedEvent evt )
		{
			return;
		}
		
		var eventArgs = new TimeRoutedEventArgs
		{
			Route       = RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			RoutedEvent = DailyReportView.TimeSelectedEvent,
			Time        = evt.StartTime
		};
			
		RaiseEvent( eventArgs  );
	}
}

