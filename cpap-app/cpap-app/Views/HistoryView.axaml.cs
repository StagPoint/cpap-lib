using System;
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

		var start = DateTime.Today.AddDays( -365 );
		var end   = DateTime.Today;

		var profileID    = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dayMapping   = StorageService.GetMapping<DailyReport>();
		var eventMapping = StorageService.GetMapping<ReportedEvent>();
		var statsMapping = StorageService.GetMapping<SignalStatistics>();

		var sql  = $"SELECT * FROM [{dayMapping.TableName}] WHERE [{dayMapping.ForeignKey.ColumnName}] = ? AND [{nameof( DailyReport.ReportDate )}] BETWEEN ? AND ?";
		
		var eventQuery = $@"SELECT [{eventMapping.TableName}].* FROM [{dayMapping.TableName}]
INNER JOIN [{eventMapping.TableName}] ON [{eventMapping.TableName}].{eventMapping.ForeignKey.ColumnName} = [{dayMapping.TableName}].{dayMapping.PrimaryKey.ColumnName}
WHERE [{dayMapping.TableName}].{dayMapping.ForeignKey.ColumnName} = ? AND [{dayMapping.TableName}].ReportDate BETWEEN ? AND ?;
";
		
		var statsQuery = $@"SELECT [{statsMapping.TableName}].* FROM [{dayMapping.TableName}]
INNER JOIN [{statsMapping.TableName}] ON [{statsMapping.TableName}].{statsMapping.ForeignKey.ColumnName} = [{dayMapping.TableName}].{dayMapping.PrimaryKey.ColumnName}
WHERE [{dayMapping.TableName}].{dayMapping.ForeignKey.ColumnName} = ? AND [{dayMapping.TableName}].ReportDate BETWEEN ? AND ?;
";
			
		var days   = store.Query<DailyReport>( sql, profileID, start, end );
		var events = store.Query<ReportedEvent>( eventQuery, profileID, start, end );
		var stats  = store.Query<SignalStatistics>( statsQuery, profileID, start, end );

		foreach( var day in days )
		{
			day.Events = events.Where( x => x.StartTime >= day.RecordingStartTime && x.StartTime <= day.RecordingEndTime ).ToList();
		}

		var viewModel = new HistoryViewModel()
		{
			Start = start,
			End   = end,
			Days  = days
		};

		return viewModel;
	}
}

