using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

using ScottPlot;
using ScottPlot.Avalonia;

namespace cpap_app.Views;

public partial class SignalChartContainer : UserControl
{
	private List<SignalChartConfiguration> _signalConfigs;
	private List<SignalChart>              _charts = new();
	
	public SignalChartContainer()
	{
		InitializeComponent();

		_signalConfigs = SignalConfigurationStore.GetSignalConfigurations();

		foreach( var config in _signalConfigs )
		{
			if( !config.IsVisible )
			{
				continue;
			}

			var chart = new SignalChart() { Configuration = config };

			if( !string.IsNullOrEmpty( config.SecondarySignalName ) )
			{
				var secondaryConfig = _signalConfigs.FirstOrDefault( x => x.SignalName.Equals( config.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) );
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
	
	#region Base class overrides 

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		foreach( var chart in _charts )
		{
			chart.Chart.AxesChanged += ChartOnAxesChanged;
			chart.Chart.PointerMoved += ChartOnPointerMoved;
		}
	}
	
	protected override void OnPointerWheelChanged( PointerWheelEventArgs e )
	{
		base.OnPointerWheelChanged( e );

		e.Handled = true;
	}

	protected override void OnUnloaded( RoutedEventArgs e )
	{
		base.OnUnloaded( e );
		
		var charts = this.GetLogicalDescendants().OfType<SignalChart>().ToList();
		foreach( var chart in charts )
		{
			chart.Chart.AxesChanged  -= ChartOnAxesChanged;
			chart.Chart.PointerMoved -= ChartOnPointerMoved;
		}
	}
	
	#endregion 

	#region Event handlers

	private void ChartOnPointerMoved( object? sender, PointerEventArgs e )
	{
		if( sender is not AvaPlot plot )
		{
			return;
		}
		
		// Returns mouse coordinates as grid coordinates, taking pan and zoom into account
		(double mouseCoordX, double mouseCoordY) = plot.GetMouseCoordinates();

		// Synchronize the update of the vertical indicator in all charts in the group
		foreach( var chart in _charts )
		{
			chart.UpdateSelectedTime( mouseCoordX );
		}
	}

	private void ChartOnAxesChanged( object? sender, EventArgs e )
	{
		var who           = sender as AvaPlot;
		var newAxisLimits = who.Plot.GetAxisLimits();
		
		foreach( var control in _charts )
		{
			if( control.Chart == who || !control.Chart.IsEnabled )
			{
				continue;
			}
			
			var chart = control.Chart;

			// disable events briefly to avoid an infinite loop
			chart.Configuration.AxesChangedEventEnabled = false;
			{
				var currentAxisLimits  = chart.Plot.GetAxisLimits();
				var modifiedAxisLimits = new AxisLimits( newAxisLimits.XMin, newAxisLimits.XMax, currentAxisLimits.YMin, currentAxisLimits.YMax );

				chart.Plot.SetAxisLimits( modifiedAxisLimits );
				chart.RenderRequest( RenderType.LowQualityThenHighQualityDelayed );
			}
			chart.Configuration.AxesChangedEventEnabled = true;
		}
	}

	internal void SelectTimeRange( DateTime startTime, DateTime endTime )
	{
		if( DataContext is not DailyReport day || _charts.Count == 0 )
		{
			return;
		}

		var offsetStart   = (startTime - day.RecordingStartTime).TotalSeconds;
		var offsetEnd     = (endTime - day.RecordingStartTime).TotalSeconds;

		foreach( var control in _charts )
		{
			if( !control.Chart.IsEnabled )
			{
				continue;
			}
			
			var chart = control.Chart;

			// disable events briefly to avoid an infinite loop
			chart.Configuration.AxesChangedEventEnabled = false;
			{
				var currentAxisLimits  = chart.Plot.GetAxisLimits();
				var modifiedAxisLimits = new AxisLimits( offsetStart, offsetEnd, currentAxisLimits.YMin, currentAxisLimits.YMax );

				chart.Plot.SetAxisLimits( modifiedAxisLimits );
				chart.RenderRequest( RenderType.LowQualityThenHighQualityDelayed );
			}
			chart.Configuration.AxesChangedEventEnabled = true;
		}
	}
	
	#endregion 
}

