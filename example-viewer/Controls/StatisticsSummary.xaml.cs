using System.Windows;
using System.Windows.Controls;

using cpaplib;

namespace example_viewer.Controls;

public partial class StatisticsSummary : UserControl
{
	public StatisticsSummary()
	{
		InitializeComponent();
	}
	
	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DailyReport day )
			{
				// In cases where the data source has changed and data binding is being refreshed, 
				// it is necessary to clear the ItemsSource first to avoid a runtime exception 
				// caused by the underlying Session list changing. 
				lvwStatistics.ItemsSource = null;
				lvwStatistics.Items.Clear();
				
				lvwStatistics.ItemsSource = day.Statistics;
			}
		}
	}
}

