using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;

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

		var byMonth = ReportMode is { SelectedIndex: 1 };

		var end     = storedDates.Last();
		var start   = DateHelper.Max( storedDates[ 0 ], end.AddYears( -1 ) );
		var history = HistoryViewModel.GetHistory( profileID, start, end );
		var groups  = byMonth ? GroupDaysByMonth( history.Days, start, end ) : GroupDaysStandard( history.Days, start, end );

		var viewModel = new TherapyStatisticsViewModel();
		viewModel.Sections.Add( BuildCpapSection( groups ) );

		var showPulseOximetry = history.Days.Any( day => day.Sessions.Any( session => session.SourceType == SourceType.PulseOximetry ) );
		if( showPulseOximetry )
		{
			viewModel.Sections.Add( BuildOximetrySection( groups ) );
		}

		return viewModel;
	}
	
	private TherapyStatisticsSectionViewModel BuildOximetrySection( List<GroupedDays> groups )
	{
		var section = new TherapyStatisticsSectionViewModel
		{
			Label   = "Pulse Oximetry Statistics",
			Headers = groups,
		};

		section.Groups.Add( BuildOxygenStats( groups ) );

		return section;
	}
	
	private TherapyStatisticsGroupViewModel BuildOxygenStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Blood Oxygen Saturation",
		};

		group.Items.Add( CompileGroupAverages( "Average SpO2", groups, GetStatisticsValue( SignalNames.SpO2, stats => stats.Average ), value => $"{value:F2}%" ) );
		group.Items.Add( CompileGroupAverages( "Min SpO2",     groups, GetStatisticsValue( SignalNames.SpO2, stats => stats.Minimum ), value => $"{value:F2}%" ) );

		return group;
	}

	private TherapyStatisticsSectionViewModel BuildCpapSection( List<GroupedDays> groups )
	{
		var section = new TherapyStatisticsSectionViewModel
		{
			Label   = "CPAP Statistics",
			Headers = groups,
		};

		section.Groups.Add( BuildCPAPUsageStats( groups ) );
		section.Groups.Add( BuildEventsStats( groups ) );
		section.Groups.Add( BuildLeakStats( groups ) );
		section.Groups.Add( BuildPressureStats( groups ) );

		return section;
	}

	private static TherapyStatisticsGroupViewModel BuildPressureStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Pressure Statistics",
		};

		group.Items.Add( CompileGroupAverages( "Average Pressure", groups, GetStatisticsValue( SignalNames.Pressure, stats => stats.Average ) ) );
		group.Items.Add( CompileGroupAverages( "Min Pressure", groups, GetStatisticsValue( SignalNames.Pressure, stats => stats.Minimum ) ) );
		group.Items.Add( CompileGroupAverages( "Max Pressure", groups, GetStatisticsValue( SignalNames.Pressure, stats => stats.Maximum ) ) );
		group.Items.Add( CompileGroupAverages( "95th Percentile Pressure", groups, GetStatisticsValue( SignalNames.Pressure, stats => stats.Percentile95 ) ) );
		group.Items.Add( CompileGroupAverages( "Average EPAP", groups, GetStatisticsValue( SignalNames.EPAP, stats => stats.Average ) ) );
		group.Items.Add( CompileGroupAverages( "Min EPAP", groups, GetStatisticsValue( SignalNames.EPAP, stats => stats.Minimum ) ) );
		group.Items.Add( CompileGroupAverages( "Max EPAP", groups, GetStatisticsValue( SignalNames.EPAP, stats => stats.Maximum ) ) );

		return group;
	}

	private static TherapyStatisticsGroupViewModel BuildLeakStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Leak Statistics",
		};

		group.Items.Add( CompileGroupAverages( "Median leak rate",             groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Median ) ) );
		group.Items.Add( CompileGroupAverages( "Average leak rate",            groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Average ) ) );
		group.Items.Add( CompileGroupAverages( "95th Percentile leak rate",    groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Percentile95 ) ) );
		group.Items.Add( CompileGroupAverages( "Large Leak (% of total time)", groups, GetEventPercentage( EventType.LargeLeak ), value => $"{value:P2}" ) );

		return group;
	}

	private static Func<DailyReport, double> GetEventPercentage( EventType eventType )
	{
		return day =>
		{
			var totalLeakTime = day.Events.Where( x => x.Type == eventType ).Sum( x => x.Duration.TotalMinutes );
			if( totalLeakTime < float.Epsilon )
			{
				return 0;
			}

			return totalLeakTime / day.TotalSleepTime.TotalMinutes;
		};
	}

	private static Func<DailyReport, double> GetEventIndex( EventType eventType )
	{
		return day =>
		{
			var eventCount = day.Events.Count( x => x.Type == eventType );
			if( eventCount == 0 )
			{
				return 0;
			}

			return eventCount / day.TotalSleepTime.TotalHours;
		};
	}

	private static Func<DailyReport, double> GetStatisticsValue( string signalName, Func<SignalStatistics, double> valueFunc )
	{
		return day =>
		{
			var stat = day.Statistics.FirstOrDefault( x => x.SignalName == signalName );

			return stat != null ? valueFunc( stat ) : 0;
		};
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

		// TODO: Only the first and last months should have their start and/or end times adjusted. All others should span the full month. 
		
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
			Label = "Respiratory Events",
		};

		group.Items.Add( CompileGroupAverages( "AHI",                                      groups, day => day.EventSummary.AHI ) );
		group.Items.Add( CompileGroupAverages( "Obstructive Apnea Index",                  groups, day => day.EventSummary.ObstructiveApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Hypopnea Index",                           groups, day => day.EventSummary.HypopneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Unclassified Apnea Index",                 groups, day => day.EventSummary.UnclassifiedApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Central Apnea Index",                      groups, day => day.EventSummary.CentralApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "RERA Index",                               groups, day => day.EventSummary.RespiratoryArousalIndex ) );
		// group.Items.Add( CompileGroupAverages( "Flow Reduction Index",                     groups, GetEventIndex( EventType.FlowReduction ), value => $"{value:F2}" ) );
		group.Items.Add( CompileGroupAverages( "Total Time in Apnea",                      groups, GetTotalTimeInApnea,                      value => $"{TimeSpan.FromSeconds( value ):hh\\:mm\\:ss}" ) );
		group.Items.Add( CompileGroupAverages( "Cheyne-Stokes Respiration (% total time)", groups, GetEventPercentage( EventType.CSR ),      value => $"{value:P2}" ) );

		return group;
	}
	
	private double GetTotalTimeInApnea( DailyReport day )
	{
		return day.Events
			.Where( x => EventTypes.Apneas.Contains( x.Type ) )
			.Sum( x => x.Duration.TotalSeconds );
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
	
	private void ReportMode_SelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( StatsContainer != null )
		{
			Grid.SetIsSharedSizeScope( StatsContainer, false );
		}
		DataContext = BuildStatisticsViewModel();

		if( StatsContainer != null )
		{
			Grid.SetIsSharedSizeScope( StatsContainer, true );
		}
	}
}
