using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using cpap_app.ViewModels;

using ScottPlot;
using ScottPlot.Avalonia;

namespace cpap_app.Views;

public partial class SignalChartContainer : UserControl
{
	private SignalConfigurationViewModel _signalConfigs;
	private List<SignalChart>            _charts = new();
	
	public SignalChartContainer()
	{
		InitializeComponent();

		_signalConfigs = new SignalConfigurationViewModel();

		foreach( var config in _signalConfigs.UnPinnedCharts )
		{
			if( !config.IsVisible )
			{
				continue;
			}

			var chart = new SignalChart()
			{
				Title               = config.Title,
				SignalName          = config.SignalName,
				SecondarySignalName = config.SecondarySignalName,
				PlotColor           = config.PlotColor,
				BaselineHigh        = config.BaselineHigh,
				BaselineLow         = config.BaselineLow,
				FillBelow           = config.FillBelow,
				AxisMinValue        = config.AxisMinValue,
				AxisMaxValue        = config.AxisMaxValue
			};

			_charts.Add( chart );
			
			UnPinnedCharts.Children.Add( chart );
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

	#region SignalChart event handlers

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
		
		var controls = this.GetLogicalDescendants().OfType<SignalChart>().ToList();
		
		foreach( var control in controls )
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
	
	#endregion 
}

