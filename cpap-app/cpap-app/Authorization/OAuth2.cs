using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

// ReSharper disable CommentTypo

namespace OAuth
{
	public class AccessTokenInfo
	{
		public string   AccessToken           { get; set; } = string.Empty;
		public DateTime AccessTokenExpiration { get; set; } = DateTime.MinValue;
		public string   RefreshToken          { get; set; } = string.Empty;

		public bool AccessTokenIsValid
		{
			get => !string.IsNullOrEmpty( AccessToken ) && DateTime.Now <= AccessTokenExpiration.AddMinutes( -15 );
		}

		public bool IsValid
		{
			get
			{
				return AccessTokenIsValid || !string.IsNullOrEmpty( RefreshToken );
			}
		}
	}

	/// <summary>
	/// To obtain the ClientID and ClientSecret field values, you must have a project on the Google API
	/// site with the Fitness API enabled.
	///		Go to https://developers.google.com/fit/rest/v1/get-started for information on getting started.
	///		Enable Fitness API access here: https://console.cloud.google.com/flows/enableapi?apiid=fitness
	///		Google API Console: https://console.developers.google.com/
	/// </summary>
	public class AuthorizationConfig
	{
		#region Public properties

		public string AuthorizationEndpoint { get; } = "https://accounts.google.com/o/oauth2/v2/auth";
		public string TokenRequestUri       { get; } = "https://www.googleapis.com/oauth2/v4/token";
		public string RedirectUri           { get; init; }

		public string ClientID     { get; init; }
		public string ClientSecret { get; init; }

		public bool IsValid
		{
			get => !string.IsNullOrEmpty( RedirectUri ) && !string.IsNullOrEmpty( ClientID ) && !string.IsNullOrEmpty( ClientSecret );
		}

		#endregion

		#region Constructor

		public AuthorizationConfig()
			: this( string.Empty, string.Empty, 3264 )
		{
		}

		public AuthorizationConfig( string clientID, string clientSecret, int? port )
		{
			RedirectUri = $"http://{IPAddress.Loopback}:{port ?? GetRandomUnusedPort()}/";

			ClientID     = clientID;
			ClientSecret = clientSecret;
		}

		#endregion

		#region Private functions

		public static int GetRandomUnusedPort()
		{
			// ref http://stackoverflow.com/a/3978040

			var listener = new TcpListener( IPAddress.Loopback, 0 );
			listener.Start();

			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();

			return port;
		}

		#endregion
	}

	public class AuthorizationClient
	{
		#region Public functions

		public static async Task<AccessTokenInfo> AuthorizeAsync( AuthorizationConfig config )
		{
			AccessTokenResponse response = await AuthorizeAndReturnAccessToken( config );

			if( response.AccessToken == null || response.RefreshToken == null )
			{
				throw new Exception( "Malformed authorization information returned from server" );
			}

			return new AccessTokenInfo
			{
				AccessToken           = response.AccessToken,
				AccessTokenExpiration = DateTime.Now.AddSeconds( response.ExpiresIn ?? 0 ),
				RefreshToken          = response.RefreshToken
			};
		}

		public static async Task<AccessTokenInfo> RefreshAuthorizationTokenAsync( AuthorizationConfig config, string refreshToken )
		{
			AccessTokenResponse response = await RefreshAuthorizationToken( config, refreshToken );

			if( response.AccessToken == null )
			{
				throw new Exception( "Malformed authorization information returned from server" );
			}

			return new AccessTokenInfo
			{
				AccessToken           = response.AccessToken,
				AccessTokenExpiration = DateTime.Now.AddSeconds( response.ExpiresIn ?? 0 ),
				RefreshToken          = refreshToken
			};
		}

		#endregion

		#region Private functions

		private static async Task<AccessTokenResponse> RefreshAuthorizationToken( AuthorizationConfig config, string refreshToken )
		{
			string tokenRequestUri  = config.TokenRequestUri;
			string tokenRequestBody = $"client_id={config.ClientID}&client_secret={config.ClientSecret}&refresh_token={refreshToken}&grant_type=refresh_token";

			HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create( tokenRequestUri );
			tokenRequest.Method      = "POST";
			tokenRequest.ContentType = "application/x-www-form-urlencoded";

			byte[] tokenRequestBodyBytes = Encoding.ASCII.GetBytes( tokenRequestBody );
			tokenRequest.ContentLength = tokenRequestBodyBytes.Length;

			await using( Stream requestStream = tokenRequest.GetRequestStream() )
			{
				await requestStream.WriteAsync( tokenRequestBodyBytes, 0, tokenRequestBodyBytes.Length );
			}

			try
			{
				var tokenResponse       = await tokenRequest.GetResponseAsync();
				var tokenResponseStream = tokenResponse.GetResponseStream();

				if( tokenResponseStream == null )
				{
					throw new NullReferenceException( "Token request did not return a response" );
				}

				using StreamReader reader       = new StreamReader( tokenResponseStream );
				string             responseText = await reader.ReadToEndAsync();

				return JsonConvert.DeserializeObject<AccessTokenResponse>( responseText ) ?? throw new InvalidOperationException( "Failed to deserialize the response from the server" );
			}
			catch( WebException err )
			{
				if( err is { Status: WebExceptionStatus.ProtocolError, Response: HttpWebResponse response } )
				{
					var responseStream = response.GetResponseStream();
					if( responseStream == null )
					{
						throw new NullReferenceException( "Web request did not return a response" );
					}

					using StreamReader reader       = new StreamReader( responseStream );
					string             responseText = await reader.ReadToEndAsync();
					throw new WebException( err.Message + "\r\n" + responseText );
				}

				throw;
			}
		}

