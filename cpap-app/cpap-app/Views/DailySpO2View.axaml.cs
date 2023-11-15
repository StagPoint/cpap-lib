using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySpO2View : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<DateTimeRoutedEventArgs> DeletionRequestedEvent = 
		RoutedEvent.Register<DailySpO2View, DateTimeRoutedEventArgs>( nameof( DeletionRequested ), RoutingStrategies.Bubble );

	public event EventHandler<DateTimeRoutedEventArgs> DeletionRequested
	{
		add => AddHandler( DeletionRequestedEvent, value );
		remove => RemoveHandler( DeletionRequestedEvent, value );
	}
		
	#endregion 
	
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
				SetVisibility( SourcesSummary,     false );
				break;
			case DailyReport day when day.Sessions.All( x => x.SourceType != SourceType.PulseOximetry ):
				DataContext = null;
				break;
			case DailyReport day:
				SetVisibility( pnlNoInfoAvailable, false );
				SetVisibility( pnlOximetryInfo,    true );
				SetVisibility( SourcesSummary,     true );

				LoadDay( day );
				break;
		}
	}
	
	private void LoadDay( DailyReport day )
	{
		var oximetrySessions = day.Sessions.Where( x => x.SourceType == SourceType.PulseOximetry ).ToArray();

		SourcesSummary.DataContext = new DailySummaryViewModel()
		{
			ReportDate         = day.ReportDate,
			RecordingStartTime = oximetrySessions.Select( x => x.StartTime ).Min(),
			RecordingEndTime   = oximetrySessions.Select( x => x.EndTime ).Max(),
			TotalSleepTime     = TimeSpan.FromSeconds( oximetrySessions.Sum( x => x.Duration.TotalSeconds ) ),
			Sources            = day.Sessions.Where( x => x.SourceType == SourceType.PulseOximetry ).Select( x => x.Source ).Distinct().ToList(),
		};

		OxygenEvents.DataContext = new EventSummaryViewModel( day, EventTypes.OxygenSaturation );
		PulseEvents.DataContext  = new EventSummaryViewModel( day, EventTypes.Pulse );

		OxygenSummary.DataContext = DataDistribution.GetDataDistribution(
			day.Sessions,
			SignalNames.SpO2,
			new[]
			{
				new DataDistribution.RangeDefinition( "< 90 %",  89.5 ),
				new DataDistribution.RangeDefinition( "90% - 94%", 94.5 ),
				new DataDistribution.RangeDefinition( "\u2265 95 %", double.MaxValue )
			} );

		PulseSummary.DataContext = DataDistribution.GetDataDistribution(
			day.Sessions,
			SignalNames.Pulse,
			new[]
			{
				new DataDistribution.RangeDefinition( "< 50 bpm",    49.5 ),
				new DataDistribution.RangeDefinition( "50-99 bpm",   99.5 ),
				new DataDistribution.RangeDefinition( "100-120 bpm", 120.5 ),
				new DataDistribution.RangeDefinition( "\u2265 120 bpm",  double.MaxValue )
			}
		);
	}

	#endregion 
	
	#region Event handlers 
	
	private void DeleteOximetryData_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReport day )
		{
			throw new Exception( $"Cannot request deletion of data for {DataContext}" );
		}
		
		RaiseEvent( new DateTimeRoutedEventArgs
		{
			RoutedEvent = DeletionRequestedEvent,
			Source      = this,
			DateTime    = day.ReportDate.Date
		} );
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


