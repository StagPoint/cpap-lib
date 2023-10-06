using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySpO2View : UserControl
{
	public DailySpO2View()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			OxygenSummary.DataContext = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.SpO2,  new[] { 100, 95, 94, 90, 89, 0 } );
			PulseSummary.DataContext  = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.Pulse, new[] { 250, 111, 110, 60, 59, 0 } );
		}
	}
}


