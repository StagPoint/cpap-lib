using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Views;

public partial class DailySessionsList : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<DateTimeRangeRoutedEventArgs> SessionSelectedEvent =
		RoutedEvent.Register<DailyReportView, DateTimeRangeRoutedEventArgs>( nameof( SessionSelected ), RoutingStrategies.Bubble );

	public static void AddSessionSelectedHandler( IInputElement element, EventHandler<DateTimeRangeRoutedEventArgs> handler )
	{
		element.AddHandler( SessionSelectedEvent, handler );
	}

	public event EventHandler<DateTimeRangeRoutedEventArgs> SessionSelected
	{
		add => AddHandler( SessionSelectedEvent, value );
		remove => RemoveHandler( SessionSelectedEvent, value );
	}

	#endregion 
	
	#region Constructor 
	
	public DailySessionsList()
	{
		InitializeComponent();
	}
	
	#endregion

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			// Filter the day's Sessions to only those that were produced by the CPAP machine
			lstSessions.ItemsSource = day.Sessions.Where( x => x.SourceType == SourceType.CPAP );
		}
	}

	private void lstSessions_Tapped( object? sender, TappedEventArgs e )
	{
		if( sender is Border { Tag: Session session } )
		{
			SelectSession( session );
		}
	}
	
	private async void ViewDetails_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReport day )
		{
			throw new InvalidOperationException( $"{nameof( DataContext )} does not contain a {nameof( DailyReport )} reference" );
		}
		
		if( e.Source is Control { Tag: Session session } )
		{
			// Focus the Session in the user interface
			SelectSession( session );
			
			var viewModel = new SessionDetailsViewModel( day, session );

			var statsView = new DailyStatisticsSummaryView()
			{
				DataContext = new DailyStatisticsViewModel( viewModel.Statistics ),
			};

			var dialog = new ContentDialog()
			{
				Title           = $"Session Details",
				Content         = statsView,
				CloseButtonText = "Done",
			};
		
			await dialog.ShowAsync();
		}
	}
	
	private void Delete_OnTapped( object? sender, RoutedEventArgs e )
	{
		//throw new NotImplementedException();
	}
	
	private void SelectSession( Session session )
	{
		var timeRangeEventArgs = new DateTimeRangeRoutedEventArgs
		{
			Route       = RoutingStrategies.Bubble,
			RoutedEvent = SessionSelectedEvent,
			StartTime   = session.StartTime,
			EndTime     = session.EndTime
		};

		RaiseEvent( timeRangeEventArgs );

		var signalEventArgs = new SignalSelectionArgs
		{
			RoutedEvent = SignalSelection.SignalSelectedEvent,
			Source      = this,
			SignalName  = SignalNames.FlowRate,
		};

		RaiseEvent( signalEventArgs );
	}
}

