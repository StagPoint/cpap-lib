using System;
using System.Drawing;
using System.Linq;

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
			var baseDate = day.ReportDate.Date;
			var minX     = (baseDate - viewModel.Start).TotalDays - 0.45;
			
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

		minStartTime = Math.Floor( minStartTime );
		maxEndTime   = Math.Ceiling( maxEndTime );

		Chart.Plot.SetAxisLimitsY( minStartTime - 1, maxEndTime + 1 );
		Chart.Plot.YAxis.SetBoundary( minStartTime - 1, maxEndTime + 1 );

		var positions = new double[ 5 ];
		var labels    = new string[ 5 ];
		var range     = (maxEndTime - minStartTime);
		
		for( int i = 0; i < 5; i++ )
		{
			var closestHour = (i == 0) ? minStartTime : Math.Round( minStartTime + i * range / 4 );
			var clockTime   = (int)(closestHour % 24);
			var labelValue  = (clockTime > 12) ? $"{clockTime - 12:F0} PM" : (clockTime == 0) ? "12 AM" : $"{clockTime:F0} AM";
			
			positions[ i ] = closestHour;
			labels[ i ]    = labelValue;
		}
		
		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.2f ), null );
        _selectionSpan.IgnoreAxisAuto = true;
        _selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}
	
	protected override object BuildTooltipDataContext( DailyReport day )
	{
		var cpapSessions = day.Sessions.Where( x => x.SourceType == SourceType.CPAP ).ToArray();

		if( cpapSessions.Length == 0 )
		{
			return new SessionTimesViewModel()
			{
				Date               = day.ReportDate.Date,
				NumberOfSessions   = 0,
				LongestSessionTime = TimeSpan.Zero,
			};
		}
		
		var longestSessionTime = TimeSpan.FromHours( cpapSessions.Max( x => x.Duration.TotalHours ) );

		return new SessionTimesViewModel()
		{
			Date               = day.ReportDate.Date,
			Start              = cpapSessions.Min( x => x.StartTime ),
			End                = cpapSessions.Max( x => x.EndTime ),
			TotalTimeSpan      = day.TotalTimeSpan,
			TotalSleepTime     = day.TotalSleepTime,
			NumberOfSessions   = cpapSessions.Length,
			LongestSessionTime = longestSessionTime,
		};
	}

	#endregion
}

