﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class DailyEventsListView : UserControl
{
	#region Public properties 
	
	public EventType? SelectedEventType { get; set; }
	
	#endregion 
	
	#region Private fields

	private List<EventMarkerConfiguration> _eventConfigs = new();
	
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

		_eventConfigs = EventMarkerConfigurationStore.GetEventMarkerConfigurations();

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

		var eventBounds = evt.GetTimeBounds();
		var eventTime   = eventBounds.StartTime.AddSeconds( eventBounds.Duration.TotalSeconds * 0.5 );

		var timeSelectedEventArgs = new DateTimeRoutedEventArgs
		{
			RoutedEvent = TimeSelection.TimeSelectedEvent,
			Source      = sender,
			DateTime    = eventTime,
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
	
	private async void MarkAsFalse_OnClick( object? sender, RoutedEventArgs e )
	{
		if( sender is not MenuItem { Tag: ReportedEvent evt } )
		{
			return;
		}

		if( DataContext is not EventSummaryViewModel viewModel )
		{
			throw new InvalidOperationException();
		}

		if( viewModel.Day is not DailyReportViewModel day )
		{
			throw new InvalidOperationException();
		}

		var typeName = _eventConfigs.FirstOrDefault( x => x.EventType == evt.Type )?.Label ?? evt.Type.ToName();
		
		var msg = $"""
		           Are you sure you wish to mark this {typeName} event as a False Positive?
		           This will have the same effect as deleting the event, and will cause all event
		           information for this day to be recalculated.

		           This action cannot be undone. Proceed with extreme caution
		           """;

		var owner     = this.FindAncestorOfType<Window>();
		var confirmed = await InputDialog.GetConfirmation( owner!, Icon.Warning, "Mark as False Positive?", msg );

		if( !confirmed )
		{
			return;
		}

		// Make sure a notation is made about this change
		day.AppendNote( $"Marked {typeName} event at {evt.StartTime:g} as a False Positive." );

		// When an event is marked as a "false positive" instead of being deleted, we add the value of
		// FalsePositive to the existing type in order to make the process reversible. All processes 
		// that deal with events will need to ignore any event whose Type is EventType.FalsePositive or higher. 
		evt.Type = EventType.FalsePositive + (int)evt.Type;
		
		day.UpdateEventSummary();

		var userProfile = UserProfileStore.GetActiveUserProfile();
		StorageService.Connect().SaveDailyReport( userProfile.UserProfileID, day );

		day.Reload();
	}

	#endregion
}

