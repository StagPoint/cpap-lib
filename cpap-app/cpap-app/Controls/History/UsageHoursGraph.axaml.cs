using System;
using System.Diagnostics;
using System.Linq;

using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_app.ViewModels.Tooltips;

using cpaplib;

using Color = System.Drawing.Color;

namespace cpap_app.Controls;

public partial class UsageHoursGraph : HistoryGraphBase
{
	#region Constructor 
	
	public UsageHoursGraph()
	{
		InitializeComponent();

		GraphTitle.Text = "Usage Time";
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

		var totalDays = viewModel.TotalDays;

		// NOTE: For some reason, we need to offset the beginning and end to account for centered bar offsets
		Chart.Plot.SetAxisLimitsX( -0.5, totalDays - 0.5 );
		Chart.Plot.XAxis.SetBoundary( -0.5, totalDays - 0.5 );
		
		Chart.Plot.SetAxisLimitsY( 0, 12 );
		Chart.Plot.YAxis.SetBoundary( 0, 12 );

		var days         = viewModel.Days;
		var values       = new double[ totalDays ];
		var maxUsageTime = 0.0;

		foreach( var day in days )
		{
			// There may be gaps in the days, so just calculate the index for each day.
			int index = (int)Math.Floor( (day.ReportDate.Date - viewModel.Start.Date).TotalDays );
			if( index < 0 || index >= totalDays )
			{
				continue;
			}

			// TODO: There is one date in the sample data that actually has a duplicate with the same date. Find out why. 
			Debug.Assert( values[ index ] == 0, "Duplicate index" );
			
			values[ index ] = day.TotalSleepTime.TotalHours;
			maxUsageTime    = Math.Max( maxUsageTime, values[ index ] );
		}

		var barChart = Chart.Plot.AddBar( values, DataColors.GetLightThemeColor( 0 ).ToDrawingColor() );
		barChart.BarWidth        = 0.95;
		barChart.BorderLineWidth = 1;

		var positions = new double[ 5 ];
		var labels    = new string[ 5 ];
		var maxTime   = maxUsageTime <= 12 ? 12.0 : 16.0;

		for( int i = 0; i < 5; i++ )
		{
			positions[ i ] = i == 0 ? 0.0 : i * maxTime / 4;
			labels[ i ]    = positions[ i ].ToString( "F2" );
		}

		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
        _selectionSpan.IgnoreAxisAuto = true;
        _selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}
	
	protected override object BuildTooltipDataContext( DailyReport day )
	{
		return new UsageHoursViewModel
		{
			Date           = day.ReportDate.Date,
			TotalTimeSpan  = day.TotalTimeSpan,
			TotalSleepTime = day.TotalSleepTime,
			NonTherapyTime = CalculateMaskOffTime( day ),
		};
	}

	private static TimeSpan CalculateMaskOffTime( DailyReport day )
	{
		var result = TimeSpan.Zero;

		var cpapSessions = day.Sessions.Where( x => x.SourceType == SourceType.CPAP ).ToList();

		for( int i = 1; i < cpapSessions.Count; i++ )
		{
			result += cpapSessions[ i ].StartTime - cpapSessions[ i - 1 ].EndTime;
		}

		return result;
	}

	#endregion 
}

