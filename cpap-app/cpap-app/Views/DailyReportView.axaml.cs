using System;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_db;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Views;

public partial class DailyReportView : UserControl
{
	public DailyReportView()
	{
		InitializeComponent();

		using( var store = StorageService.Connect() )
		{
			var latestDate = store.GetMostRecentDay();
			DateSelector.SelectedDate = latestDate;
		}

		TabFrame.Content = new DailyDetailsView();
	}

	private void DetailTypes_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( sender is not TabStrip tabs )
		{ 
			return;
		}

		if( tabs.SelectedItem is not TabItem selectedItem )
		{
			return;
		}

		if( string.IsNullOrEmpty( selectedItem.Tag as string ) )
		{
			return;
		}
	
		var typeName = $"{typeof( MainView ).Namespace}.{selectedItem.Tag}View";
		var pageType = Type.GetType( typeName );
	
		if( pageType == null )
		{
			throw new Exception( $"Unhandled page type: {selectedItem.Tag}" );
		}
	
		var page = Activator.CreateInstance( pageType );
		TabFrame.Content = page;
	}
}

