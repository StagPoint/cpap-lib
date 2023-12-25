using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpap_db;

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
		var profileID   = UserProfileStore.GetActiveUserProfile().UserProfileID;
		var storedDates = StorageService.Connect().GetStoredDates( profileID );

		if( storedDates.Count == 0 )
		{
			return new TherapyStatisticsViewModel();
		}

		var end     = storedDates.Last();
		var start   = DateHelper.Max( storedDates[ 0 ], end.AddYears( -1 ) );
		var history = HistoryViewModel.GetHistory( profileID, start, end );
		var groups  = true ? GroupDaysByMonth( history.Days, start, end ) : GroupDaysStandard( history.Days, start, end );

		var viewModel = new TherapyStatisticsViewModel
		{
			Headers = groups,
		};

		viewModel.Groups.Add( BuildEventsStats( groups ) );
		viewModel.Groups.Add( BuildCPAPUsageStats( groups ) );
		viewModel.Groups.Add( BuildLeakStats( groups ) );

		return viewModel;
	}
	
	private TherapyStatisticsGroupViewModel BuildLeakStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Leak Statistics",
		};

		group.Items.Add( CompileGroupAverages( "Average leak rate",         groups, GetAverageLeakRate, value => $"{value:F2} L/min" ) );
		group.Items.Add( CompileGroupAverages( "95th Percentile leak rate", groups, GetMaxLeakRate, value => $"{value:F2} L/min" ) );

		return group;
	}
	
	private double GetAverageLeakRate( DailyReport day )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.LeakRate );

		return stat?.Average ?? 0;
	}

	private double GetMaxLeakRate( DailyReport day )
	{
		var stat = day.Statistics.FirstOrDefault( x => x.SignalName == SignalNames.LeakRate );

		return stat?.Percentile95 ?? 0;
	}

	private static List<GroupedDays> GroupDaysByMonth( List<DailyReport> days, DateTime startDay, DateTime endDay )
	{
		var results = new List<GroupedDays>();

		results.Add( new GroupedDays()
		{
			Label     = "Most Recent",
			DateLabel = string.Empty,
			StartDate = endDay.Date,
			EndDate   = endDay.Date,
		} );

		var lastMonthStart = new DateTime( endDay.Year, endDay.Month, 1 ).AddMonths( 1 );

		for( int i = 0; i < 12; i++ )
		{
			var monthStart = lastMonthStart.AddMonths( -1 );
			var monthEnd   = DateHelper.Min( lastMonthStart.AddDays( -1 ), endDay );

			if( monthEnd < startDay )
			{
				break;
			}

			if( days.Any( x => x.ReportDate.Date <= monthEnd ) )
			{
				results.Add( new GroupedDays()
				{
					Label     = $"{monthStart:MMMM}",
					DateLabel = $"{monthStart:yyyy}",
					StartDate = DateHelper.Max( startDay, monthStart ),
					EndDate   = monthEnd,
				} );
			}

			lastMonthStart = monthStart;
		}

		foreach( var group in results )
		{
			group.Days.AddRange( days.Where( x => x.ReportDate.Date >= group.StartDate && x.ReportDate.Date <= group.EndDate ) );
		}
		
		return results;
	}
	
	private static List<GroupedDays> GroupDaysStandard( List<DailyReport> days, DateTime startDay, DateTime endDay )
	{
		var results = new List<GroupedDays>();

		results.Add( new GroupedDays()
		{
			Label     = "Most Recent",
			StartDate = endDay.Date,
			EndDate   = endDay.Date,
		} );

		var groupStartDate = DateHelper.Max( startDay, endDay.Date.AddDays( -6 ) );
		results.Add( new GroupedDays()
		{
			Label     = "Last Week",
			StartDate = groupStartDate,
			EndDate   = endDay.Date,
		} );

		groupStartDate = DateHelper.Max( startDay, endDay.Date.AddMonths( -1 ) );
		results.Add( new GroupedDays()
		{
			Label     = "Last Month",
			StartDate = groupStartDate,
			EndDate   = endDay.Date,
		} );

		groupStartDate = DateHelper.Max( startDay, endDay.Date.AddMonths( -3 ) );
		results.Add( new GroupedDays()
		{
			Label     = "Last Three Months",
			StartDate = groupStartDate,
			EndDate   = endDay.Date,
		} );

		groupStartDate = DateHelper.Max( startDay, endDay.Date.AddYears( -1 ) );
		results.Add( new GroupedDays()
		{
			Label     = "Last Year",
			StartDate = groupStartDate,
			EndDate   = endDay.Date,
		} );

		foreach( var group in results )
		{
			if( group.StartDate < group.EndDate )
			{
				group.DateLabel = $"{group.StartDate:d} - {group.EndDate:d}";
			}
			
			group.Days.AddRange( days.Where( x => x.ReportDate.Date >= group.StartDate && x.ReportDate.Date <= group.EndDate ) );
		}
		
		return results;
	}
	
	private TherapyStatisticsGroupViewModel BuildEventsStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Respiratory Event Indices",
		};

		group.Items.Add( CompileGroupAverages( "AHI", groups, day => day.EventSummary.AHI ) );
		group.Items.Add( CompileGroupAverages( "Obstructive Apnea Index", groups, day => day.EventSummary.ObstructiveApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Hypopnea Index", groups, day => day.EventSummary.HypopneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Unclassified Apnea Index", groups, day => day.EventSummary.UnclassifiedApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Central Apnea Index", groups, day => day.EventSummary.CentralApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "RERA Index", groups, day => day.EventSummary.RespiratoryArousalIndex ) );

		return group;
	}
	
	private static TherapyStatisticsLineItemViewModel CompileGroupAverages( string name, List<GroupedDays> groups, Func<DailyReport,double> averageFunc, Func<double, string>? conversionFunc = null )
	{
		var viewModel = new TherapyStatisticsLineItemViewModel() { Label = name };
		var averages  = CompileGroupAverages( groups, averageFunc );

		conversionFunc ??= ( value ) => $"{value:F2}";

		foreach( var average in averages )
		{
			viewModel.Values.Add( conversionFunc( average ) );
		}

		return viewModel;
	}

	private static List<double> CompileGroupAverages( List<GroupedDays> groups, Func<DailyReport,double> func )
	{
		var result = new List<double>( groups.Count );

		foreach( var group in groups )
		{
			double totalValue = 0;

			foreach( var day in group.Days )
			{
				totalValue += func( day );
			}

			var average = group.Days.Count > 0 ? totalValue / group.Days.Count : 0.0;

			result.Add( average );
		}

		return result;
	}

	private static TherapyStatisticsGroupViewModel BuildCPAPUsageStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label  = "Therapy Time",
		};

		group.Items.Add( CalculateCompliancePerPeriod( groups ) );
		group.Items.Add( CalculateAverageUsagePerPeriod( groups ) );
		group.Items.Add( CompileAverageNumberOfSessions( groups ) );
		group.Items.Add( CalculateAverageSleepEfficiency( groups ) );

		return group;
	}
	
	private static TherapyStatisticsLineItemViewModel CalculateAverageSleepEfficiency( List<GroupedDays> groups )
	{
		var averages = CompileGroupAverages( groups, day => day.CalculateSleepEfficiency() );
		
		return new TherapyStatisticsLineItemViewModel
		{
			Label  = "Average sleep efficiency",
			Values = averages.Select( x => $"{x:P1}" ).ToList()
		};
	}

	private static TherapyStatisticsLineItemViewModel CompileAverageNumberOfSessions( List<GroupedDays> groups )
	{
		var averages = CompileGroupAverages( groups, day => day.Sessions.Count( x => x.SourceType == SourceType.CPAP ) );
		
		return new TherapyStatisticsLineItemViewModel
		{
			Label  = "Average number of sessions",
			Values = averages.Select( x => $"{x:N0}" ).ToList()
		};
	}

	private static TherapyStatisticsLineItemViewModel CalculateCompliancePerPeriod( List<GroupedDays> groups )
	{
		var complianceValues = new List<double>( groups.Count );
		for( int i = 0; i < groups.Count; i++ )
		{
			complianceValues.Add( GetCompliancePercentage( groups[ i ] ) );
		}

		var complianceModel = new TherapyStatisticsLineItemViewModel
		{
			Label  = "Compliance (> 4 hours per day)",
			Values = complianceValues.Select( x => $"{x:P0}" ).ToList()
		};

		return complianceModel;
	}

	private static TherapyStatisticsLineItemViewModel CalculateAverageUsagePerPeriod( List<GroupedDays> groups )
	{
		var averageSleepTimes = CompileGroupAverages( groups, day => day.TotalSleepTime.TotalHours );
		var averageUsageModel = new TherapyStatisticsLineItemViewModel
		{
			Label  = "Average usage per night",
			Values = averageSleepTimes.Select( x => TimeSpan.FromHours( x ).ToString( @"h\:mm" ) ).ToList()
		};

		return averageUsageModel;
	}

	private static double GetCompliancePercentage( GroupedDays days, double complianceThreshold = 4 )
	{
		var numberOfCompliantDays = days.Days.Count( x => x.TotalSleepTime.TotalHours >= complianceThreshold );
		var numberOfGroupDays     = (days.EndDate.Date - days.StartDate.Date).TotalDays + 1.0;

		return ( numberOfCompliantDays / numberOfGroupDays );
	}
}
