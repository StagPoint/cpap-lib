using System.Collections.Generic;

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

		VisibleColumns = db.SelectById<DailyStatisticsColumnVisibility>( 0 );
		
		Statistics = day.Statistics;
	}
}
