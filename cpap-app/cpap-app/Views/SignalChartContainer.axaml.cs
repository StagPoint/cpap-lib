using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;

using cpap_app.Configuration;
using cpap_app.ViewModels;

using cpaplib;

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
				Title           = config.Title,
				SignalName      = config.SignalName,
				PlotColor       = config.PlotColor,
				RedLinePosition = config.RedlinePosition,
				FillBelow       = config.FillBelow
			};

			_charts.Add( chart );
			
			UnPinnedCharts.Children.Add( chart );
		}
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		foreach( var chart in _charts )
		{
			chart.Chart.AxesChanged += ChartOnAxesChanged;
		}
	}

	protected override void OnPointerWheelChanged( PointerWheelEventArgs e )
	{
		base.OnPointerWheelChanged( e );

		e.Handled = true;
		Debug.WriteLine( e );
	}

	protected override void OnUnloaded( RoutedEventArgs e )
	{
		base.OnUnloaded( e );
		
		var charts = this.GetLogicalDescendants().OfType<SignalChart>().ToList();
		foreach( var chart in charts )
		{
			chart.Chart.AxesChanged -= ChartOnAxesChanged;
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
				chart.Render();
			}
			chart.Configuration.AxesChangedEventEnabled = true;
		}
	}
}

