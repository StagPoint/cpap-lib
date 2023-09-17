using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using cpaplib;

using cpapviewer.Helpers;

namespace cpapviewer.Controls;

public partial class SessionList : UserControl
{
	public event TimeRangeSelectedEventHandler OnTimeSelected;

	public SessionList()
	{
		InitializeComponent();
		lvwSessions.SelectionChanged += LvwSessionsOnSelectionChanged;
	}
	
	private void LvwSessionsOnSelectionChanged( object sender, SelectionChangedEventArgs e )
	{
		var item = lvwSessions.SelectedItem;
		if( item is Session session )
		{
			lvwSessions.SelectedIndex = -1;
			OnTimeSelected?.Invoke( this, session.StartTime, session.EndTime );
		}
	}

	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DailyReport day )
			{
				// In cases where the data source has changed and data binding is being refreshed, 
				// it is necessary to clear the ItemsSource first to avoid a runtime exception 
				// caused by the underlying Session list changing. 
				lvwSessions.ItemsSource = null;
				lvwSessions.ItemsSource = day.Sessions;

				CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView( lvwSessions.ItemsSource );
				view.GroupDescriptions.Clear();

				if( day.Sessions.Select( x => x.Source ).Distinct().Count() > 1 )
				{
					PropertyGroupDescription groupDescription = new PropertyGroupDescription( nameof( Session.Source ) );
					view.GroupDescriptions.Add( groupDescription );
				}
			}
		}
	}
}

