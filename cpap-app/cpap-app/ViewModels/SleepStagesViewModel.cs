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
			new SleepStageSummaryItemViewModel() { Label = "Awake", Percentage = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 53 ) },
			new SleepStageSummaryItemViewModel() { Label = "Rem", Percentage   = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 59 ) },
			new SleepStageSummaryItemViewModel() { Label = "Light", Percentage = Random.Shared.NextInt64( 99 ), TimeInStage = new TimeSpan( 3, 50, 0 ) },
			new SleepStageSummaryItemViewModel() { Label = "Deep", Percentage  = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 32 ) },
		};
	}
}

public enum SleepStage
{
	Awake = 0,
	Rem   = 1,
	Light = 2,
	Deep  = 3,
}

public class SleepStagePeriodViewModel
{
	public event EventHandler<bool>? ValidationStatusChanged;

	public SleepStage Stage     { get; set; } = SleepStage.Awake;
	public DateTime   StartDate { get; set; } = DateTime.Today.AddDays( -1 );
	public DateTime   EndDate   { get; set; } = DateTime.Today;
	public DateTime   StartTime { get; set; } = DateTime.Now.AddHours( -9 );
	public DateTime   EndTime   { get; set; } = DateTime.Now;

	public List<string> ValidationErrors = new();
	
	public void SetValidationStatus( bool isValid )
	{
		ValidationStatusChanged?.Invoke( null, isValid );
	}
}

public class SleepStageSummaryItemViewModel
{
	public string   Label       { get; set; } = "Light";
	public TimeSpan TimeInStage { get; set; } = TimeSpan.Zero;
	public double   Percentage  { get; set; } = 0;
}
