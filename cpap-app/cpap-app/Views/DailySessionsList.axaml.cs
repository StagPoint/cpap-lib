using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySessionsList : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> SessionSelectedEvent =
		RoutedEvent.Register<DailyReportView, DateTimeRangeRoutedEventArgs>( nameof( SessionSelected ), RoutingStrategies.Bubble );

	public static void AddSessionSelectedHandler( IInputElement element, EventHandler<DateTimeRangeRoutedEventArgs> handler )
	{
		element.AddHandler( SessionSelectedEvent, handler );
	}

	public event EventHandler<DateTimeRangeRoutedEventArgs> SessionSelected
	{
		add => AddHandler( SessionSelectedEvent, value );
		remove => RemoveHandler( SessionSelectedEvent, value );
	}

	#endregion 
	
	#region Constructor 
	
	public DailySessionsList()
	{
		InitializeComponent();
	}
	
	#endregion
	
	private void SelectingItemsControl_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( sender is ListBox { SelectedItem: Session session } listBox )
		{
			var eventArgs = new DateTimeRangeRoutedEventArgs
			{
				Route       = RoutingStrategies.Bubble,
				RoutedEvent = SessionSelectedEvent,
				StartTime   = session.StartTime,
				EndTime     = session.EndTime
			};
			
			RaiseEvent( eventArgs  );
			
			listBox.SelectedItem = null;
		}
	}
}

