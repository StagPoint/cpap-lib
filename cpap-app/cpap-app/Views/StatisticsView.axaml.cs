using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

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

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is not TherapyStatisticsViewModel )
			{
				DataContext = BuildStatisticsViewModel();
			}
		}
	}

	private TherapyStatisticsViewModel BuildStatisticsViewModel()
	{
		var start     = DateTime.Today.AddYears( -1 );
		var end       = DateTime.Today;
		var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;
		var history   = HistoryViewModel.GetHistory( profileID, start, end );

		var viewModel = new TherapyStatisticsViewModel
		{
			MostRecentDate      = history.End,
			LastWeekStart       = DateHelper.Max( history.End.AddDays( -7 ),  history.Start ),
			LastMonthStart      = DateHelper.Max( history.End.AddDays( -30 ), history.Start ),
			LastNinetyDaysStart = DateHelper.Max( history.End.AddDays( -90 ), history.Start ),
			LastYearStart       = DateHelper.Max( history.End.AddYears( -1 ), history.Start ),
		};

		viewModel.Groups.Add(BuildCPAPUsageStats( history ) );
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

		group.Items.Add( CalcRespiratoryEventIndexSummary( "AHI", history, day => day.EventSummary.AHI ) );
		group.Items.Add( CalcRespiratoryEventIndexSummary( "Obstructive Apnea Index", history, day => day.EventSummary.ObstructiveApneaIndex ) );
		group.Items.Add( CalcRespiratoryEventIndexSummary( "Hypopnea Index", history, day => day.EventSummary.HypopneaIndex ) );
		group.Items.Add( CalcRespiratoryEventIndexSummary( "Unclassified Apnea Index", history, day => day.EventSummary.UnclassifiedApneaIndex ) );
		group.Items.Add( CalcRespiratoryEventIndexSummary( "Central Apnea Index", history, day => day.EventSummary.CentralApneaIndex ) );
		group.Items.Add( CalcRespiratoryEventIndexSummary( "RERA Index", history, day => day.EventSummary.RespiratoryArousalIndex ) );

		return group;
	}
	
	private static TherapyStatisticsItemViewModel CalcRespiratoryEventIndexSummary( string name, HistoryViewModel history, Func<DailyReport,double> func )
	{
		if( history.Days.Count == 0 )
		{
			return new TherapyStatisticsItemViewModel() { Name = name };
		}
		
		return new TherapyStatisticsItemViewModel
		{
			Name                 = name,
			MostRecentValue      = $"{func( history.Days[ ^1 ] ):F2}",
			LastWeekAverage      = $"{CalcHistoricalAverage( history, 7,   func ):F2}",
			LastMonthAverage     = $"{CalcHistoricalAverage( history, 30,  func ):F2}",
			LastNinetyDayAverage = $"{CalcHistoricalAverage( history, 90,  func ):F2}",
			LastYearAverage      = $"{CalcHistoricalAverage( history, 365, func ):F2}"
		};
	}

	private static double CalcHistoricalAverage( HistoryViewModel history, int count, Func<DailyReport,double> func )
	{
		var days = history.Days;
		
		double totalValue = 0;
		var    startDate  = DateHelper.Max( history.Start, history.End.AddDays( -(count - 1) ) );
		var    startIndex = days.FindIndex( x => x.ReportDate.Date >= startDate );

		if( startIndex < 0 )
		{
			return 0;
		}

		for( int i = startIndex; i < days.Count; i++ )
		{
			totalValue += func( days[ i ] );
		}

		var numberOfDays = Math.Min( count, history.TotalDays );

		return ( totalValue / numberOfDays );
	}

	private static TherapyStatisticsGroupViewModel BuildCPAPUsageStats( HistoryViewModel history )
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
	
	private static double GetCompliancePercentage( HistoryViewModel history, int count, double complianceThreshold = 4 )
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

	private static TimeSpan GetAverageSleepTime( HistoryViewModel history, int count )
	{
		var days = history.Days;
		
		double totalHours = 0;
		var    startDate  = DateHelper.Max( history.Start, history.End.AddDays( -(count - 1) ) );
		var    startIndex = days.FindIndex( x => x.ReportDate.Date >= startDate );

		if( startIndex < 0 )
		{
			return TimeSpan.Zero;
		}

		for( int i = startIndex; i < days.Count; i++ )
		{
			totalHours  += days[ i ].TotalSleepTime.TotalHours;
		}

		var numberOfDays = Math.Min( count, history.TotalDays );

		return TimeSpan.FromHours( totalHours / numberOfDays );
	}
}
