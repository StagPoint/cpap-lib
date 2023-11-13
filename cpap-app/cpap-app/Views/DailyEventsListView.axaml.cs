using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyEventsListView : UserControl
{
	#region Public properties 
	
	public EventType? SelectedEventType { get; set; }
	
	#endregion 
	
	#region Constructor 
	
	public DailyEventsListView()
	{
		InitializeComponent();
	}
	
	#endregion 
	
	#region Base class overrides 

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
			
			if( DataContext is EventSummaryViewModel model )
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
				DataContext = new EventSummaryViewModel( day );
			}
		}
	}
	
	#endregion 
	
	#region Event handlers 
	
	private void TvwEvents_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( tvwEvents.SelectedItem is not ReportedEvent evt )
		{
			return;
		}

		var timeSelectedEventArgs = new DateTimeRoutedEventArgs
		{
			RoutedEvent = TimeSelection.TimeSelectedEvent,
			Source      = sender,
			DateTime    = evt.StartTime,
		};
			
		RaiseEvent( timeSelectedEventArgs  );

		var eventTypeEventArgs = new ReportedEventTypeArgs
		{
			RoutedEvent = EventSelection.EventTypeSelectedEvent,
			Source      = sender,
			Type        = evt.Type
		};

		RaiseEvent( eventTypeEventArgs );
	}
	
	#endregion 
}

