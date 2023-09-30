using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;

namespace cpap_app.Views;

public partial class SignalChartContainer : UserControl
{
	private List<SignalChart> _charts = new();
	
	public SignalChartContainer()
	{
		InitializeComponent();

		List<SignalChartConfiguration> signalConfigs = SignalConfigurationStore.GetSignalConfigurations();

		foreach( var config in signalConfigs )
		{
			if( !config.IsVisible )
			{
				continue;
			}

			var chart = new SignalChart() { Configuration = config };

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
	
	#region Event handlers

	public void ChartDisplayedRangeChanged( object? sender, TimeRangeRoutedEventArgs e )
	{
		foreach( var control in _charts )
		{
			if( control != sender )
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
}

