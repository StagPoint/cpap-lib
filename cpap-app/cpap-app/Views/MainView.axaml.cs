using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Views;

public partial class MainView : UserControl
{
	public MainView()
	{
		InitializeComponent();

		NavView.Content = new HomeView() { DataContext = new HomeViewModel( DateTime.Today ) };
	}

	private void NavView_OnSelectionChanged( object? sender, NavigationViewSelectionChangedEventArgs e )
	{
		if( sender is not NavigationView navView )
		{
			return;
		}

		if( e.IsSettingsSelected )
		{
			navView.Content = new AppSettingsView();
		}
		else if( e.SelectedItem is NavigationViewItem navViewItem )
		{
			if( string.IsNullOrEmpty( navViewItem.Tag as string ) )
			{
				return;
			}

			var typeName = $"{typeof( MainView ).Namespace}.{navViewItem.Tag}View";
			var pageType = Type.GetType( typeName );

			if( pageType == null )
			{
				throw new Exception( $"Unhandled page type: {navView.Tag}" );
			}

			var page = Activator.CreateInstance( pageType );
			navView.Content         = page;
			navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
		}
	}

	private void Exit_OnClick( object? sender, RoutedEventArgs e )
	{
		if( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp )
		{
			desktopApp.Shutdown();
		}
		else if( Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime viewApp )
		{
			viewApp.MainView = null;
		}
	}
	
	private void Import_SpO2( object? sender, TappedEventArgs e )
	{
		throw new NotImplementedException();
	}
	
	private void Import_CPAP( object? sender, TappedEventArgs e )
	{
		throw new NotImplementedException();
	}
}
