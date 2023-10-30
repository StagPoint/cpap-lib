using Avalonia;
using Avalonia.Styling;

using cpap_app.Configuration;

using cpap_db;

namespace cpap_app.ViewModels;

public class ApplicationSettingsStore
{
	static ApplicationSettingsStore()
	{
		using var store = StorageService.Connect();
		
		// TODO: Move initialization to application startup
		Initialize( store );
	}
	
	public static ApplicationSettings GetSettings()
	{
		using var store = StorageService.Connect();
		
		return store.SelectById<ApplicationSettings>( 0 );
	}

	public static bool SaveSettings( ApplicationSettings settings )
	{
		using var store = StorageService.Connect();
		return store.Update( settings, 0 );
	}
	
	private static void Initialize( StorageService store )
	{
		var mapping = StorageService.CreateMapping<ApplicationSettings>( "app_settings" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );

		store.CreateTable<ApplicationSettings>();

		var records = store.SelectAll<ApplicationSettings>();
		if( records.Count > 0 )
		{
			return;
		}

		var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		
		var defaultSettings = new ApplicationSettings()
		{
			Theme = isDarkTheme ? ApplicationThemeType.Dark : ApplicationThemeType.Light
		};
		
		store.Insert( defaultSettings, primaryKeyValue: 0 );
	}
}
