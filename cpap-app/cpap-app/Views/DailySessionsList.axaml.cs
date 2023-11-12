using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

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

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			// Filter the day's Sessions to only those that were produced by the CPAP machine
			lstSessions.ItemsSource = day.Sessions.Where( x => x.SourceType == SourceType.CPAP );
		}
	}

	private void lstSessions_Tapped( object? sender, TappedEventArgs e )
	{
		if( sender is Border { Tag: Session session } )
		{
			var eventArgs = new DateTimeRangeRoutedEventArgs
			{
				Route       = RoutingStrategies.Bubble,
				RoutedEvent = SessionSelectedEvent,
				StartTime   = session.StartTime,
				EndTime     = session.EndTime
			};
			
			RaiseEvent( eventArgs  );
		}
	}
}

