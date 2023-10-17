using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyStatisticsSummaryView : UserControl
{
	public DailyStatisticsSummaryView()
	{
		InitializeComponent();
	}
	
	private void VisibleColumnsOnPropertyChanged( object? sender, PropertyChangedEventArgs e )
	{
		throw new System.NotImplementedException();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is DailyReport day )
			{
				StatsGrid.DataContext = new DailyStatisticsViewModel( day );
			}
		}
	}
}

