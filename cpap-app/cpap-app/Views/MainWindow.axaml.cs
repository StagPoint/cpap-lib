using System.Diagnostics;

using Avalonia;
using Avalonia.Styling;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.ViewModels;

using FluentAvalonia.UI.Windowing;

namespace cpap_app.Views;

public partial class MainWindow : AppWindow
{
	public MainWindow()
	{
		InitializeComponent();

		var appSettings = ApplicationSettingsStore.GetSettings();

		var requestedTheme = GetThemeVariant( appSettings.Theme );
		if( requestedTheme != null )
		{
			Application.Current!.RequestedThemeVariant = requestedTheme;
		}

		UserProfileStore.UserProfileActivated += ( sender, profile ) =>
		{
			Title = $"CPAP Data Viewer - {profile.UserName}";
		};

		#if DEBUG
		this.AttachDevTools();
		#endif
	}
	
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
}
