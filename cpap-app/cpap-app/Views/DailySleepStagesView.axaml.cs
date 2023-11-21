using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.XPath;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySleepStagesView : UserControl
{
	private SleepStagesViewModel? _sleepStages;
	
	public DailySleepStagesView()
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

		if( change.NewValue is DailyReport day )
		{
			_sleepStages = new SleepStagesViewModel( day );

			var events           = GetRemEvents( day );
			var remEventsSummary = new EventSummaryViewModel( day, events );
			if( remEventsSummary.TotalCount > 0 )
			{
				remEventsSummary.AddGroupSummary( "Respiratory Disturbance (RDI)", EventTypes.RespiratoryDisturbance, events );
			}
			
			RemEvents.DataContext = remEventsSummary;

			NoRemEventsFound.IsVisible = (remEventsSummary.TotalCount == 0);
		}

		OuterContainer.DataContext = _sleepStages;
	}

	private List<ReportedEvent> GetRemEvents( DailyReport day )
	{
		var results = new List<ReportedEvent>();
		var signals = new List<Signal>();

		// First find all of the Sleep Stage signals, which will be used to determine whether an 
		// event happened during REM sleep or not
		foreach( var session in day.Sessions )
		{
			var signal = session.GetSignalByName( SignalNames.SleepStages );
			if( signal != null )
			{
				signals.Add( signal );
			}
		}

		foreach( var ev in day.Events )
		{
			if( EventTypes.RespiratoryDisturbance.Contains( ev.Type ) )
			{
				foreach( var signal in signals )
				{
					if( signal.StartTime <= ev.StartTime && signal.EndTime >= ev.StartTime )
					{
						var value = (SleepStage)Math.Round( signal.GetValueAtTime( ev.StartTime, false ) );
						if( value == SleepStage.REM )
						{
							results.Add( ev );
						}

						break;
					}
				}
			}
		}

		return results;
	}
	
	private void DeleteData_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _sleepStages != null, nameof( _sleepStages ) + " != null" );
		
		RaiseEvent( new SessionDataEventArgs
		{
			RoutedEvent = SessionDataEvents.SessionDeletionRequestedEvent,
			Source      = this,
			Date        = _sleepStages.Day.ReportDate.Date,
			SourceType  = SourceType.HealthAPI
		} );
	}
}

