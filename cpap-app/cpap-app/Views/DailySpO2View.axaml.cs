using System.Linq;

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

		switch( change.NewValue )
		{
			case null:
				SetVisibility( pnlNoInfoAvailable, true );
				SetVisibility( pnlOximetryInfo,    false );
				return;
			case DailyReport day when day.Sessions.All( x => x.Type != SessionType.PulseOximetry ):
				DataContext = null;
				return;
			case DailyReport day:
				SetVisibility( pnlNoInfoAvailable, false );
				SetVisibility( pnlOximetryInfo,    true );
			
				OxygenEvents.DataContext = new DailyEventsViewModel( day, EventTypes.OxygenSaturation );
				PulseEvents.DataContext  = new DailyEventsViewModel( day, EventTypes.Pulse );
			
				OxygenSummary.DataContext = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.SpO2,  new[] { "> 95 %", "95 - 90 %", "< 90 %" },         new[] { 100, 96, 95, 90, 89, 0 } );
				PulseSummary.DataContext  = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.Pulse, new[] { "> 110 bpm", "110 - 60 bpm", "< 60 bpm" }, new[] { 250, 111, 110, 60, 59, 0 } );
				break;
		}
	}

	private void SetVisibility( Control control, bool value )
	{
		if( control.IsVisible != value )
		{
			control.IsVisible = value;
		}
	}
}


