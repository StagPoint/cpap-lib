using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Styling;

using cpap_app.Configuration;

namespace cpap_app.ViewModels;

public class AppSettingsViewModel : INotifyPropertyChanged
{
	#region Public properties 
	
	public ApplicationThemeType CurrentAppTheme
	{
		get => _currentAppTheme;
		set
		{
			_settings.Theme = value;
			
			if( RaiseAndSetIfChanged( ref _currentAppTheme, value ) )
			{
				var newTheme = GetThemeVariant( value );
				if( newTheme != null )
				{
					Application.Current!.RequestedThemeVariant = newTheme;
					
					// TODO: This is dumb. Refactor how application settings are loaded, edited, and saved. 
					ApplicationSettingsStore.SaveSettings( _settings );
				}
			}
		}
	}

	#endregion 
	
    #region Private fields

	private ApplicationSettings  _settings;
	private ApplicationThemeType _currentAppTheme;

	public ApplicationThemeType[] AppThemes { get; } = { ApplicationThemeType.System, ApplicationThemeType.Light, ApplicationThemeType.Dark };

    #endregion
	
	#region Constructor

	public AppSettingsViewModel()
	{
		_settings        = ApplicationSettingsStore.GetSettings();
		_currentAppTheme = _settings.Theme;
	}
	
	#endregion 

	#region Private functions 

	private static ThemeVariant? GetThemeVariant( ApplicationThemeType value )
	{
		return value switch
		{
			ApplicationThemeType.Light  => ThemeVariant.Light,
			ApplicationThemeType.Dark   => ThemeVariant.Dark,
			ApplicationThemeType.System => ThemeVariant.Default,
			_                           => null
		};
	}
	
	#endregion 
	
	#region INotifyPropertyChanged interface implementation

	public event PropertyChangedEventHandler? PropertyChanged;

	protected bool RaiseAndSetIfChanged<T>( ref T field, T value, [CallerMemberName] string propertyName = "" )
	{
		if( !EqualityComparer<T>.Default.Equals( field, value ) )
		{
			field = value;
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
			return true;
		}
		return false;
	}

	protected void RaisePropertyChanged( string propName )
	{
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propName ) );
	}

    #endregion
}
