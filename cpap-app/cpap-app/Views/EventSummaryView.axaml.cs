using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class EventSummaryView : UserControl
{
	public static readonly StyledProperty<bool> IsFooterVisibleProperty = AvaloniaProperty.Register<DataDistributionView, bool>( nameof( IsFooterVisible ) );

	public bool IsFooterVisible
	{
		get => GetValue( IsFooterVisibleProperty );
		set => SetValue( IsFooterVisibleProperty, value );
	}

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
				Route       = RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
				RoutedEvent = DailyReportView.ReportedEventTypeSelectedEvent,
				Source      = this,
				Type        = eventType
			};
			
			RaiseEvent( eventArgs );
		}
	}
}

