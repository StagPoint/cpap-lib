using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using cpaplib;

using example_viewer.ViewModels;

namespace example_viewer.Controls;

public partial class EventTree : UserControl
{
	public event TimeSelectedEventHandler OnTimeSelected;

	public EventTree()
	{
		InitializeComponent();

		tvwEvents.PreviewMouseWheel += HandlePreviewMouseWheel;
		tvwEvents.SelectedItemChanged += TvwEventsOnSelectedItemChanged;
	}
	
	private void TvwEventsOnSelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
	{
		var item = tvwEvents.SelectedItem;
		if( item is ReportedEvent evt )
		{
			OnTimeSelected?.Invoke( this, evt.StartTime );
		}
	}

	private void HandlePreviewMouseWheel( object sender, MouseWheelEventArgs e )
	{
		// We can't use the Treeview's built-in Scrollview, so we need to pass MouseWheel events to the 
		// Scrollview or other parent element which contains the Treeview and prevent the Treeview from
		// acting on the event directly. 
		if( !e.Handled )
		{
			e.Handled = true;
			
			var eventArg = new MouseWheelEventArgs( e.MouseDevice, e.Timestamp, e.Delta );
			eventArg.RoutedEvent = UIElement.MouseWheelEvent;
			eventArg.Source      = sender;
			
			var parent = ((Control)sender).Parent as UIElement;
			
			parent.RaiseEvent( eventArg );
		}
	}

	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DayRecord day )
			{
				tvwEvents.DataContext = new EventViewModel( day );

				Debug.WriteLine( $"Event Tree Loading {day.ReportDate.Date}" );
			}
		}
	}
}
