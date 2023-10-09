using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class SignalChartContainer : UserControl
{
	private List<SignalChart> _charts = new();

	// TODO: Disable sync while loading, re-enable after data loading is complete
	private bool _synchronizeActive = false;
	
	#region Constructor 
	
	public SignalChartContainer()
	{
		InitializeComponent();

		List<SignalChartConfiguration> signalConfigs = SignalConfigurationStore.GetSignalConfigurations();
		List<EventMarkerConfiguration> eventConfigs  = EventMarkerConfigurationStore.GetEventMarkerConfigurations();

		foreach( var config in signalConfigs )
		{
			if( !config.IsVisible )
			{
				continue;
			}

			var chart = new SignalChart() { ChartConfiguration = config, MarkerConfiguration = eventConfigs };

			if( !string.IsNullOrEmpty( config.SecondarySignalName ) )
			{
				var secondaryConfig = signalConfigs.FirstOrDefault( x => x.SignalName.Equals( config.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) );
				if( secondaryConfig != null )
				{
					chart.SecondaryConfiguration = secondaryConfig;
				}
			}

			_charts.Add( chart );

			if( config.IsPinned )
			{
				PinnedCharts.Children.Add( chart );
			}
			else
			{
				UnPinnedCharts.Children.Add( chart );
			}
		}
	}
	
	#endregion
	
	#region Base class overrides

	protected override void OnPointerEntered( PointerEventArgs e )
	{
		base.OnPointerEntered( e );
		_synchronizeActive = true;
	}

	protected override void OnGotFocus( GotFocusEventArgs e )
	{
		base.OnGotFocus( e );
		_synchronizeActive = true;
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			_synchronizeActive = false;
		}
	}

	#endregion 
	
	#region Event handlers

	public void ChartDisplayedRangeChanged( object? sender, TimeRangeRoutedEventArgs e )
	{
		if( _synchronizeActive )
		{
			foreach( var control in _charts.Where( control => control != sender ) )
			{
				control.SetDisplayedRange( e.StartTime, e.EndTime );
			}
		}
	}

	private void ChartOnTimeMarkerChanged( object? sender, TimeRoutedEventArgs e )
	{
		foreach( var control in _charts )
		{
			if( control != sender )
			{
				control.UpdateTimeMarker( e.Time );
			}
		}
	}

	internal void SelectTimeRange( DateTime startTime, DateTime endTime )
	{
		if( DataContext is not DailyReport day || _charts.Count == 0 )
		{
			return;
		}

		foreach( var control in _charts )
		{
			control.SetDisplayedRange( startTime, endTime );
		}
	}
	
	#endregion
	
	#region Public functions

	public void ShowEventType( EventType eventType )
	{
		foreach( var chart in _charts )
		{
			if( chart.ChartConfiguration != null && chart.ChartConfiguration.DisplayedEvents.Contains( eventType ) )
			{
				if( !chart.ChartConfiguration.IsPinned )
				{
					chart.BringIntoView();
				}

				chart.Focus();

				return;
			}
		}
	}
	
	#endregion 
}

