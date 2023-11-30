using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class HistoryView : UserControl
{
	public HistoryView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DataContext = BuildDataContext();
	}
	
	private HistoryViewModel BuildDataContext()
	{
		using var store = StorageService.Connect();

		var start = DateTime.Today.AddDays( -90 );
		var end   = DateTime.Today;

		var profileID    = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dayMapping   = StorageService.GetMapping<DailyReport>();
		var eventsMapping = StorageService.GetMapping<ReportedEvent>();
		var statsMapping = StorageService.GetMapping<SignalStatistics>();

		var dayQuery  = $@"
			SELECT * 
			FROM [{dayMapping.TableName}] 
			WHERE [{dayMapping.ForeignKey.ColumnName}] = ? AND [{nameof( DailyReport.ReportDate )}] BETWEEN ? AND ? 
			ORDER BY [{dayMapping.TableName}].[{dayMapping.PrimaryKey.ColumnName}]";
		
		var eventQuery = $@"
			SELECT [{eventsMapping.TableName}].* FROM [{dayMapping.TableName}]
			INNER JOIN [{eventsMapping.TableName}] ON [{eventsMapping.TableName}].{eventsMapping.ForeignKey.ColumnName} = [{dayMapping.TableName}].{dayMapping.PrimaryKey.ColumnName}
			WHERE [{dayMapping.TableName}].{dayMapping.ForeignKey.ColumnName} = ? AND [{dayMapping.TableName}].ReportDate BETWEEN ? AND ?
			ORDER BY [{dayMapping.TableName}].[{dayMapping.PrimaryKey.ColumnName}];
			";
		
		var statsQuery = $@"
			SELECT [{statsMapping.TableName}].* FROM [{dayMapping.TableName}]
			INNER JOIN [{statsMapping.TableName}] ON [{statsMapping.TableName}].{statsMapping.ForeignKey.ColumnName} = [{dayMapping.TableName}].{dayMapping.PrimaryKey.ColumnName}
			WHERE [{dayMapping.TableName}].{dayMapping.ForeignKey.ColumnName} = ? AND [{dayMapping.TableName}].ReportDate BETWEEN ? AND ? 
			ORDER BY [{dayMapping.TableName}].[{dayMapping.PrimaryKey.ColumnName}];
			";

		var startTime = Environment.TickCount;

		// Only load the part of the DailyReports that is going to be relevant to the consumer (skipping Signal data, for instance)
		var days = store.Query<DailyReport>( dayQuery, profileID, start, end );
		foreach( var day in days )
		{
			day.Events     = store.SelectByForeignKey<ReportedEvent>( day.ID );
			day.Statistics = store.SelectByForeignKey<SignalStatistics>( day.ID );
			day.Sessions   = store.SelectByForeignKey<Session>( day.ID );
		}
		
		/*
		var statsRows  = store.Query( statsQuery, profileID, start, end );
		var stats      = statsMapping.ExtractFromDataRows( statsRows );
		var statsIndex = 0;

		var eventsRows  = store.Query( eventQuery, profileID, start, end );
		var events      = eventsMapping.ExtractFromDataRows( eventsRows );
		var eventsIndex = 0;
		
		foreach( var day in days )
		{
			// Advance the index to the start of the current DailyReport's events 
			while( eventsIndex < events.Count && (long)eventsRows[ eventsIndex ][ 0 ] < day.ID )
			{
				eventsIndex += 1;
			}
			
			// Add each of the day's events
			while( eventsIndex < eventsRows.Count && (long)eventsRows[ eventsIndex ][ 0 ] == day.ID )
			{
				day.Events.Add( events[ eventsIndex ] );
				eventsIndex += 1;
			}

			// Advance the index to the start of the current DailyReport's stats 
			while( statsIndex < statsRows.Count && (long)statsRows[ statsIndex ][ 0 ] < day.ID )
			{
				statsIndex += 1;
			}

			// Add each of the day's stats
			while( statsIndex < statsRows.Count && (long)statsRows[ statsIndex ][ 0 ] == day.ID )
			{
				day.Statistics.Add( stats[ statsIndex ] );
				statsIndex += 1;
			}
		}
		*/

		var elapsed = Environment.TickCount - startTime;
		Debug.WriteLine( $"Retrieved {days.Count} objects in {elapsed}ms" );

		var viewModel = new HistoryViewModel()
		{
			Start = start,
			End   = end,
			Days  = days
		};

		return viewModel;
	}
}

