using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Controls;

public partial class EventSummaryView : UserControl
{
	public EventSummaryView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) && change.NewValue != null && change.NewValue is not EventSummaryViewModel )
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
				RoutedEvent = EventSelection.EventTypeSelectedEvent,
				Source      = this,
				Type        = eventType
			};
			
			RaiseEvent( eventArgs );
		}
	}
}

