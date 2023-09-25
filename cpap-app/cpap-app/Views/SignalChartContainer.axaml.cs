using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using ScottPlot;
using ScottPlot.Avalonia;

namespace cpap_app.Views;

public partial class SignalChartContainer : UserControl
{
	public SignalChartContainer()
	{
		InitializeComponent();
	}

	protected override void OnPointerWheelChanged( PointerWheelEventArgs e )
	{
		base.OnPointerWheelChanged( e );

		e.Handled = true;
		Debug.WriteLine( e );
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		var charts = this.GetLogicalDescendants().OfType<SignalChart>().ToList();
		foreach( var chart in charts )
		{
			chart.Chart.AxesChanged += ChartOnAxesChanged;
		}
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
			if( control.Chart == who )
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

