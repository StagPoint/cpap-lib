using System;

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
	
	private static HistoryViewModel BuildDataContext()
	{
		using var store = StorageService.Connect();

		var start = DateTime.Today.AddDays( -180 );
		var end   = DateTime.Today;

		var profileID  = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dayMapping = StorageService.GetMapping<DailyReport>();

		var dayQuery  = $@"
			SELECT * 
			FROM [{dayMapping.TableName}] 
			WHERE [{dayMapping.ForeignKey.ColumnName}] = ? AND [{nameof( DailyReport.ReportDate )}] BETWEEN ? AND ? 
			ORDER BY [{dayMapping.TableName}].[{dayMapping.PrimaryKey.ColumnName}]";
		
		// Only load the part of the DailyReports that is going to be relevant to the consumer (skipping Signal data, for instance)
		var days = store.Query<DailyReport>( dayQuery, profileID, start, end );
		foreach( var day in days )
		{
			day.Events     = store.SelectByForeignKey<ReportedEvent>( day.ID );
			day.Statistics = store.SelectByForeignKey<SignalStatistics>( day.ID );
			day.Sessions   = store.SelectByForeignKey<Session>( day.ID );

			day.Events.Sort();
			day.Sessions.Sort();
		}

		days.Sort();

		var viewModel = new HistoryViewModel()
		{
			Start = start,
			End   = end,
			Days  = days
		};

		return viewModel;
	}
}

