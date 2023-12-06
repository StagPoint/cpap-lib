using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.TextFormatting;

using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.Core;

namespace cpap_app.Views;

public partial class StatisticsView : UserControl
{
	public StatisticsView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DataContext = BuildStatisticsViewModel();
	}

	private TherapyStatisticsViewModel BuildStatisticsViewModel()
	{
		var viewModel = new TherapyStatisticsViewModel();

		var start     = DateTime.Today.AddYears( -1 );
		var end       = DateTime.Today;
		var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;
		var history   = HistoryViewModel.GetHistory( profileID, start, end );
		
		viewModel.Groups.Add( BuildCPAPUsageStats( history ) );
		viewModel.Groups.Add( BuildEventsStats( history ) );

		return viewModel;
	}
	
	private TherapyStatisticsGroupViewModel BuildEventsStats( HistoryViewModel history )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Name = "Respiratory Event Indices",
		};

		var days          = history.Days;
		var mostRecentDay = history.Days[ ^1 ];

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "AHI",
			MostRecentValue      = $"{mostRecentDay.EventSummary.AHI:F2}",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Obstructive Apnea Index",
			MostRecentValue      = $"{mostRecentDay.EventSummary.ApneaIndex:F2}",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Hypopnea Index",
			MostRecentValue      = $"{mostRecentDay.EventSummary.HypopneaIndex:F2}",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Clear Airway Index",
			MostRecentValue      = "1.13",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Flow Limitation Index",
			MostRecentValue      = "1.13",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "RERA Index",
			MostRecentValue      = "1.13",
			LastWeekAverage      = "2.33",
			LastMonthAverage     = "2.33",
			LastNinetyDayAverage = "3.24",
			LastYearAverage      = "2.67"
		} );

		return group;
	}

	private TherapyStatisticsGroupViewModel BuildCPAPUsageStats( HistoryViewModel history )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Name  = "Therapy Time",
		};

		var days          = history.Days;
		var mostRecentDay = history.Days[ ^1 ];

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Average usage per night",
			MostRecentValue      = mostRecentDay.TotalSleepTime.ToString( @"hh\:mm" ),
			LastWeekAverage      = GetAverageSleepTime( history, 7 ).ToString( @"hh\:mm" ),
			LastMonthAverage     = GetAverageSleepTime( history, 30 ).ToString( @"hh\:mm" ),
			LastNinetyDayAverage = GetAverageSleepTime( history, 90 ).ToString( @"hh\:mm" ),
			LastYearAverage      = GetAverageSleepTime( history, 365 ).ToString( @"hh\:mm" ),
		} );

		group.Items.Add( new TherapyStatisticsItemViewModel
		{
			Name                 = "Compliance (> 4 hours per day)",
			MostRecentValue      = mostRecentDay.TotalSleepTime.TotalHours >= 4 ? "100%" : "0%",
			LastWeekAverage      = $"{GetCompliancePercentage( history, 7 ):P0}",
			LastMonthAverage     = $"{GetCompliancePercentage( history, 30 ):P0}",
			LastNinetyDayAverage = $"{GetCompliancePercentage( history, 90 ):P0}",
			LastYearAverage      = $"{GetCompliancePercentage( history, 365 ):P0}"
		} );

		return group;
	}
	
	private double GetCompliancePercentage( HistoryViewModel history, int count, double complianceThreshold = 4 )
	{
		var days      = history.Days;
		var startDate = DateHelper.Max( history.Start, history.End.AddDays( -(count - 1) ) );

		double numberOfCompliantDays = 0;

		int i = days.Count - 1;
		while( i > 0 && days[ i ].ReportDate >= startDate )
		{
			if( days[ i ].TotalSleepTime.TotalHours >= complianceThreshold )
			{
				numberOfCompliantDays += 1;
			}

			i -= 1;
		}
		
		var numberOfDays = Math.Min( count, history.TotalDays );

		return ( numberOfCompliantDays / numberOfDays );
	}

	private TimeSpan GetAverageSleepTime( HistoryViewModel history, int count )
	{
		var days = history.Days;
		
		double totalHours = 0;
		var    startDate  = DateHelper.Max( history.Start, history.End.AddDays( -count ) );
		var    startIndex = days.FindIndex( x => x.ReportDate.Date >= startDate );

		for( int i = startIndex; i < days.Count; i++ )
		{
			totalHours  += days[ i ].TotalSleepTime.TotalHours;
		}

		var numberOfDays = Math.Min( count, history.TotalDays );

		return TimeSpan.FromHours( totalHours / numberOfDays );
	}
}
