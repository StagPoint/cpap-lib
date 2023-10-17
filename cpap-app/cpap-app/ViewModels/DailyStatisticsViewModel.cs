using System;
using System.Collections.Generic;
using System.Linq;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyStatisticsColumnVisibility
{
	public bool Minimum       { get; set; } = true;
	public bool Average       { get; set; } = true;
	public bool Median        { get; set; } = false;
	public bool Percentile95  { get; set; } = true;
	public bool Percentile995 { get; set; } = true;
	public bool Maximum       { get; set; } = false;
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

	public DailyStatisticsViewModel( DailyReport day )
	{
		using var db = StorageService.Connect();

		// TODO: Should column order be configurable also?
		// Retrieve the list of visible columns 
		VisibleColumns = db.SelectById<DailyStatisticsColumnVisibility>( 0 );

		// Retrieve the Signal Chart Configurations so that we can re-use the DisplayOrder values the user has configured 
		var configurations = SignalChartConfigurationStore.GetSignalConfigurations();
		
		Statistics = new List<SignalStatistics>();
		foreach( var configuration in configurations )
		{
			var stat = day.Statistics.FirstOrDefault( x => x.SignalName.Equals( configuration.SignalName, StringComparison.OrdinalIgnoreCase ) );
			if( stat != null )
			{
				Statistics.Add( stat );
			}
		}
		
		// Add in any statistics that have not (yet?) been configured 
		foreach( var stat in day.Statistics )
		{
			if( !Statistics.Any( x => x.SignalName == stat.SignalName ) )
			{
				Statistics.Add( stat );
			}
		}
	}
}
