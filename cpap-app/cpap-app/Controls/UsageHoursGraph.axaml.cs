using System;
using System.Diagnostics;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;

using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_db;
using cpaplib;

using Color = System.Drawing.Color;

namespace cpap_app.Controls;

public partial class UsageHoursGraph : HistoryGraphBase
{
	#region Constructor 
	
	public UsageHoursGraph()
	{
		InitializeComponent();
		
		GraphTitle.Text   = "Usage Time";
	}
	
	#endregion 
	
	#region Private functions

	protected override void OnHover( PointerPoint mousePosition, int hoveredDayIndex, DateTime hoveredDate )
	{
		const int SPACING = 12;

		var day = _history.Days.FirstOrDefault( x => x.ReportDate.Date == hoveredDate );
		if( day == null )
		{
			ToolTip.SetIsOpen( this, false );
			return;
		}
		
		var tooltip = ToolTip.GetTip( this ) as ToolTip;
		Debug.Assert( tooltip != null, nameof( tooltip ) + " != null" );
		
		tooltip.DataContext = new UsageHoursViewModel
		{
			Date           = hoveredDate,
			TotalTimeSpan  = day.TotalTimeSpan,
			TotalSleepTime = day.TotalSleepTime,
			NonTherapyTime = CalculateMaskOffTime( day ),
		};

		tooltip.Measure( tooltip.DesiredSize );

		var axisLimits      = Chart.Plot.GetAxisLimits();
		var onLeftSide      = hoveredDayIndex < axisLimits.XCenter;
		var tooltipWidth    = tooltip.Bounds.Width;
		var tooltipPosition = !onLeftSide ? mousePosition.Position.X - SPACING : mousePosition.Position.X + SPACING + tooltipWidth;
		
		ToolTip.SetPlacement( this, PlacementMode.LeftEdgeAlignedTop );
		ToolTip.SetHorizontalOffset( this, tooltipPosition );
		ToolTip.SetVerticalOffset( this, mousePosition.Position.Y - tooltip.Bounds.Height + SPACING ); 
		ToolTip.SetIsOpen( this, true );
	}
	
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

		using var store = StorageService.Connect();

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

			Debug.Assert( values[ index ] == 0, "Duplicate index" );
			
			values[ index ] = day.TotalSleepTime.TotalHours;
			maxUsageTime    = Math.Max( maxUsageTime, values[ index ] );
		}

		var barChart = Chart.Plot.AddBar( values );
		barChart.BarWidth        = 0.95;
		barChart.BorderLineWidth = 1;

		var positions = new double[ 5 ];
		var labels    = new string[ 5 ];
		var maxTime   = maxUsageTime <= 12 ? 12.0 : 16.0;

		for( int i = 0; i < 5; i++ )
		{
			positions[ i ] = i == 0 ? 0.0 : i * maxTime / 4;
			labels[ i ]    = positions[ i ].ToString( "F0" );
		}

		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
        _selectionSpan.IgnoreAxisAuto = true;
        _selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}
	
	private TimeSpan CalculateMaskOffTime( DailyReport day )
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

