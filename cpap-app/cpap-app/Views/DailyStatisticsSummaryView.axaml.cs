using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyStatisticsSummaryView : UserControl
{
	public DailyStatisticsSummaryView()
	{
		InitializeComponent();
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
	
	private void Row_OnTapped( object? sender, TappedEventArgs e )
	{
		if( e.Source is Control { Tag: SignalStatistics stats } )
		{
			RaiseEvent( new SignalSelectionArgs
			{
				RoutedEvent = SignalSelection.SignalSelectedEvent,
				Source      = this,
				SignalName  = stats.SignalName,
			} );
		}
	}
}

