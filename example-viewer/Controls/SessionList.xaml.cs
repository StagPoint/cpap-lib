using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using cpaplib;

using example_viewer.Helpers;

namespace example_viewer.Controls;

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
		if( item is MaskSession session )
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
				lvwSessions.DataContext = day.Sessions;
				lvwSessions.ItemsSource = day.Sessions;
			}
		}
	}
}

