using System.Linq;

using Avalonia;
using Avalonia.Controls;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyEventSummaryView : UserControl
{
	public DailyEventSummaryView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name != nameof( DataContext ) )
		{
			return;
		}

		switch( change.NewValue )
		{
			case null:
				Events.DataContext = null;
				return;
			case DailyReport day:
			{
				var viewModel = new EventSummaryViewModel( day );
				viewModel.Indexes.Add( new EventGroupSummary( "Apnea/Hypopnea Index (AHI)", EventTypes.Apneas, day.TotalSleepTime, day.Events ) );

				if( day.Events.Any( x => EventTypes.RespiratoryDisturbancesOnly.Contains( x.Type ) ) )
				{
					viewModel.Indexes.Add( new EventGroupSummary( "Respiratory Disturbance (RDI)", EventTypes.RespiratoryDisturbance, day.TotalSleepTime, day.Events ) );
				}

				Events.DataContext = viewModel;
				break;
			}
		}
	}
}

