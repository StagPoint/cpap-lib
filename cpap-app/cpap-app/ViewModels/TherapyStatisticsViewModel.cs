using System;
using System.Collections.Generic;

using Avalonia.Controls;

namespace cpap_app.ViewModels;

public class TherapyStatisticsViewModel
{
	public List<TherapyStatisticsItemViewModel> Items { get; set; } = new();

	public DateTime MostRecentDate      { get; set; } = DateTime.Today;
	public DateTime LastWeekStart       { get; set; } = DateTime.Today.AddDays( -7 );
	public DateTime LastMonthStart      { get; set; } = DateTime.Today.AddMonths( -1 );
	public DateTime LastNinetyDaysStart { get; set; } = DateTime.Today.AddMonths( -3 );
	public DateTime LastYearStart       { get; set; } = DateTime.Today.AddYears( -1 );
}

public class TherapyStatisticsItemViewModel
{
	public string Name                 { get; set; } = string.Empty;
	public double MostRecentValue      { get; set; }
	public double LastWeekAverage      { get; set; }
	public double LastMonthAverage     { get; set; }
	public double LastNinetyDayAverage { get; set; }
	public double LastYearAverage      { get; set; }
}
