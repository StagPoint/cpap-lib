using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class OxygenEventsView : UserControl
{
	public static readonly StyledProperty<bool> IsFooterVisibleProperty = AvaloniaProperty.Register<DataDistributionView, bool>( nameof( IsFooterVisible ) );

	public bool IsFooterVisible
	{
		get => GetValue( IsFooterVisibleProperty );
		set => SetValue( IsFooterVisibleProperty, value );
	}

	public OxygenEventsView()
	{
		InitializeComponent();
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

