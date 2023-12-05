using System.Collections.Generic;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class StatisticsView : UserControl
{
	public StatisticsView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DataContext = BuildStatisticsViewModel();
	}

	private TherapyStatisticsViewModel BuildStatisticsViewModel()
	{
		var list = new List<TherapyStatisticsItemViewModel>();

		for( int i = 0; i < 25; i++ )
		{
			list.Add( new TherapyStatisticsItemViewModel
			{
				Name                 = $"This is item {i}",
				MostRecentValue      = i + 0,
				LastWeekAverage      = i + 1,
				LastMonthAverage     = i + 2,
				LastNinetyDayAverage = i + 3,
				LastYearAverage      = i + 4
			} );
		}
		
		var viewModel = new TherapyStatisticsViewModel()
		{
			Items = list,
		};

		return viewModel;
	}
}
