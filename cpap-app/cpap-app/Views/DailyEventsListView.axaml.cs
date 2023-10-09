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
using Avalonia.VisualTree;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyEventsListView : UserControl
{
	public EventType? SelectedEventType { get; set; }
	
	public DailyEventsListView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		if( SelectedEventType != null )
		{
			// Expand the first parent node that corresponds to the selected event type
			var node = tvwEvents.Items.FirstOrDefault( x => x is EventTypeSummary evt && evt.Type == SelectedEventType );
			if( node != null )
			{
				if( tvwEvents.ContainerFromItem( node ) is TreeViewItem item )
				{
					tvwEvents.ExpandSubTree( item );
				}
			}
			
			if( DataContext is DailyEventsViewModel model )
			{
				// Select the first leaf node that corresponds to the event type
				var eventNode = model.Day.Events.FirstOrDefault( x => x.Type == SelectedEventType );
				if( eventNode != null )
				{
					tvwEvents.SelectedItem = eventNode;
				}
			}
		}
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
		
		var eventArgs = new DateTimeRoutedEventArgs
		{
			Route       = RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			RoutedEvent = DailyReportView.TimeSelectedEvent,
			DateTime        = evt.StartTime
		};
			
		RaiseEvent( eventArgs  );
	}
}

