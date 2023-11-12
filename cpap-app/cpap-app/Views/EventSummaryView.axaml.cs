using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class EventSummaryView : UserControl
{
	public EventSummaryView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) && change.NewValue != null && change.NewValue is not DailyEventsViewModel )
		{
			DataContext = null;
		}
	}

	private void EventType_Tapped( object? sender, TappedEventArgs e )
	{
		if( sender is Control { Tag: EventType eventType } )
		{
			var eventArgs = new ReportedEventTypeArgs
			{
				Route       = RoutingStrategies.Bubble,
				RoutedEvent = EventSelection.EventTypeSelectedEvent,
				Source      = this,
				Type        = eventType
			};
			
			RaiseEvent( eventArgs );
		}
	}
}

