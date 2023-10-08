using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySpO2View : UserControl
{
	#region Constructor 
	
	public DailySpO2View()
	{
		InitializeComponent();
	}
	
	#endregion
	
	#region Base class overrides 

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
				SetVisibility( pnlNoInfoAvailable, true );
				SetVisibility( pnlOximetryInfo,    false );
				return;
			case DailyReport day when day.Sessions.All( x => x.SourceType != SourceType.PulseOximetry ):
				DataContext = null;
				return;
			case DailyReport day:
				SetVisibility( pnlNoInfoAvailable, false );
				SetVisibility( pnlOximetryInfo,    true );

				var oximetrySessions = day.Sessions.Where( x => x.SourceType == SourceType.PulseOximetry ).ToArray();

				SourcesSummary.DataContext = new DailySummaryViewModel()
				{
					ReportDate         = day.ReportDate,
					RecordingStartTime = oximetrySessions.Select( x => x.StartTime ).Min(),
					RecordingEndTime   = oximetrySessions.Select( x => x.EndTime ).Max(),
					TotalSleepTime     = TimeSpan.FromSeconds( oximetrySessions.Sum( x => x.Duration.TotalSeconds ) ),
					Sources            = day.Sessions.Where( x => x.SourceType == SourceType.PulseOximetry ).Select( x => x.Source ).Distinct().ToList(),
				};
				
				OxygenEvents.DataContext = new DailyEventsViewModel( day, EventTypes.OxygenSaturation );
				PulseEvents.DataContext  = new DailyEventsViewModel( day, EventTypes.Pulse );
				
				OxygenSummary.DataContext = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.SpO2,  new[] { "> 95 %", "95 - 90 %", "< 90 %" },         new[] { 100, 96, 95, 90, 89, 0 } );
				PulseSummary.DataContext  = DataDistribution.GetDataDistribution( day.Sessions, SignalNames.Pulse, new[] { "> 110 bpm", "110 - 60 bpm", "< 60 bpm" }, new[] { 250, 111, 110, 60, 59, 0 } );
				break;
		}
	}
	
	#endregion 
	
	#region Private functions 

	private void SetVisibility( Control control, bool value )
	{
		if( control.IsVisible != value )
		{
			control.IsVisible = value;
		}
	}
	
	#endregion 
}


