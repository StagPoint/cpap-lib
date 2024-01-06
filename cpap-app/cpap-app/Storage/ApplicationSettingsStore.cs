using System;
using System.Linq;

using Avalonia;
using Avalonia.Styling;

using cpap_app.Configuration;

using cpap_db;

namespace cpap_app.ViewModels;

public static class ApplicationSettingNames
{
	public static readonly string PrintExportPath = "Print Export Path";
}

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

	public static double GetNumericSetting( string key, double defaultValue )
	{
		using var store = StorageService.Connect();

		var existingSetting = store.SelectAll<StoredNumericSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) );
		if( existingSetting != null )
		{
			return existingSetting.Value;
		}

		SaveNumericSetting( key, defaultValue );

		return defaultValue;
	}

	public static double? GetNumericSetting( string key )
	{
		using var store = StorageService.Connect();

		return store.SelectAll<StoredNumericSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) )?.Value;
	}

	public static void SaveNumericSetting( string key, double value )
	{
		using var store = StorageService.Connect();

		var existingSetting = store.SelectAll<StoredNumericSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) );
		if( existingSetting != null )
		{
			existingSetting.Value = value;
			store.Update( existingSetting );

			return;
		}

		var newSetting = new StoredNumericSetting()
		{
			Key   = key,
			Value = value,
		};

		store.Insert( newSetting );
	}

	public static string? GetStringSetting( string key )
	{
		using var store = StorageService.Connect();

		return store.SelectAll<StoredStringSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) )?.Value;
	}

	public static string GetStringSetting( string key, string defaultValue )
	{
		using var store = StorageService.Connect();

		var existingSetting = store.SelectAll<StoredStringSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) );
		if( existingSetting != null )
		{
			return existingSetting.Value;
		}

		SaveStringSetting( key, defaultValue );

		return defaultValue;
	}

	public static void SaveStringSetting( string key, string value )
	{
		using var store = StorageService.Connect();

		var existingSetting = store.SelectAll<StoredStringSetting>().FirstOrDefault( x => string.Equals( x.Key, key, StringComparison.OrdinalIgnoreCase ) );
		if( existingSetting != null )
		{
			existingSetting.Value = value;
			store.Update( existingSetting );

			return;
		}

		var newSetting = new StoredStringSetting()
		{
			Key   = key,
			Value = value,
		};

		store.Insert( newSetting );
	}

	public static bool SaveSettings( ApplicationSettings settings )
	{
		using var store = StorageService.Connect();
		return store.Update( settings, 0 );
	}
	
	private static void Initialize( StorageService store )
	{
		InitializeApplicationSettingsTable( store );
		InitializeNumericKeyValuePairs( store );
		InitializeStringKeyValuePairs( store );
	}
	
	private static void InitializeStringKeyValuePairs( StorageService store )
	{
		StorageService.CreateMapping<StoredStringSetting>( "string_settings" );

		store.CreateTable<StoredStringSetting>();
	}

	private static void InitializeNumericKeyValuePairs( StorageService store )
	{
		StorageService.CreateMapping<StoredNumericSetting>( "numeric_settings" );

		store.CreateTable<StoredNumericSetting>();
	}

	private static void InitializeApplicationSettingsTable( StorageService store )
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
