using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using cpap_app.Views;

using cpap_db;

using QuestPDF.Infrastructure;

namespace cpap_app;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load( this );

		StorageService.InitializeDatabase( StorageService.GetApplicationDatabasePath() );
		QuestPDF.Settings.License = LicenseType.Community;
	}

	public override void OnFrameworkInitializationCompleted()
	{
		switch( ApplicationLifetime )
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
				desktop.MainWindow = new MainWindow
				{
					DataContext = null
				};
				break;
			case ISingleViewApplicationLifetime singleViewPlatform:
				singleViewPlatform.MainView = new MainView
				{
					DataContext = null
				};
				break;
		}

		base.OnFrameworkInitializationCompleted();
	}
}
