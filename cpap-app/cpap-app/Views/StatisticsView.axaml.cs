﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpap_db;
using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
		section.Groups.Add( BuildPulseStats( groups ) );

		return section;
	}
	
	private TherapyStatisticsGroupViewModel BuildOxygenStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Blood Oxygen Saturation",
		};

		group.Items.Add( CompileGroupAverages( "Average SpO2",               groups, GetStatisticsValue( SignalNames.SpO2, stats => stats.Average ), value => $"{value:F0}%" ) );

		group.Items.Add( CompileGroupAverages( "Min SpO2",                   groups, GetStatisticsValue( SignalNames.SpO2, stats => stats.Minimum ), value => $"{value:F0}%" ) );
		group.Items.Add( CompileGroupAverages( "Desaturation Index",         groups, GetEventIndex( EventType.Desaturation ),                        value => $"{value:F2}" ) );
		group.Items.Add( CompileGroupAverages( "Avg. Desaturation Duration", groups, GetAverageEventDuration( EventType.Desaturation ),              FormatTimespan ) );
		group.Items.Add( CompileGroupMaximums( "Max. Desaturation Duration", groups, GetMaxEventDuration( EventType.Desaturation ), FormatTimespan ) );
		
		group.Items.Add( CompileGroupAverages( "Hypoxemia Index",         groups, GetEventIndex( EventType.Hypoxemia ),           value => $"{value:F2}" ) );
		group.Items.Add( CompileGroupAverages( "Avg. Hypoxemia Duration", groups, GetAverageEventDuration( EventType.Hypoxemia ), FormatTimespan ) );
		group.Items.Add( CompileGroupMaximums( "Max. Hypoxemia Duration", groups, GetMaxEventDuration( EventType.Hypoxemia ), FormatTimespan ) );
		
		group.Items.Add( CompileGroupAverages( "Time in Hypoxemia (% of total time)", groups, GetEventPercentage( EventType.Hypoxemia ), value => $"{value:P2}" ) );

		return group;
	}

	private TherapyStatisticsGroupViewModel BuildPulseStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Pulse Rate",
		};

		group.Items.Add( CompileGroupAverages( "Average Pulse",                 groups, GetStatisticsValue( SignalNames.Pulse, stats => stats.Average ), value => $"{value:F0}" ) );
		group.Items.Add( CompileGroupAverages( "Min Pulse",                     groups, GetStatisticsValue( SignalNames.Pulse, stats => stats.Minimum ), value => $"{value:F0}" ) );
		group.Items.Add( CompileGroupAverages( "Max Pulse",                     groups, GetStatisticsValue( SignalNames.Pulse, stats => stats.Maximum ), value => $"{value:F0}" ) );
		group.Items.Add( CompileGroupAverages( "Rate Change Index",             groups, GetEventIndex( EventType.PulseRateChange ),                      value => $"{value:F2}" ) );
		group.Items.Add( CompileGroupAverages( "Tachycardia (% of total time)", groups, GetEventPercentage( EventType.Tachycardia ),                     value => $"{value:P2}" ) );
		group.Items.Add( CompileGroupAverages( "Bradycardia (% of total time)", groups, GetEventPercentage( EventType.Bradycardia ),                     value => $"{value:P2}" ) );

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
		section.Groups.Add( BuildPressureStats( groups ) );
		section.Groups.Add( BuildLeakStats( groups ) );
		section.Groups.Add( BuildRespirationStats( groups ) );

		return section;
	}

	private TherapyStatisticsGroupViewModel BuildEventsStats( List<GroupedDays> groupedDays )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Respiratory Events",
		};

		group.Items.Add( CompileGroupAverages( "AHI",                           groupedDays, day => day.EventSummary.AHI ) );
		group.Items.Add( CompileGroupAverages( "Peak AHI",                      groupedDays, day => day.EventSummary.PeakAHI ) );
		group.Items.Add( CompileGroupAverages( "Peak RDI",                      groupedDays, CalculatePeakRDI ) );
		group.Items.Add( CompileGroupAverages( "Obstructive Apnea Index",       groupedDays, day => day.EventSummary.ObstructiveApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Hypopnea Index",                groupedDays, day => day.EventSummary.HypopneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Unclassified Apnea Index",      groupedDays, day => day.EventSummary.UnclassifiedApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "Central Apnea Index",           groupedDays, day => day.EventSummary.CentralApneaIndex ) );
		group.Items.Add( CompileGroupAverages( "RERA Index",                    groupedDays, day => day.EventSummary.RespiratoryArousalIndex ) );
		group.Items.Add( CompileGroupAverages( "Flow Limit Index",              groupedDays, GetEventIndex( EventType.FlowLimitation ), value => $"{value:F2}" ) );
		group.Items.Add( CompileGroupAverages( "Total Time in Apnea (Average)", groupedDays, GetTotalTimeInApnea,                       FormatTimespan ) );

		if( groupedDays.Any( x => x.Days.Any( day => day.Events.Any( evt => evt.Type == EventType.CSR ) ) ) )
		{
			group.Items.Add( CompileGroupAverages( "Cheyne-Stokes Resp. (% of total time)", groupedDays, GetEventPercentage( EventType.CSR ), value => $"{value:P2}" ) );
		}
		else if( groupedDays.Any( x => x.Days.Any( day => day.Events.Any( evt => evt.Type == EventType.PeriodicBreathing ) ) ) )
		{
			group.Items.Add( CompileGroupAverages( "Periodic Breathing (% of total time)", groupedDays, GetEventPercentage( EventType.PeriodicBreathing ), value => $"{value:P2}" ) );
		}

		return group;
	}
	
	private double CalculatePeakRDI( DailyReport day )
	{
		var events    = day.Events.Where( x => EventTypes.RespiratoryDisturbance.Contains( x.Type ) );
		var window    = new List<DateTime>( 128 );
		var peakCount = 0;

		foreach( var e in events )
		{
			var currentTime = e.StartTime;
			window.Add( currentTime );

			var thresholdTime = currentTime.AddHours( -1 );
			while( window.Count > 0 && window[ 0 ] < thresholdTime )
			{
				window.RemoveAt( 0 );	
			}

			peakCount = Math.Max( peakCount, window.Count );
		}

		return peakCount;
	}

	private static TherapyStatisticsGroupViewModel BuildPressureStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Pressure",
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
			Label = "Leak Rate",
		};

		group.Items.Add( CompileGroupAverages( "Median leak rate",             groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Median ) ) );
		group.Items.Add( CompileGroupAverages( "Average leak rate",            groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Average ) ) );
		group.Items.Add( CompileGroupAverages( "95th Percentile leak rate",    groups, GetStatisticsValue( SignalNames.LeakRate, stats => stats.Percentile95 ) ) );
		group.Items.Add( CompileGroupAverages( "Large Leak (% of total time)", groups, GetEventPercentage( EventType.LargeLeak ), value => $"{value:P2}" ) );

		return group;
	}

	private static TherapyStatisticsGroupViewModel BuildRespirationStats( List<GroupedDays> groups )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = "Respiration",
		};

		group.Items.Add( CompileGroupAverages( "Median Respiration Rate",    groups, GetStatisticsValue( SignalNames.RespirationRate, stats => stats.Median ) ) );
		group.Items.Add( CompileGroupAverages( "Average Respiration Rate",   groups, GetStatisticsValue( SignalNames.RespirationRate, stats => stats.Average ) ) );
		group.Items.Add( CompileGroupAverages( "Median Tidal Volume",        groups, GetStatisticsValue( SignalNames.TidalVolume,     stats => stats.Median ) ) );
		group.Items.Add( CompileGroupAverages( "Median Minute Ventilation",  groups, GetStatisticsValue( SignalNames.MinuteVent,      stats => stats.Median ) ) );

		return group;
	}

	private static TherapyStatisticsGroupViewModel BuildSignalStats( List<GroupedDays> groups, string label, string signalName )
	{
		var group = new TherapyStatisticsGroupViewModel
		{
			Label = label,
		};

		group.Items.Add( CompileGroupAverages( "Median",          groups, GetStatisticsValue( signalName, stats => stats.Median ) ) );
		group.Items.Add( CompileGroupAverages( "Minimum",         groups, GetStatisticsValue( signalName, stats => stats.Minimum ) ) );
		group.Items.Add( CompileGroupAverages( "95th Percentile", groups, GetStatisticsValue( signalName, stats => stats.Percentile95 ) ) );

		return group;
	}

	private static Func<DailyReport, double> GetTotalTimeInEvent( EventType eventType )
	{
		return day =>
		{
			return day.Events.Where( x => x.Type == eventType ).Sum( x => x.Duration.TotalSeconds );
		};
	}

	private static Func<DailyReport, double> GetAverageEventDuration( EventType eventType )
	{
		return day =>
		{
			var matchingEvents = day.Events.Where( x => x.Type == eventType ).ToArray();
			
			var totalEventDuration = matchingEvents.Sum( x => x.Duration.TotalSeconds );
			if( totalEventDuration < float.Epsilon )
			{
				return 0;
			}

			return totalEventDuration / matchingEvents.Length;
		};
	}

	private static Func<DailyReport, double> GetMaxEventDuration( EventType eventType )
	{
		return day =>
		{
			if( !day.Events.Any( evt => evt.Type == eventType ) )
			{
				return 0;
			}
			
			var matchingEvents = day.Events.Where( evt => evt.Type == eventType );
			return matchingEvents.Max( evt => evt.Duration.TotalSeconds );
		};
	}

	private static Func<DailyReport, double> GetEventPercentage( EventType eventType )
	{
		return day =>
		{
			var totalEventDuration = day.Events.Where( x => x.Type == eventType ).Sum( x => x.Duration.TotalSeconds );
			if( totalEventDuration < float.Epsilon )
			{
				return 0;
			}

			return totalEventDuration / day.TotalSleepTime.TotalSeconds;
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

	private double GetTotalTimeInApnea( DailyReport day )
	{
		return day.Events
			.Where( x => EventTypes.Apneas.Contains( x.Type ) )
			.Sum( x => x.Duration.TotalSeconds );
	}

	private static TherapyStatisticsLineItemViewModel CompileGroupMaximums( string name, List<GroupedDays> groups, Func<DailyReport, double> averageFunc, Func<double, string>? conversionFunc = null )
	{
		var viewModel = new TherapyStatisticsLineItemViewModel() { Label = name };
		var maximums  = CompileGroupMaximums( groups, averageFunc );

		conversionFunc ??= ( value ) => $"{value:F2}";

		foreach( var average in maximums )
		{
			viewModel.Values.Add( conversionFunc( average ) );
		}

		return viewModel;
	}
	
	private static TherapyStatisticsLineItemViewModel CompileGroupAverages( string name, List<GroupedDays> groups, Func<DailyReport,double> averageFunc, Func<double, string>? conversionFunc = null )
	{
		var viewModel = new TherapyStatisticsLineItemViewModel() { Label = name };
		var averages  = CompileGroupAverages( groups, averageFunc, true );

		conversionFunc ??= ( value ) => $"{value:F2}";

		foreach( var average in averages )
		{
			viewModel.Values.Add( conversionFunc( average ) );
		}

		return viewModel;
	}

	private static List<double> CompileGroupAverages( List<GroupedDays> groups, Func<DailyReport,double> func, bool existingDaysOnly = true )
	{
		var result = new List<double>( groups.Count );

		foreach( var group in groups )
		{
			double totalValue = 0;
			int    totalCount = existingDaysOnly ? 0 : group.Days.Count;

			foreach( var day in group.Days )
			{
				var dailyValue = func( day );

				if( existingDaysOnly && dailyValue > 0 )
				{
					totalValue += dailyValue;
					totalCount += 1;
				}
				else
				{
					totalValue += dailyValue;
				}
			}

			var average = totalCount > 0 ? totalValue / totalCount : 0.0;

			result.Add( average );
		}

		return result;
	}

	private static List<double> CompileGroupMaximums( List<GroupedDays> groups, Func<DailyReport,double> func )
	{
		var result = new List<double>( groups.Count );

		foreach( var group in groups )
		{
			double maxValue = 0;

			foreach( var day in group.Days )
			{
				maxValue = Math.Max( maxValue, func( day ) );
			}

			result.Add( maxValue );
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
		group.Items.Add( CompileAverageSessionDuration( groups ) );
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

	private static TherapyStatisticsLineItemViewModel CompileAverageSessionDuration( List<GroupedDays> groups )
	{
		var averages = CompileGroupAverages( groups, day => day.Sessions.Where( x => x.SourceType == SourceType.CPAP ).Average( x => x.Duration.TotalSeconds ) );
		
		return new TherapyStatisticsLineItemViewModel
		{
			Label  = "Average Session Duration",
			Values = averages.Select( FormatTimespan ).ToList()
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
			Label  = "Compliance (\u2265 4 hours per day)",
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
	
	private static List<GroupedDays> GroupDaysByMonth( List<DailyReport> days, DateTime startDay, DateTime endDay )
	{
		var results = new List<GroupedDays>();

		results.Add( new GroupedDays()
		{
			Label     = "Most Recent",
			DateLabel = $"{startDay:d}",
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
			DateLabel = $"{startDay:d}",
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
	
	private static string FormatTimespan( double value )
	{
		return $@"{TimeSpan.FromSeconds( value ):hh\:mm\:ss}";
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
	
	private async Task<string?> GetSaveFilename()
	{
		var sp = TopLevel.GetTopLevel( this )?.StorageProvider;
		if( sp == null )
		{
			throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
		}

		var filePicker = await sp.SaveFilePickerAsync( new FilePickerSaveOptions()
		{
			Title                  = $"Save to PDF File",
			SuggestedStartLocation = null,
			DefaultExtension       = ".pdf",
			ShowOverwritePrompt    = true,
		} );

		return filePicker?.Path.LocalPath;
	}
	
	private async void PrintReport_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not TherapyStatisticsViewModel viewModel )
		{
			throw new InvalidOperationException();
		}

		var saveFilePath = await GetSaveFilename();
		if( string.IsNullOrEmpty( saveFilePath ) )
		{
			return;
		}

		var userName = UserProfileStore.GetActiveUserProfile().UserName;
		
		var pdfDocument = Document.Create( document =>
		{
			foreach( var section in viewModel.Sections )
			{
				document.Page( page =>
				{
					page.Size( PageSizes.Letter.Landscape() );
					page.Margin( 8, Unit.Point );
					page.PageColor( Colors.White );
					page.DefaultTextStyle( x => x.FontSize( 8 ) );

					page.Header()
					    .AlignCenter()
					    .Text( section.Label )
					    .SemiBold().FontSize( 12 ).FontColor( Colors.Grey.Darken2 );

                    PrintSection( document, page, section );
                    
					page.Footer()
					    .AlignCenter()
					    .Table( table =>
					    {
						    table.ColumnsDefinition( columns =>
						    {
							    columns.RelativeColumn();
							    columns.RelativeColumn( 3 );
							    columns.RelativeColumn();
						    });
							    
							table.Cell().Column( 1 )
						    .Text( x =>
						    {
							    x.Span( "Page " );
							    x.CurrentPageNumber();
						    } );

							table.Cell().Column( 2 )
							     .AlignCenter()
							     .Text( $"Printed on {DateTime.Today:d} at {DateTime.Now:t}" );

							table.Cell().Column( 3 )
							     .AlignRight()
							     .Text( $"Use Profile: {userName}" );
					    });
				} );
			}
		} );
		
		pdfDocument.GeneratePdf( saveFilePath );
		
		Process process = new Process();
		process.StartInfo = new ProcessStartInfo(saveFilePath) { UseShellExecute = true };
		process.Start();
		await process.WaitForExitAsync();
	}

	private static void PrintSection( IDocumentContainer document, PageDescriptor page, TherapyStatisticsSectionViewModel section )
	{
		page.Content()
		    .PaddingVertical( 6, Unit.Point )
		    .Table(table =>
		    {
			    table.ColumnsDefinition(columns =>
			    {
				    columns.RelativeColumn( 2 );
		   
				    for( int i = 0; i < section.Headers.Count; i++ )
				    {
					    columns.RelativeColumn();
				    }
			    });
		   
			    table.Cell().Column( 0 ).Row( 0 ).Element( PrimaryColumnHeader ).AlignLeft().Text( "Details" ).SemiBold();
		   
			    uint columnIndex = 2;
			    foreach( var header in section.Headers )
			    {
					table.Cell()
					     .Row( 0 )
					     .Column( columnIndex )
					     .Element( PrimaryColumnHeader )
					     .AlignCenter()
					     .Text( header.Label )
					     .SemiBold()
					     .FontColor( Colors.Black )
					     .WrapAnywhere( false );
					
				    columnIndex += 1;
			    }

			    uint groupRowIndex = 2;
			    foreach( var group in section.Groups )
			    {
				    table.Cell()
				         .Column( 0 )
				         .Row( groupRowIndex )
				         .ColumnSpan( (uint)section.Headers.Count + 1 )
				         .Element( SectionHeader )
				         .Text( group.Label )
				         .SemiBold()
					    ;

				    foreach( var item in group.Items )
				    {
					    groupRowIndex += 1;

					    table.Cell()
					         .Column( 0 )
					         .Row( groupRowIndex )
					         .PaddingLeft( 4, Unit.Point )
					         .Text( item.Label );

					    uint valueColumnIndex = 2;

					    foreach( var value in item.Values )
					    {
						    table.Cell()
						         .Column( valueColumnIndex )
						         .Row( groupRowIndex )
						         .PaddingLeft( 4, Unit.Point )
						         .Text( value );

						    valueColumnIndex += 1;
					    }
				    }

				    table.Cell()
				         .Column( 1 )
				         .Row( ++groupRowIndex )
				         .ColumnSpan( (uint)section.Headers.Count + 1 )
				         .PaddingLeft( 4, Unit.Point )
				         .Text( string.Empty );

				    groupRowIndex += 1;
			    }

			    return;

			    static IContainer SectionHeader( IContainer container )
			    {
				    return container
				           .Border( 0.5f )
				           .Background( Colors.Grey.Lighten3 )
				           .PaddingLeft( 2, Unit.Point )
				           .PaddingRight( 2, Unit.Point )
				           .AlignTop();
			    }

			    static IContainer PrimaryColumnHeader( IContainer container )
			    {
				    return container
				           .Border( 0.5f )
				           .Background( Colors.Grey.Lighten2 )
				           .PaddingLeft( 2, Unit.Point )
				           .PaddingRight( 2, Unit.Point )
				           .AlignTop();
			    }
		    });
	}
}
