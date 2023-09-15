using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using cpaplib;

using example_viewer.ViewModels;

namespace example_viewer.Controls;

public partial class EventSummary : UserControl
{
	public EventSummary()
	{
		InitializeComponent();
	}
	
	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DayRecord day )
			{
				gridEventSummary.DataContext = day.EventSummary;
			}
			else if( DataContext is EventSummary summary )
			{
				gridEventSummary.DataContext = summary;
			}
		}
	}
}

