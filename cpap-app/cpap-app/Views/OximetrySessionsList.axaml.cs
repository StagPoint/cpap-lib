using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cpap_app.Views;

public partial class OximetrySessionsList : UserControl
{
	public OximetrySessionsList()
	{
		InitializeComponent();
	}
	
	private void SelectingItemsControl_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		lstSessions.SelectedItem = null;
	}
}

