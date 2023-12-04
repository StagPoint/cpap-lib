using System;
using System.Drawing;

using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_app.ViewModels.Tooltips;

using cpaplib;

using ScottPlot.Plottable;

namespace cpap_app.Controls;

public partial class AhiHistoryGraph : HistoryGraphBase
{
	#region Constructor 
	
	public AhiHistoryGraph()
	{
		InitializeComponent();

		GraphTitle.Text = "AHI";
	}
	
	#endregion
	
	#region Private functions

	protected override void LoadData( HistoryViewModel viewModel )
	{
		_history = viewModel;
		
		NoDataLabel.IsVisible = false;
		Chart.IsEnabled       = true;
		this.IsEnabled        = true;

		Chart.Plot.Clear();

		if( viewModel.Days.Count == 0 )
		{
			return;
		}

		var totalDays = viewModel.TotalDays;

		// NOTE: For some reason, we need to offset the beginning and end to account for centered bar offsets
		Chart.Plot.SetAxisLimitsX( -0.5, totalDays - 0.5 );
		Chart.Plot.XAxis.SetBoundary( -0.5, totalDays - 0.5 );
		
		var days   = viewModel.Days;
		
		var valuesHypopnea     = new double[ totalDays ];
		var valuesObstructive  = new double[ totalDays ];
		var valuesClearAirway  = new double[ totalDays ];
		var valuesUnclassified = new double[ totalDays ];

		var maxValue = 0.0;
			
		foreach( var day in days )
		{
			// There may be gaps in the days, so just calculate the index for each day.
			int index = (int)Math.Floor( (day.ReportDate.Date - viewModel.Start.Date).TotalDays );
			if( index < 0 || index >= totalDays )
			{
				continue;
			}

			var counts = new AhiHistoryViewModel( day );

			valuesUnclassified[ index ] = counts.UnclassifiedIndex;
			valuesClearAirway[ index ]  = valuesUnclassified[ index ] + counts.ClearAirwayIndex;
			valuesObstructive[ index ]  = valuesClearAirway[ index ] + counts.ObstructiveIndex;
			valuesHypopnea[ index ]     = valuesObstructive[ index ] + counts.HypopneaIndex;

			maxValue = Math.Max( maxValue, counts.ApneaHypopneaIndex + 1 );
		}

		var      colorIndex = ColorIndex;
		BarPlot? bars       = null;

		bars                 = Chart.Plot.AddBar( valuesHypopnea, DataColors.GetLightThemeColor( colorIndex + 0 ).ToDrawingColor() );
		bars.BarWidth        = 0.95;
		bars.BorderLineWidth = 1;

		bars                 = Chart.Plot.AddBar( valuesObstructive, DataColors.GetLightThemeColor( colorIndex + 1 ).ToDrawingColor() );
		bars.BarWidth        = 0.95;
		bars.BorderLineWidth = 1;

		bars                 = Chart.Plot.AddBar( valuesClearAirway, DataColors.GetLightThemeColor( colorIndex + 2 ).ToDrawingColor() );
		bars.BarWidth        = 0.95;
		bars.BorderLineWidth = 1;

		bars                 = Chart.Plot.AddBar( valuesUnclassified, DataColors.GetLightThemeColor( colorIndex + 3 ).ToDrawingColor() );
		bars.BarWidth        = 0.95;
		bars.BorderLineWidth = 1;

		const int DIVISIONS = 6;

		var positions = new double[ DIVISIONS ];
		var labels    = new string[ DIVISIONS ];

		for( int i = 0; i < DIVISIONS; i++ )
		{
			positions[ i ] = i * (maxValue / (DIVISIONS - 1));
			labels[ i ]    = $"{positions[ i ]:F2}";
		}

		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		Chart.Plot.SetAxisLimitsY( 0, maxValue );
		Chart.Plot.YAxis.SetBoundary( 0, maxValue );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.35f ), null );
		_selectionSpan.IgnoreAxisAuto = true;
		_selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}
	
	protected override object BuildTooltipDataContext( DailyReport day )
	{
		return new AhiHistoryViewModel( day );
	}

	#endregion 
}

