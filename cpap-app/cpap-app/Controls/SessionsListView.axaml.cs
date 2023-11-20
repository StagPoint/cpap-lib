using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;
using cpap_app.Views;

using cpaplib;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Controls;

public partial class SessionsListView : UserControl
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
	
	#region Public properties 
	
	public static readonly StyledProperty<SourceType> SessionSourceTypeProperty = AvaloniaProperty.Register<SessionsListView, SourceType>( nameof( SessionSourceType ) );

	public SourceType SessionSourceType
	{
		get => GetValue( SessionSourceTypeProperty );
		set => SetValue( SessionSourceTypeProperty, value );
	}

	#endregion 
	
	#region Constructor 
	
	public SessionsListView()
	{
		InitializeComponent();
	}
	
	#endregion

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.NewValue is DailyReport day )
		{
			// Filter the day's Sessions to only those that match the specified SourceType
			lstSessions.ItemsSource = day.Sessions.Where( x => x.SourceType == SessionSourceType );
		}
	}

	private void lstSessions_Tapped( object? sender, TappedEventArgs e )
	{
		if( sender is Border { Tag: Session session } )
		{
			SelectSession( session );
		}
	}
	
	private void lstSessions_DoubleTapped( object? sender, TappedEventArgs e )
	{
		ViewDetails_OnTapped( sender, e );
	}

	private async void ViewDetails_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyReport day )
		{
			throw new InvalidOperationException( $"{nameof( DataContext )} does not contain a {nameof( DailyReport )} reference" );
		}
		
		if( SessionSourceType != SourceType.CPAP && SessionSourceType != SourceType.PulseOximetry )
		{
			return;
		}
			
		if( e.Source is Control { Tag: Session session } )
		{
			// Focus the Session in the user interface
			SelectSession( session );

			// Create the view that will show the Session details 
			var detailView = new SessionDetailsView()
			{
				DataContext = new SessionDetailsViewModel( day, session ),
			};

			// Because the dialog is contained in a separate Window, we need to pass along any events it generates 
			detailView.OnSignalSelected += ( o, args ) => RaiseEvent( args );
			
			// Actually, it turns out that we cannot raise some types of events, because they cause this control to unload and stop raising those events :/
			//detailView.OnEventTypeSelected += ( o, args ) => RaiseEvent( args ); 
			
			var dialog = new TaskDialog()
			{
				Title = $"Session Details",
				Buttons =
				{
					TaskDialogButton.OKButton,
				},
				XamlRoot = (Visual)VisualRoot!,
				Content = detailView,
				MaxWidth = 800,
			};
		
			await dialog.ShowAsync();
		}
	}
	
	private void Delete_OnTapped( object? sender, RoutedEventArgs e )
	{
		if( SessionSourceType != SourceType.CPAP && SessionSourceType != SourceType.PulseOximetry )
		{
			return;
		}
			
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
	
	private void ContextMenu_OnOpening( object? sender, EventArgs e )
	{
		if( sender is not MenuFlyout menu )
		{
			return;
		}
		
		if( SessionSourceType != SourceType.CPAP && SessionSourceType != SourceType.PulseOximetry )
		{
			menu.Hide();
			return;
		}
			
		throw new NotImplementedException();
	}
}

