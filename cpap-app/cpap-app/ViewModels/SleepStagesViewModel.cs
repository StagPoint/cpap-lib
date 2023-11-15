using System;
using System.Collections.Generic;

namespace cpap_app.ViewModels;

public class SleepStagesViewModel
{
	public List<SleepStageSummaryItemViewModel> StageSummaries { get; set; } = new List<SleepStageSummaryItemViewModel>();

	public SleepStagesViewModel()
	{
		StageSummaries = new List<SleepStageSummaryItemViewModel>
		{
			new SleepStageSummaryItemViewModel() { Label = "Awake", Percentage = 14, TimeInStage = TimeSpan.FromMinutes( 53 ) },	
			new SleepStageSummaryItemViewModel() { Label = "Rem", Percentage = 16, TimeInStage = TimeSpan.FromMinutes( 59 ) },	
			new SleepStageSummaryItemViewModel() { Label = "Light", Percentage = 61, TimeInStage = new TimeSpan( 3, 50, 0 ) },	
			new SleepStageSummaryItemViewModel() { Label = "Deep", Percentage = 9, TimeInStage = TimeSpan.FromMinutes( 32 ) },	
		};
	}
}

public class SleepStageSummaryItemViewModel
{
	public string   Label       { get; set; } = "Light";
	public TimeSpan TimeInStage { get; set; } = TimeSpan.Zero;
	public double   Percentage  { get; set; } = 0;
}
