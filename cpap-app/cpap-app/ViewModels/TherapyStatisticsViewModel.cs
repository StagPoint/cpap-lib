using System;
using System.Collections.Generic;

using Avalonia.Controls;

using cpaplib;

namespace cpap_app.ViewModels;

public class GroupedDays
{
	public required string            Label     { get; set; }
	public          string            DateLabel { get; set; } = string.Empty;
	public          DateTime          StartDate { get; set; }
	public          DateTime          EndDate   { get; set; }
	public          List<DailyReport> Days      { get; set; } = new List<DailyReport>();
	
	public bool IsSingleDate { get => StartDate.Date == EndDate.Date; }

	public override string ToString()
	{
		return $"{Label} - {Days.Count} days";
	}
}

public class TherapyStatisticsViewModel
{
	public List<GroupedDays>                       Headers  { get; set; } = new();
	public List<TherapyStatisticsSectionViewModel> Sections { get; set; } = new();
}

public class TherapyStatisticsSectionViewModel
{
	public required string                                Label   { get; set; }
	public          List<TherapyStatisticsGroupViewModel> Groups  { get; set; } = new();
}

public class TherapyStatisticsGroupViewModel
{
	public required string                                   Label { get; set; }
	public          List<TherapyStatisticsLineItemViewModel> Items { get; set; } = new();
}

public class TherapyStatisticsLineItemViewModel
{
	public required string       Label  { get; set; }
	public          List<string> Values { get; set; } = new();
}

