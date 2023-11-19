using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using cpap_app.ViewModels;

using OAuth;

namespace cpap_app.Views;

public partial class GoogleFitUserAuthorizationView : UserControl
{
	public event EventHandler<AccessTokenInfo>? AuthorizationSuccess;
	public event EventHandler<string>?          AuthorizationError;
		
	public GoogleFitUserAuthorizationView()
	{
		InitializeComponent();
	}
	
	private async void AuthorizeAccess_OnClick( object? sender, RoutedEventArgs e )
	{
		var clientConfig = AuthorizationConfigStore.GetConfig();
		if( !clientConfig.IsValid )
		{
			AuthorizationError?.Invoke( this, $"The client application configuration is invalidS" );
			return;
		}

		try
		{
			var accessTokenInfo = await AuthorizationClient.AuthorizeAsync( clientConfig );
			if( accessTokenInfo.IsValid )
			{
				AuthorizationSuccess?.Invoke( this, accessTokenInfo );
			}
		}
		catch( Exception exception )
		{
			AuthorizationError?.Invoke( this, exception.Message );
		}
	}
}

