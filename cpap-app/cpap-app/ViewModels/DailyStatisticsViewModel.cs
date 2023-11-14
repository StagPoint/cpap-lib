using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using cpap_db;

using cpaplib;

using ReactiveUI;

namespace cpap_app.ViewModels;

public class DailyStatisticsColumnVisibility : ReactiveObject
{
	#region Private fields

	private bool _minimum       = true;
	private bool _average       = true;
	private bool _median        = false;
	private bool _percentile95  = true;
	private bool _percentile995 = true;
	private bool _maximum       = false;
	private bool _avgDeviation  = false;
	
	#endregion

	public bool AllowCustomization { get; set; } = true;

	public bool Minimum
	{
		get => _minimum;
		set => this.RaiseAndSetIfChanged( ref _minimum, value );
	}
	
	public bool Average
	{
		get => _average;
		set => this.RaiseAndSetIfChanged( ref _average, value );
	}
	
	public bool Median
	{
		get => _median;
		set => this.RaiseAndSetIfChanged( ref _median, value );
	}

	public bool Percentile95
	{
		get => _percentile95;
		set => this.RaiseAndSetIfChanged( ref _percentile95, value );
	}

	public bool Percentile995
	{
		get => _percentile995;
		set => this.RaiseAndSetIfChanged( ref _percentile995, value );
	}

	public bool Maximum
	{
		get => _maximum;
		set => this.RaiseAndSetIfChanged( ref _maximum, value );
	}

	public bool AverageDeviation
	{
		get => _avgDeviation;
		set => this.RaiseAndSetIfChanged( ref _avgDeviation, value );
	}
}

public class DailyStatisticsViewModel
{
	public DailyStatisticsColumnVisibility VisibleColumns { get; set; }
	public List<SignalStatistics>          Statistics     { get; set; }

	static DailyStatisticsViewModel()
	{
		using var connection = StorageService.Connect();

		var mapping = StorageService.CreateMapping<DailyStatisticsColumnVisibility>( "stats_columns" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );

		connection.CreateTable<DailyStatisticsColumnVisibility>();

		if( connection.SelectById<DailyStatisticsColumnVisibility>( 0 ) == null )
		{
			var defaultValues = new DailyStatisticsColumnVisibility();
			connection.Insert( defaultValues, primaryKeyValue: 0 );
		}
	}

	public DailyStatisticsViewModel( List<SignalStatistics> stats, bool allowCustomColumns = false )
	{
		using var db = StorageService.Connect();

		// TODO: Should column order be configurable also?

		if( allowCustomColumns )
		{
			// Retrieve the list of visible columns 
			VisibleColumns                    =  db.SelectById<DailyStatisticsColumnVisibility>( 0 );
			VisibleColumns.AllowCustomization =  true;
			VisibleColumns.PropertyChanged    += VisibleColumnsOnPropertyChanged;
		}
		else
		{
			VisibleColumns = new DailyStatisticsColumnVisibility
			{
				AllowCustomization = false,
				Minimum            = true,
				Average            = false,
				Median             = true,
				Percentile95       = true,
				Percentile995      = true,
				Maximum            = true,
				AverageDeviation   = false
			};
		}

		// Retrieve the Signal Chart Configurations so that we can re-use the DisplayOrder values the user has configured 
		var configurations = SignalChartConfigurationStore.GetSignalConfigurations();
		
		Statistics = new List<SignalStatistics>();
		
		foreach( var configuration in configurations )
		{
			// Attempt to retrieve the signal statistic named by the configuration record
			var stat = stats.FirstOrDefault( x => x.SignalName.Equals( configuration.SignalName, StringComparison.OrdinalIgnoreCase ) );
			if( stat == null )
			{
				continue;
			}
			
			// If the statistic is already in our result set, move on
			if( Statistics.Any( x => x.SignalName == stat.SignalName ) )
			{
				continue;
			}
			
			// Add the statistic to the result set
			Statistics.Add( stat );

			// If the signal is configured to have a pair (such as with Pressure and EPAP), attempt to add the paired signal immediately after
			// the current signal. The paired signal (called SecondarySignal in the chart configuration) will likely be sorted near the 
			// end of the list of signals, and we want it to appear with the main signal. 
			if( !string.IsNullOrEmpty( configuration.SecondarySignalName ) )
			{
				var secondaryStat = stats.FirstOrDefault( x => x.SignalName.Equals( configuration.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) );
				if( secondaryStat == null )
				{
					continue;
				}
				
				// Make sure we haven't added it already. If so, just skip it. 
				if( Statistics.Any( x => x.SignalName.Equals( configuration.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) ) )
				{
					continue;
				}

				Statistics.Add( secondaryStat );
			}
		}
		
		// Add in any statistics that have not yet been included (presumably because there is no configuration record for it)
		foreach( var stat in stats )
		{
			if( !Statistics.Any( x => x.SignalName == stat.SignalName ) )
			{
				Statistics.Add( stat );
			}
		}
		
		// Now that we have all of our Statistics in the order we want them, assign the configured names for them 
		foreach( var stat in Statistics )
		{
			var config = configurations.FirstOrDefault( x => x.SignalName.Equals( stat.SignalName, StringComparison.OrdinalIgnoreCase ) );
			if( config != null )
			{
				stat.SignalName = config.Title;
			}
		}
	}

	public DailyStatisticsViewModel( DailyReport day ) : this( day.Statistics, true )
	{
	}
	
	private void VisibleColumnsOnPropertyChanged( object? sender, PropertyChangedEventArgs e )
	{
		using var db = StorageService.Connect();
		db.Update( VisibleColumns, 0 );
	}
}