		private static async Task<AccessTokenResponse> AuthorizeAndReturnAccessToken( AuthorizationConfig config )
		{
			// Creates a redirect URI using an available port on the loopback address.
			string redirectUri = config.RedirectUri;

			// Creates an HttpListener to listen for requests on that redirect URI.
			using var http = new HttpListener();
			http.Prefixes.Add( redirectUri );

			http.Start();

			var encodedRedirect = Uri.EscapeDataString( redirectUri );
			var encodedScope    = Uri.EscapeDataString( "https://www.googleapis.com/auth/fitness.sleep.read" );

			// Creates the OAuth 2.0 authorization request.
			string authorizationRequest = $"{config.AuthorizationEndpoint}?prompt=consent&response_type=code&access_type=offline&scope={encodedScope}&redirect_uri={encodedRedirect}&client_id={config.ClientID}";

			// Opens request in the user's default browser.
			OpenUrl( authorizationRequest );

			// Waits for the OAuth authorization response.
			var context = await http.GetContextAsync();

			// Sends an HTTP response to the browser.
			var    response       = context.Response;
			string responseString = "<html><body><h3>Authorization complete.</h3><p>You may close this browser window and return to the application</p></body></html>";
			byte[] buffer         = Encoding.UTF8.GetBytes( responseString );

			response.ContentLength64 = buffer.Length;

			var responseOutput = response.OutputStream;
			await responseOutput.WriteAsync( buffer, 0, buffer.Length );
			responseOutput.Close();

			http.Stop();

			// Checks for errors.
			string? error = context.Request.QueryString.Get( "error" );
			if( error != null )
			{
				throw new Exception( $"OAuth authorization error: {error}." );
			}

			var code = context.Request.QueryString.Get( "code" );
			if( code == null )
			{
				throw new Exception( $"Malformed authorization response. {context.Request.QueryString}" );
			}

			// Starts the code exchange at the Token Endpoint.
			return await ExchangeCodeForTokensAsync( config, code );
		}

		private static async Task<AccessTokenResponse> ExchangeCodeForTokensAsync( AuthorizationConfig config, string code )
		{
			var encodedRedirect = Uri.EscapeDataString( config.RedirectUri );

			string tokenRequestUri  = config.TokenRequestUri;
			string tokenRequestBody = $"code={code}&redirect_uri={encodedRedirect}&client_id={config.ClientID}&client_secret={config.ClientSecret}&scope=&grant_type=authorization_code";

			HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create( tokenRequestUri );
			tokenRequest.Method      = "POST";
			tokenRequest.ContentType = "application/x-www-form-urlencoded";
			tokenRequest.Accept      = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

			byte[] tokenRequestBodyBytes = Encoding.ASCII.GetBytes( tokenRequestBody );
			tokenRequest.ContentLength = tokenRequestBodyBytes.Length;

			await using( Stream requestStream = tokenRequest.GetRequestStream() )
			{
				await requestStream.WriteAsync( tokenRequestBodyBytes, 0, tokenRequestBodyBytes.Length );
			}

			try
			{
				var tokenResponse       = await tokenRequest.GetResponseAsync();
				var tokenResponseStream = tokenResponse.GetResponseStream();

				if( tokenResponseStream == null )
				{
					throw new NullReferenceException( "Token request did not return a response" );
				}

				using StreamReader reader       = new StreamReader( tokenResponseStream );
				string             responseText = await reader.ReadToEndAsync();

				return JsonConvert.DeserializeObject<AccessTokenResponse>( responseText ) ?? throw new InvalidOperationException( "Failed to deserialize the response from the server." );
			}
			catch( WebException err )
			{
				if( err is { Status: WebExceptionStatus.ProtocolError, Response: HttpWebResponse response } )
				{
					var responseStream = response.GetResponseStream();
					if( responseStream == null )
					{
						throw new NullReferenceException( $"Request to {tokenRequestUri} did not return a response" );
					}

					using StreamReader reader       = new StreamReader( responseStream );
					string             responseText = await reader.ReadToEndAsync();
					throw new WebException( err.Message + "\r\n" + responseText );
				}

				throw;
			}
		}

		private static void OpenUrl( string url )
		{
			try
			{
				Process.Start( new ProcessStartInfo { FileName = url, UseShellExecute = true } );
			}
			catch
			{
				// https://github.com/dotnet/runtime/issues/17938
				if( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
				{
					url = url.Replace( "&", "^&" );
					Process.Start( new ProcessStartInfo( url ) { UseShellExecute = true } );
				}
				else if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
				{
					Process.Start( "xdg-open", url );
				}
				else if( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
				{
					Process.Start( "open", url );
				}
				else
				{
					throw;
				}
			}
		}

		#endregion

		#region Nested types

		internal class AccessTokenResponse
		{
			[JsonProperty( "access_token" )]
			public string? AccessToken { get; set; }

			[JsonProperty( "refresh_token" )]
			public string? RefreshToken { get; set; }

			[JsonProperty( "expires_in" )]
			public int? ExpiresIn { get; set; }

			[JsonProperty( "scope" )]
			public string? Scope { get; set; }

			[JsonProperty( "token_type" )]
			public string? TokenType { get; set; }
		}

		#endregion
	}
}
