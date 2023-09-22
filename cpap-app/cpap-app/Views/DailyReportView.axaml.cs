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
		TabFrame.Content = new DailyDetailsView();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		using( var store = StorageService.Connect() )
		{
			var latestDate = store.GetMostRecentDay();
			DateSelector.SelectedDate = latestDate;
		}
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

		if( page is StyledElement view )
		{
			view.DataContext = this.DataContext;
		}
	}
	
	private void DateSelector_OnSelectedDateChanged( object? sender, SelectionChangedEventArgs e )
	{
		using( var store = StorageService.Connect() )
		{
			// TODO: Implement visual indication of "no data available" to match previous viewer codebase
			var day = store.LoadDailyReport( DateSelector.SelectedDate ?? store.GetMostRecentDay() );

			DataContext = day;
			
			// I don't know why setting DataContext doesn't cascade down in Avalonia like it did in WPF, 
			// but apparently I need to handle that manually.
			if( TabFrame.Content is StyledElement uc )
			{
				uc.DataContext = day;
			}
		}
	}
	private void BtnLastDay_OnClick( object? sender, RoutedEventArgs e )
	{
		DateSelector.SelectedDate = DateTime.Today.AddDays( -1 );
	}
}

