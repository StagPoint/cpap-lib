using System;
using System.IO;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;
using cpap_app.Views;

using cpap_db;

namespace cpap_app;

using Path = System.IO.Path;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load( this );

		StorageService.InitializeDatabase( StorageService.GetApplicationDatabasePath() );
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainViewModel()
			};
		}
		else if( ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform )
		{
			singleViewPlatform.MainView = new MainView
			{
				DataContext = new MainViewModel()
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
