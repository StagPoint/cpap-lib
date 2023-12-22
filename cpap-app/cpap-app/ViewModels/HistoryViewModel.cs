using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class HistoryViewModel : ViewModelBase
{
	#region Public properties 
	
	public DateTime Start { get; set; } = DateTime.Today.AddDays( -90 );
	public DateTime End   { get; set; } = DateTime.Today;

	public List<DailyReport> Days = new List<DailyReport>();

	public int TotalDays
	{
		get => (int)Math.Floor( (End.Date - Start.Date).TotalDays + 1.0 );
	}

	#endregion 
	
	#region Public functions 
	
	public static HistoryViewModel GetHistory( int profileID, DateTime start, DateTime end )
	{
		using var store = StorageService.Connect();

		var days = store.LoadDailyReportsForRange( profileID, start, end, false );

		// Trim the start and end dates to match the actual days retrieved 
		var viewModel = new HistoryViewModel()
		{
			Start = days.Count > 0 ? DateHelper.Max( start, days[ 0 ].ReportDate.Date ) : start,
			End   = days.Count > 0 ? days[ ^1 ].ReportDate.Date : end,
			Days  = days
		};

		return viewModel;
	}

	#endregion 
}
