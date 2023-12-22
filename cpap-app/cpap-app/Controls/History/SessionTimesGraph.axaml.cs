using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_app.ViewModels.Tooltips;

using cpaplib;

namespace cpap_app.Controls;

public partial class SessionTimesGraph : HistoryGraphBase
{
	#region Constructor 
	
	public SessionTimesGraph()
	{
		InitializeComponent();

		GraphTitle.Text = "Session Times";
	}
	
	#endregion 
	
	#region Base class overrides 

	protected override void LoadData( HistoryViewModel viewModel )
	{
		_history = viewModel;
		
		Chart.Plot.Clear();

		var totalDays = viewModel.TotalDays;
		if( totalDays == 0 )
		{
			return;
		}

		NoDataLabel.IsVisible = false;
		Chart.IsEnabled       = true;
		this.IsEnabled        = true;

		// NOTE: For some reason, we need to offset the beginning and end to account for centered bar offsets
		Chart.Plot.SetAxisLimitsX( -0.5, totalDays - 0.5 );
		Chart.Plot.XAxis.SetBoundary( -0.5, totalDays - 0.5 );
		
		var days         = viewModel.Days;
		var minStartTime = double.MaxValue;
		var maxEndTime   = double.MinValue;

		foreach( var day in days )
		{
			var baseDate = day.ReportDate.Date.AddHours( 12 );
			var minX     = (day.ReportDate.Date - viewModel.Start).TotalDays - 0.45;
			
			foreach( var session in day.Sessions )
			{
				if( session.SourceType != SourceType.CPAP || session.Duration.TotalMinutes < 5 )
				{
					continue;
				}
				
				var minY = (session.StartTime - baseDate).TotalHours;
				var maxY = (session.EndTime - baseDate).TotalHours;

				minStartTime = Math.Min( minStartTime, minY );
				maxEndTime   = Math.Max( maxEndTime, maxY );

				var bar = Chart.Plot.AddRectangle( minX, minX + 0.9, minY, maxY );
				bar.Color           = Color.DodgerBlue;
				bar.BorderColor     = Color.Black;
				bar.BorderLineWidth = 0.5f;
			}
		}

		Chart.Plot.SetAxisLimitsY( minStartTime - 1, maxEndTime + 1 );
		Chart.Plot.YAxis.SetBoundary( minStartTime - 1, maxEndTime + 1 );

		// var positions = new double[ 5 ];
		// var labels    = new string[ 5 ];
		// var range     = (maxEndTime - minStartTime);
		//
		// for( int i = 0; i < 5; i++ )
		// {
		// 	positions[ i ] = i == 0 ? 0.0 : i * range / 4;
		// 	labels[ i ]    = positions[ i ].ToString( "F2" );
		// }
		//
		// Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
        _selectionSpan.IgnoreAxisAuto = true;
        _selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}
	
	protected override object BuildTooltipDataContext( DailyReport day )
	{
		var longestSessionTime = TimeSpan.Zero;
		if( day.Sessions.Count > 0 )
		{
			longestSessionTime = TimeSpan.FromHours( 
				day.Sessions
				   .Where( x => x.SourceType == SourceType.CPAP )
				   .Max( x => x.Duration.TotalHours ) 
			);
		}

		return new SessionTimesViewModel()
		{
			Date               = day.ReportDate.Date,
			TotalTimeSpan      = day.TotalTimeSpan,
			TotalSleepTime     = day.TotalSleepTime,
			NumberOfSessions   = day.Sessions.Count,
			LongestSessionTime = longestSessionTime,
		};
	}

	#endregion
}

