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
				grdStatisticsSummary.DataContext = day.Statistics;
			}
		}
	}
}

