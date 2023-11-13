using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;

using cpaplib;

namespace cpap_app.Views;

public partial class OximetrySessionsList : UserControl
{
	public OximetrySessionsList()
	{
		InitializeComponent();
	}
	
	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			// Filter the day's Sessions to only those that were produced by a Pulse Oximeter
			lstSessions.ItemsSource = day.Sessions.Where( x => x.SourceType == SourceType.PulseOximetry );
		}
	}

	private void SelectingItemsControl_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( lstSessions.SelectedItem is Session session )
		{
			var timeRangeEventArgs = new DateTimeRangeRoutedEventArgs
			{
				Route       = RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
				RoutedEvent = TimeSelection.TimeRangeSelectedEvent,
				StartTime   = session.StartTime,
				EndTime     = session.EndTime,
				Source      = this,
			};
			
			RaiseEvent( timeRangeEventArgs  );

			var signalEventArgs = new SignalSelectionArgs
			{
				RoutedEvent = SignalSelection.SignalSelectedEvent,
				Source      = this,
				SignalName  = SignalNames.SpO2,
			};

			RaiseEvent( signalEventArgs );
			
			lstSessions.SelectedItem = null;
		}
	}
	
	private void ViewDetails_OnTapped( object? sender, RoutedEventArgs e )
	{
		//throw new System.NotImplementedException();
	}
	
	private void Delete_OnTapped( object? sender, RoutedEventArgs e )
	{
		//throw new System.NotImplementedException();
	}
}

