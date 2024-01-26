using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_app.Animation;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.Importers;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Windowing;

using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

using OAuth;

namespace cpap_app.Views;

public partial class MainView : UserControl
{
	#region Public events

	public class ImportRequestEventArgs : RoutedEventArgs
	{
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate   { get; set; }
		
		public Action? OnImportComplete { get; set; }

		public ImportRequestEventArgs( RoutedEvent? routedEvent )
			: base( routedEvent )
		{
		}
	}

	public static readonly RoutedEvent<ImportRequestEventArgs> ImportCpapRequestedEvent =
		RoutedEvent.Register<MainView, ImportRequestEventArgs>( nameof( ImportCpapRequested ), RoutingStrategies.Bubble );

	public event EventHandler<ImportRequestEventArgs> ImportCpapRequested
	{
		add => AddHandler( ImportCpapRequestedEvent, value );
		remove => RemoveHandler( ImportCpapRequestedEvent, value );
	}

	public static readonly RoutedEvent<ImportRequestEventArgs> ImportOximetryRequestedEvent =
		RoutedEvent.Register<MainView, ImportRequestEventArgs>( nameof( ImportOximetryRequested ), RoutingStrategies.Bubble );

	public event EventHandler<ImportRequestEventArgs> ImportOximetryRequested
	{
		add => AddHandler( ImportOximetryRequestedEvent, value );
		remove => RemoveHandler( ImportOximetryRequestedEvent, value );
	}

	public static readonly RoutedEvent<DateTimeRoutedEventArgs> LoadDateRequestedEvent =
		RoutedEvent.Register<MainView, DateTimeRoutedEventArgs>( nameof( LoadDateRequested ), RoutingStrategies.Bubble );

	public event EventHandler<DateTimeRoutedEventArgs> LoadDateRequested
	{
		add => AddHandler( LoadDateRequestedEvent, value );
		remove => RemoveHandler( LoadDateRequestedEvent, value );
	}

	#endregion 
	
	#region Public properties

	public static readonly DirectProperty<MainView, UserProfile> ActiveUserProfileProperty =
		AvaloniaProperty.RegisterDirect<MainView, UserProfile>( nameof( ActiveUserProfile ), o => o.ActiveUserProfile );

	public UserProfile ActiveUserProfile
	{
		get => _activeUserProfile;
		set => SetAndRaise( ActiveUserProfileProperty, ref _activeUserProfile, value );
	}
	
	#endregion
	
	#region Private fields

	private UserProfile _activeUserProfile = null!;
	
	#endregion 
	
	#region Constructor 
	
	public MainView()
	{
		InitializeComponent();
		
		NavFrame.IsNavigationStackEnabled = false;
		NavFrame.CacheSize                = 0;

		NavView.SelectedItem = navHome;

		AddHandler( ImportCpapRequestedEvent,                  HandleImportRequestCPAP );
		AddHandler( ImportOximetryRequestedEvent,              HandleImportRequestOximetry );
		AddHandler( LoadDateRequestedEvent,                    HandleLoadDateRequest );
		AddHandler( UserProfileEvents.UserProfileChangedEvent, HandleUserProfileChanged );

		btnImportCPAP.Click      += HandleImportRequestCPAP;
		btnImportOximetry.Click  += HandleImportRequestOximetry;
		btnImportGoogleFit.Click += HandleImportRequestGoogleFit;
		
		ActiveUserProfile = UserProfileStore.GetActiveUserProfile();
		
		navProfile.Tapped += ( sender, args ) =>
		{
			navProfile.ContextFlyout!.ShowAt( navProfile );
		};

		NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
	}

	#endregion 
	
	#region Base class overrides

	protected override void OnPointerPressed( PointerPressedEventArgs eventArgs )
	{
		base.OnPointerPressed( eventArgs );
		
		var point = eventArgs.GetCurrentPoint( this );
		if( point.Properties.IsXButton1Pressed && !navHome.IsSelected )
		{
			LoadTabPage( new HomeView() );
			navHome.IsSelected = true;
		}
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( ActiveUserProfile ) )
		{
			if( change.NewValue is UserProfile profile )
			{
				UserProfileStore.SetActive( profile );
			}
			
			LoadProfileMenu();
			LoadTabPage( new HomeView() );
			navHome.IsSelected = true;
		}
	}

	#endregion 
	
	#region Event handlers

	private void HandleUserProfileChanged( object? sender, UserProfileEventArgs e )
	{
		if( e.Profile.UserProfileID == ActiveUserProfile.UserProfileID )
		{
			if( e.Action == UserProfileAction.Deleted )
			{
				ActiveUserProfile = UserProfileStore.GetActiveUserProfile();
			}
			else
			{
				ActiveUserProfile = e.Profile;
			}
		}
		else if( e.Action == UserProfileAction.Activated )
		{
			ActiveUserProfile = e.Profile;
		}

		DataContext = null;
		
		LoadProfileMenu();
	}

	private void NavImportFrom_OnTapped( object? sender, TappedEventArgs e )
	{
		navImportFrom.ContextFlyout!.ShowAt( navImportFrom );
	}

	private void NavView_OnSelectionChanged( object? sender, NavigationViewSelectionChangedEventArgs e )
	{
		if( sender is not NavigationView navView )
		{
			return;
		}

		if( e.IsSettingsSelected )
		{
			NavFrame.Navigate( typeof( AppSettingsView ), null, e.RecommendedNavigationTransitionInfo );

			return;
		}

		if( e.SelectedItem is NavigationViewItem navViewItem )
		{
			switch( navViewItem.Tag )
			{
				case null:
					return;
				case System.Type pageType:
				{
					// Clear out any DailyReport from previous views or profiles
					DataContext = null;
					
					var page = Activator.CreateInstance( pageType ) as Control;
					if( page == null )
					{
						throw new InvalidCastException( $"The type {pageType} could not be cast to an appropriate view type" );
					}

					NavigationTransitionInfo transition = new FadeNavigationTransitionInfo();
					
					if( page is DailyReportView dailyReportView )
					{
						dailyReportView.ActiveUserProfile = ActiveUserProfile;
						
						// DailyReportView is way too heavy to deal with complex animations 
						transition = new SuppressNavigationTransitionInfo();
					}
					
					LoadTabPage( page, transition );

					var closePaneWhenLoading = new[] { navDailyReport, navHistory, navStatistics };

					if( closePaneWhenLoading.Contains( NavView.SelectedItem ) )
					{
						navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
					}
					else if( object.ReferenceEquals( NavView.SelectedItem, navHome ) )
					{
						SetNavViewDisplayMode( NavigationViewPaneDisplayMode.Left );
					}

					return;
				}
			}
		}
	}

	private async void HandleImportRequestGoogleFit( object? sender, RoutedEventArgs e )
	{
		var clientConfig = AuthorizationConfigStore.GetConfig();
		if( !clientConfig.IsValid )
		{
			var isConfigured = await ConfigureGoogleFitClient( clientConfig );
			if( !isConfigured )
			{
				return;
			}

			AuthorizationConfigStore.SaveConfig( clientConfig );
		}

		var accessTokenInfo = AccessTokenStore.GetAccessTokenInfo();
		if( !accessTokenInfo.IsValid )
		{
			accessTokenInfo = await AuthorizeGoogleFitClient();
			if( accessTokenInfo is not { IsValid: true } )
			{
				return;
			}

			AccessTokenStore.SaveAccessTokenInfo( accessTokenInfo );
		}

		if( !accessTokenInfo.AccessTokenIsValid )
		{
			var newTokenInfo = await AuthorizationClient.RefreshAuthorizationTokenAsync( clientConfig, accessTokenInfo.RefreshToken );
			if( newTokenInfo.AccessTokenIsValid )
			{
				accessTokenInfo = newTokenInfo;
				AccessTokenStore.SaveAccessTokenInfo( accessTokenInfo );
			}
		}
		
		var appWindow = TopLevel.GetTopLevel( this ) as AppWindow;
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.Indeterminate );

		await using var iconStream = AssetLoader.Open( new Uri( $"avares://{Assembly.GetExecutingAssembly().FullName}/Assets/google_fit_icon.png" ) );

		var progressDialog = new TaskDialog
		{
			XamlRoot        = appWindow,
			Title           = $"Sync with Google Fit",
			MinWidth        = 500,
			ShowProgressBar = true,
			IconSource      = new ImageIconSource() { Source = new Bitmap( iconStream ) },
			SubHeader       = "Importing sleep information from Google Fit",
			Content         = "Please wait while your data is imported. This may take a while.",
			Buttons =
			{
				TaskDialogButton.CancelButton
			}
		};

		var      dataWasImported         = false;
		DateTime mostRecentAvailableDate = DateTime.Today;

		progressDialog.Closing += ( dialog, args ) =>
		{
			appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.None );
		};

		progressDialog.Opened += async ( _, _ ) =>
		{
			// Show an animated indeterminate progress bar
			progressDialog.SetProgressBarState( 0, TaskDialogProgressState.Indeterminate );

			var importStartDate = GetImportStartDate( ActiveUserProfile.UserProfileID, SourceType.HealthAPI );
			mostRecentAvailableDate = importStartDate;
			
			var metaSessions = await GoogleFitImporter.ImportAsync( importStartDate, DateTime.Today.AddDays( 1 ), accessTokenInfo.AccessToken, progressNotify );
			if( metaSessions == null || metaSessions.Count == 0 )
			{
				progressDialog.Hide();
				return;
			}

			mostRecentAvailableDate = metaSessions.LastOrDefault()!.EndTime.Date;
			
			progressNotify( $"Merging sessions and events..." );

			metaSessions.Sort();

			await Task.Run( async () =>
			{

				var minDate = metaSessions.Min( x => x.StartTime.Date.AddDays( -1 ) );
				var maxDate = metaSessions.Max( x => x.EndTime.Date.AddDays( 1 ) );

				for( var loop = minDate; loop <= maxDate; loop = loop.AddDays( 1 ) )
				{
					// Copy the current date, because otherwise captures might reference the wrong value
					var date = loop;

					Dispatcher.UIThread.Post( () =>
					{
						progressNotify( $"Processing data for \n{date:D}" );
					} );

					using var db = StorageService.Connect();

					var day = db.LoadDailyReport( ActiveUserProfile.UserProfileID, date );
					if( day == null )
					{
						continue;
					}

					// Obtain a list of import data that could potentially be merged with the current day
					var overlappingImports = metaSessions.Where( x => x.CanMergeWith( day ) ).ToList();

					if( overlappingImports.Count == 0 )
					{
						continue;
					}

					bool dayIsModified = false;

					foreach( var data in overlappingImports )
					{
						foreach( var item in data.Items )
						{
							foreach( var session in item.Sessions )
							{
								// If the day doesn't already contain a matching session, add it
								if( !day.Sessions.Any( x => Session.TimesOverlap( x, session ) && x.SourceType == session.SourceType ) )
								{
									// Add the Session to the Day's Session list. 
									addSessionToDay( day, session );

									// Add only the events that happened during this Session. Note that In practice this 
									// should be all of them, since at the time I wrote this all importers only generated
									// a single Session per file, and that is not expected to change, but if it ever does 
									// this code should be robust in handling that change. 
									day.Events.AddRange( item.Events.Where( x => x.StartTime >= session.StartTime && x.StartTime <= session.EndTime ) );

									dayIsModified = true;
								}
							}
						}
					}

					if( dayIsModified )
					{
						db.SaveDailyReport( ActiveUserProfile.UserProfileID, day );

						dataWasImported = true;
					}
				}
				
				Dispatcher.UIThread.Post( () =>
				{
					progressNotify( "Finishing up..." );
				} );
				
				await Task.Delay( 500 );
			} );

			progressDialog.Hide();
		};

		await progressDialog.ShowAsync();
		
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.None );

		if( !dataWasImported )
		{
			string upToDate = $"All Google Fit data was already up to date.\nMost recent date available: {mostRecentAvailableDate:D}";
			
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from Google Fit",
				upToDate,
				ButtonEnum.Ok,
				Icon.Warning );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}
		else
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from Google Fit",
				$"Import Completed.\nMost recent date available: {mostRecentAvailableDate:D}",
				ButtonEnum.Ok,
				Icon.Warning );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

			var profileID = ActiveUserProfile.UserProfileID;

			switch( NavFrame.Content )
			{
				case DailyReportView { DataContext: DailyReport dailyReport } dailyReportView:
					// TODO: Because DailyReportView has its own flow for loading a DailyReport, this leaves open the possibility of bypassing things like event subscription, etc.
					dailyReportView.ActiveUserProfile = ActiveUserProfile;
					dailyReportView.DataContext       = LoadDailyReport( profileID, dailyReport.ReportDate.Date );
					break;
				case HomeView homeView:
					homeView.DataContext = LoadDailyReport( profileID, null );
					break;
			}
		}

		return;

		void addSessionToDay( DailyReport day, Session session )
		{
			// Search for a CPAP session that overlaps with but starts before the sleep session.
			// If one is found, we'll extend the sleep session to start at the same time. The 
			// reasoning is that FitBit (and presumably other devices) often don't start recording
			// the session until it detects sleep, but if another session started a few minutes
			// earlier then the user is obviously not asleep at that point, and it's also useful
			// to know how much time was spent awake at the start of each session.
			//
			// Similarly, a Sleep Stages session ends when the user is awake so we'll extend the
			// signal to match the latest overlapping Session end time.
			
			var signal = session.GetSignalByName( SignalNames.SleepStages );
			if( signal != null )
			{
				var sampleInterval = 1.0 / signal.FrequencyInHz;

				var matchedSession = day.Sessions
				                        .Where( x =>
					                                x.SourceType is SourceType.CPAP or SourceType.PulseOximetry &&
					                                x.StartTime < signal.StartTime &&
					                                x.EndTime > signal.StartTime )
				                        .MinBy( x => x.StartTime );
				
				if( matchedSession != null )
				{
					var timeDifference  = session.StartTime - matchedSession.StartTime;
					int numberOfSamples = (int)Math.Floor( timeDifference.TotalSeconds / sampleInterval );
				
					for( int i = 0; i < numberOfSamples; i++ )
					{
						signal.Samples.Insert( 0, (int)SleepStage.Awake );
					}
				
					signal.StartTime  -= TimeSpan.FromSeconds( sampleInterval * numberOfSamples );
					session.StartTime =  DateHelper.Min( session.StartTime, signal.StartTime );
				}
				
				matchedSession = day.Sessions
				                        .Where( x =>
					                                x.SourceType is SourceType.CPAP or SourceType.PulseOximetry &&
					                                x.StartTime <= signal.EndTime &&
					                                x.EndTime > signal.EndTime )
				                        .MaxBy( x => x.EndTime );
				
				if( matchedSession != null )
				{
					var timeDifference  = matchedSession.EndTime - session.EndTime;
					int numberOfSamples = (int)Math.Ceiling( timeDifference.TotalSeconds / sampleInterval );

					for( int i = 0; i < numberOfSamples; i++ )
					{
						signal.Samples.Add( (int)SleepStage.Awake );
					}

					signal.EndTime += TimeSpan.FromSeconds( numberOfSamples * sampleInterval );
					session.EndTime =  DateHelper.Max( session.EndTime, signal.EndTime );
				}
			}

			day.AddSession( session );
		}

		void progressNotify( string message )
		{
			progressDialog.Content = message;
		}
	}

	private static DateTime GetImportStartDate( int userID, SourceType sourceType )
	{
		using var store = StorageService.Connect();

		const string SQL = "SELECT day.ReportDate FROM day JOIN session ON session.dayID == day.ID AND session.SourceType = ? WHERE day.UserProfileID = ? ORDER BY ReportDate DESC LIMIT 1";

		var mostRecentImport = store.Connection.ExecuteScalar<DateTime>( SQL, sourceType, userID ).AsLocalTime();
		mostRecentImport = DateHelper.Max( mostRecentImport, DateTime.Today.AddDays( -90 ) );

		return mostRecentImport;
	}

	private async Task<AccessTokenInfo?> AuthorizeGoogleFitClient()
	{
		AccessTokenInfo? accessTokenInfo    = null;
		string?          authorizationError = null;

		var view = new GoogleFitUserAuthorizationView();

		var dialog = new TaskDialog()
		{
			Title    = string.Empty,
			Buttons  = { TaskDialogButton.CancelButton },
			XamlRoot = (Visual)VisualRoot!,
			Content  = view,
			MaxWidth = 600,
		};

		view.AuthorizationSuccess += ( sender, info ) =>
		{
			accessTokenInfo = info;
			dialog.Hide();
		};
		
		view.AuthorizationError   += ( sender, error ) =>
		{
			authorizationError = error;
			dialog.Hide();
		};

		var dialogResult = await dialog.ShowAsync();
		if( (TaskDialogStandardResult)dialogResult == TaskDialogStandardResult.Cancel )
		{
			return null;
		}

		if( !string.IsNullOrEmpty( authorizationError ) )
		{
			var msgBox = MessageBoxManager.GetMessageBoxStandard(
				$"Error during authorization",
				authorizationError,
				ButtonEnum.Ok,
				Icon.Error
			);
			
			await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}

		return accessTokenInfo;
	}

	private async Task<bool> ConfigureGoogleFitClient( AuthorizationConfig clientConfig )
	{
		var dialog = new TaskDialog()
		{
			Title = $"Google API Client App Configuration",
			Buttons =
			{
				TaskDialogButton.OKButton,
				TaskDialogButton.CancelButton,
			},
			XamlRoot = (Visual)VisualRoot!,
			Content  = new GoogleFitClientConfigView() { DataContext = clientConfig },
			MaxWidth = 500,
		};
		
		var dialogResult = await dialog.ShowAsync();
		return (TaskDialogStandardResult)dialogResult == TaskDialogStandardResult.OK && clientConfig.IsValid;
	}

	private void HandleImportRequestCPAP( object? sender, RoutedEventArgs e )
	{
		HandleImportRequestCPAP( sender, new ImportRequestEventArgs( ImportCpapRequestedEvent ) );
	}

	private void HandleImportRequestOximetry( object? sender, RoutedEventArgs e )
	{
		HandleImportRequestOximetry();
	}
	
	private async void HandleImportRequestOximetry()
	{
		var sp = TopLevel.GetTopLevel( this )?.StorageProvider;
		if( sp == null )
		{
			throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
		}

		var myDocumentsFolder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
		var defaultFolder     = ApplicationSettingsStore.GetStringSetting( ApplicationSettingNames.OximetryImportPath, myDocumentsFolder );
		var startFolder       = await sp.TryGetFolderFromPathAsync( defaultFolder );
			
		var genericFileTypeFilter = new FilePickerFileType( "Pulse Oximeter CSV File" )
		{
			Patterns                    = new[] { "*.csv" },
			AppleUniformTypeIdentifiers = new[] { "public.plain-text" },
			MimeTypes                   = new[] { "text/plain" },
		};

		var fileTypeFilters = OximetryImporterRegistry.GetFileTypeFilters();
		fileTypeFilters.Add( genericFileTypeFilter );

		var filePicker = await sp.OpenFilePickerAsync( new FilePickerOpenOptions()
		{
			Title                  = $"Import from Pulse Oximetry File",
			SuggestedStartLocation = startFolder,
			AllowMultiple          = true,
			FileTypeFilter         = fileTypeFilters,
		} );

		if( filePicker.Count == 0 )
		{
			return;
		}

		var importPath = Path.GetDirectoryName( filePicker[ 0 ].Path.LocalPath );
		if( Directory.Exists( importPath ) )
		{
			ApplicationSettingsStore.SaveStringSetting( ApplicationSettingNames.OximetryImportPath, importPath );
		}

		List<MetaSession>  metaSessions = new List<MetaSession>();
		List<ImportedData> importedData = new List<ImportedData>();
		MetaSession?       metaSession  = null;

		var td = new TaskDialog
		{
			XamlRoot        = (Visual)VisualRoot!,
			Title           = $"Import from Pulse Oximetry File",
			ShowProgressBar = true,
			IconSource      = new SymbolIconSource { Symbol = Symbol.Upload },
			SubHeader       = "Performing Import",
			Content         = "Please wait while your data is imported. This may take a while.",
			Buttons =
			{
				TaskDialogButton.CancelButton
			}
		};

		var appWindow = TopLevel.GetTopLevel( this ) as AppWindow;
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.Indeterminate );

		bool operationWasCancelled = false;

		int totalDaysUpdated = 0;
		int totalSessions    = 0;
		int mergedSessions   = 0;
		int failedSessions   = 0;

		var profileID = ActiveUserProfile.UserProfileID;

		td.Opened += async ( _, _ ) =>
		{
			// Show an animated indeterminate progress bar
			td.SetProgressBarState( 0, TaskDialogProgressState.Indeterminate );

			await Task.Run( async () =>
			{
				for( int i = 0; i < filePicker.Count; i++ )
				{
					// ReSharper disable once AccessToModifiedClosure
					if( operationWasCancelled )
					{
						break;
					}
							
					var fileItem = filePicker[ i ];
					var importers = OximetryImporterRegistry.FindCompatibleImporters( fileItem.Name );
					if( importers.Count == 0 )
					{
						// TODO: Need to log import failures and show them to the user
						continue;
					}

					Dispatcher.UIThread.Post( () =>
					{
						td.Content = $"Scanning data file: \n{fileItem.Name}";
					} );
					
					try
					{
						await using var file = File.OpenRead( fileItem.Path.LocalPath );

						var importOptions = ImportOptionsStore.GetPulseOximetryImportOptions( profileID );

						foreach( var importer in importers )
						{
							// ReSharper disable once AccessToModifiedClosure
							if( operationWasCancelled )
							{
								break;
							}

							var data = importer.Load( fileItem.Name, file, importOptions );
							if( data is { Sessions.Count: > 0 } )
							{
								// Keep track of the total number of sessions imported
								totalSessions += data.Sessions.Count;
					
								// Imported sessions will be added to a MetaSession object before merging
								importedData.Add( data );
								
								break;
							}
							else
							{
								// Keep track of the number of sessions that could not be imported
								failedSessions += 1;
							}
						}
					}
					catch( IOException e )
					{
						await Dispatcher.UIThread.InvokeAsync( async () =>
						{
							var msgBox = MessageBoxManager.GetMessageBoxStandard(
								"Import Error",
								$"There was an error importing file {fileItem.Name}: \n{e.Message}",
								ButtonEnum.Ok,
								Icon.Error );

							await msgBox.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
						} );

						return;
					}
				}
				
				// Sort the imported data before grouping into MetaSessions
				importedData.Sort();
				
				// Grouping the imported data into "Meta-sessions" allows us to ensure that sessions that are 
				// separated by up to an hour (on either side of the main group) still get included.
				foreach( var data in importedData )
				{
					if( metaSession == null || !metaSession.CanMerge( data ) )
					{
						metaSession = new MetaSession();
						metaSessions.Add( metaSession );
					}
					
					metaSession.Add( data );
				}

				if( metaSessions.Count > 0 )
				{
					metaSessions.Sort();
					
					var minDate = metaSessions.Min( x => x.StartTime.Date.AddDays( -1 ) );
					var maxDate = metaSessions.Max( x => x.EndTime.Date.AddDays( 1 ) );

					for( var loop = minDate; loop <= maxDate; loop = loop.AddDays( 1 ) )
					{
						// ReSharper disable once AccessToModifiedClosure
						if( operationWasCancelled )
						{
							break;
						}
							
						// Copy the current date, because otherwise captures might reference the wrong value
						var date = loop;

						Dispatcher.UIThread.Post( () =>
						{
							td.Content = $"Merging sessions and events for \n{date:D}";
						} );

						using var db  = StorageService.Connect();
						
						var day = db.LoadDailyReport( profileID, date );
						if( day == null )
						{
							continue;
						}

						// Obtain a list of import data that could potentially be merged with the current day
						var overlappingImports = metaSessions.Where( x => x.CanMergeWith( day ) ).ToList();

						if( overlappingImports.Count == 0 )
						{
							continue;
						}

						bool dayIsModified = false;
						
						foreach( var data in overlappingImports )
						{
							foreach( var item in data.Items )
							{
								foreach( var session in item.Sessions )
								{
									// If the day doesn't already contain a matching session, add it
									if( !day.Sessions.Any( x => Session.TimesOverlap( x, session ) && x.SourceType == session.SourceType ) )
									{
										// Add the Session to the Day's Session list. 
										day.AddSession( session );
										
										// Keep track of the number of sessions that were successfully merged
										mergedSessions += 1;

										// Add only the events that happened during this Session. Note that In practice this 
										// should be all of them, since at the time I wrote this all importers only generated
										// a single Session per file, and that is not expected to change, but if it ever does 
										// this code should be robust in handling that change. 
										day.Events.AddRange( item.Events.Where( x => x.StartTime >= session.StartTime && x.StartTime <= session.EndTime ) );
										day.Events.Sort();

										dayIsModified = true;
									}
								}
							}
						}

						if( dayIsModified )
						{
							day.UpdateSignalStatistics( SignalNames.SpO2 );
							day.UpdateSignalStatistics( SignalNames.Pulse );

							db.SaveDailyReport( profileID, day );

							totalDaysUpdated += 1;
						}
					}
				}

				Dispatcher.UIThread.Post( () =>
				{
					td.Hide( TaskDialogStandardResult.OK );
					appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.None );
				} );
			} );
		};

		var result = await td.ShowAsync();
		if( (TaskDialogStandardResult)result == TaskDialogStandardResult.Cancel )
		{
			operationWasCancelled = true;
		}

		if( metaSessions.Count == 0 || totalDaysUpdated < 1 )
		{
			const string upToDate  = "All pulse oximetry data was already up to date.";
			const string cancelled = "The import operation was cancelled";
			
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from Pulse Oximetry File",
				operationWasCancelled ? cancelled : upToDate,
				ButtonEnum.Ok,
				Icon.Warning );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}
		else
		{
			showImportSummary();
			
			switch( NavFrame.Content )
			{
				case DailyReportView { DataContext: DailyReport dailyReport } dailyReportView:
					// TODO: Because DailyReportView has its own flow for loading a DailyReport, this leaves open the possibility of bypassing things like event subscription, etc.
					dailyReportView.ActiveUserProfile = ActiveUserProfile;
					dailyReportView.DataContext       = LoadDailyReport( profileID, dailyReport.ReportDate.Date );
					break;
				case HomeView homeView:
					homeView.DataContext = LoadDailyReport( profileID, null );
					break;
			}
		}

		return;

		async void showImportSummary()
		{
			var summary = @$"Total files read: {totalSessions}
Days updated: {totalDaysUpdated}
Merged sessions: {mergedSessions}
Duplicate sessions: {totalSessions - mergedSessions - failedSessions}
Files not read: {failedSessions}";


			var dialogParams = new MessageBoxStandardParams()
			{
				MinWidth              = 400,
				ContentTitle          = "Pulse Oximetry Import summary",
				ContentMessage        = summary,
				ButtonDefinitions     = ButtonEnum.Ok,
				Icon                  = Icon.Info,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
			};

			var dialog = MessageBoxManager.GetMessageBoxStandard( dialogParams );
			
			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}
	}

	private void HandleLoadDateRequest( object? sender, DateTimeRoutedEventArgs e )
	{
		// NOTE: If the NavView's SelectedItem is already navDailyReport, it must be cleared or it won't fire the correct events when set
		NavView.SelectedItem = null;

		// NOTE: Changing to the Daily Report tab clears the DataContext, so this *must* be done first 
		NavView.SelectedItem = navDailyReport;

		var day = LoadDailyReport( ActiveUserProfile.UserProfileID, e.DateTime );
		if( day == null )
		{
			DataContext          = null;
			NavView.SelectedItem = navHome;
			
			Debug.WriteLine( $"Date could not be retrieved: {e.DateTime}" );

			return;
		}
		
		var viewModel = new DailyReportViewModel( ActiveUserProfile, day );

		viewModel.ReloadRequired += ( _, args ) =>
		{
			// Of course sender is going to be a DailyReportViewModel, but this is a convenient sanity check  
			if( sender is DailyReportViewModel vm )
			{
				DataContext = vm;
			}
		};

		DataContext = null;
		DataContext = viewModel;
	}

	private void HandleImportRequestOximetry( object? sender, ImportRequestEventArgs e )
	{
		HandleImportRequestOximetry();
	}
	
	private async void HandleImportRequestCPAP( object? sender, ImportRequestEventArgs e )
	{
		var owner = this.FindAncestorOfType<Window>();
		Debug.Assert( owner != null, nameof( owner ) + " != null" );
		
		var profile = ActiveUserProfile;

		// Always show the Home page when import is requested. Otherwise, the user might be on the Daily Details
		// view and Avalonia can get stuck in a layout cycle (this is a bug in Avalonia wrt ItemRepeaters and ScrollViews)
		NavView.SelectedItem = navHome;
						
		var import = await CpapImportHelper.GetImportFolder( owner );
		if( import == null || string.IsNullOrEmpty( import.Folder ) )
		{
			return;
		}

		// Make sure that the user has the correct profile selected for the machine they are importing from
		var machineInfo = import.Loader.LoadMachineIdentificationInfo( import.Folder );
		if( !string.IsNullOrEmpty( profile.MachineID ) && profile.MachineID != machineInfo.SerialNumber )
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from {machineInfo.ProductName} to profile {profile.UserName}?",
				$"The location you have selected contains data for '{machineInfo.ProductName}',\nbut your last import was from '{profile.VentilatorModel}'.\n\nAre you sure you wish to import this data into the {profile.UserName} profile?",
				ButtonEnum.YesNo,
				Icon.Database );

			var result = await dialog.ShowWindowDialogAsync( owner );

			if( result != ButtonResult.Yes )
			{
				return;
			}
		}

		var td = new TaskDialog
		{
			XamlRoot        = owner,
			Title           = "Import CPAP Data",
			ShowProgressBar = true,
			IconSource      = new SymbolIconSource { Symbol = Symbol.Upload },
			SubHeader       = "Performing Import",
			Content         = "Please wait while your CPAP data is imported. This may take a while.",
			Buttons =
			{
				TaskDialogButton.CancelButton
			}
		};

		var appWindow = owner as AppWindow;
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.Indeterminate );

		td.Opened += async ( _, _ ) =>
		{
			// Show an animated indeterminate progress bar
			td.SetProgressBarState( 0, TaskDialogProgressState.Indeterminate );

			var startTime = Environment.TickCount;
		
			await Task.Run( async () =>
			{
				var mostRecentDay = ImportFrom( import.Loader, import.Folder, e.StartDate, e.EndDate, out int numberOfDaysImported );
		
				// Sounds cheesy, but showing a progress bar for even a second serves to show 
				// the user that the work was performed. It's otherwise often too fast for them 
				// to notice, and they're left with the feeling that "it didn't work" because
				// they didn't see any visual feedback. 
				await Task.Delay( Math.Max( 0, 1000 - (Environment.TickCount - startTime) ) );

				Dispatcher.UIThread.Post( onImportComplete );
				
				return;

				async void onImportComplete()
				{
					td.Hide( TaskDialogStandardResult.OK );

					if( mostRecentDay != null && numberOfDaysImported > 0 )
					{
						// TODO: Because DailyReportView has its own flow for loading a DailyReport, this leaves open the possibility of bypassing things like event subscription, etc.
						var importedDay = LoadDailyReport( profile.UserProfileID, mostRecentDay.Value );
						Debug.Assert( importedDay != null, "Most recently imported day failed to load" );
						
						DataContext = importedDay;

						profile.LastImport      = DateTime.Now;
						profile.MachineID       = importedDay.MachineInfo.SerialNumber;
						profile.VentilatorModel = importedDay.MachineInfo.ProductName;
						profile.TherapyMode     = (OperatingMode)importedDay.Settings[ SettingNames.Mode ];
						
						UserProfileStore.Update( profile );

						var dayUnit = numberOfDaysImported > 1 ? "days" : "day";
						var dialog  = MessageBoxManager.GetMessageBoxStandard( $"Import from folder", $"Imported {numberOfDaysImported} {dayUnit}.", ButtonEnum.Ok, Icon.Database );

						await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
					}
					else
					{
						var dialog = MessageBoxManager.GetMessageBoxStandard( $"Import from folder", $"All CPAP data was already up to date.", ButtonEnum.Ok, Icon.Database );

						await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
					}

					appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.None );
				}
			} );
		};

		await td.ShowAsync();

		e.OnImportComplete?.Invoke();
	}

	#endregion 
	
	#region Private functions

	private void LoadProfileMenu()
	{
		if( navProfile.ContextFlyout is not MenuFlyout menu )
		{
			throw new InvalidOperationException();
		}

		menu.Items.Clear();

		// Always show the logged in user profile first 
		addProfileMenuItem( ActiveUserProfile );
		
		var profiles = UserProfileStore.SelectAll();

		if( profiles.Count > 1 )
		{
			menu.Items.Add( new Separator() );

			foreach( var profile in profiles )
			{
				if( profile.UserProfileID != ActiveUserProfile.UserProfileID )
				{
					addProfileMenuItem( profile );
				}
			}
		}

		menu.Items.Add( new Separator() );

		var newProfileMenuItem = new MenuItem()
		{
			Header = "New Profile",
			Icon = new SymbolIcon() { Symbol = Symbol.AddFriend },
		};

		newProfileMenuItem.Click += async ( sender, args ) =>
		{
			var newProfile = new UserProfile();
			
			var contentView = new EditUserProfileView()
			{
				DataContext = newProfile
			};

			var dialog = new TaskDialog()
			{
				Title = $"New User Profile",
				Buttons =
				{
					new TaskDialogButton( "Save", TaskDialogStandardResult.Yes ),
					TaskDialogButton.CancelButton,
				},
				XamlRoot = (Visual)VisualRoot!,
				Content  = contentView,
				MaxWidth = 800,
			};
		
			var dialogResult = await dialog.ShowAsync();

			if( (TaskDialogStandardResult)dialogResult == TaskDialogStandardResult.Yes && !string.IsNullOrEmpty( newProfile.UserName.Trim() ) )
			{
				if( UserProfileStore.Insert( newProfile ) )
				{
					RaiseEvent( new UserProfileEventArgs
					{
						RoutedEvent = UserProfileEvents.UserProfileChangedEvent,
						Source      = this,
						Profile     = newProfile,
						Action      = UserProfileAction.Added
					} );

					ActiveUserProfile = newProfile;
				}
			}
		};

		menu.Items.Add( newProfileMenuItem );
		
		return;

		void addProfileMenuItem( UserProfile profile )
		{
			var menuItem = new MenuItem()
			{
				Header = profile.UserName,
				Icon   = new SymbolIcon() { Symbol = Symbol.Contact },
				Tag    = profile,
			};

			menuItem.Click += ( sender, args ) =>
			{
				ActiveUserProfile = profile;
				menu.Hide();
			};

			menu.Items.Add( menuItem );
		}
	}

	private static DailyReport? LoadDailyReport( int profileId, DateTime? date )
	{
		using var store = StorageService.Connect();

		var day = store.LoadDailyReport( profileId, date ?? store.GetMostRecentStoredDate( profileId ) );

		return day;
	}
	
	private void LoadTabPage( Control page, NavigationTransitionInfo? transition = null )
	{
		transition ??= new FadeNavigationTransitionInfo();
		
		NavFrame.Content = page;
		
		Dispatcher.UIThread.Post( () =>
		{
			if( NavFrame.Content is InputElement control )
			{
				control.Focusable = true;
				control.Focus();
			}
		}, DispatcherPriority.Loaded );

		Dispatcher.UIThread.Post( () =>
		{
			GC.Collect( 0 );
		}, DispatcherPriority.Background );
	}

	private void SetNavViewDisplayMode( NavigationViewPaneDisplayMode mode )
	{
		Dispatcher.UIThread.Post( () =>
		{
			NavView.PaneDisplayMode = mode;
		}, DispatcherPriority.Background );
	}

	private DateTime? ImportFrom( ICpapDataLoader loader, string folder, DateTime? startDate, DateTime? endDate, out int daysImported )
	{
		var profileID      = ActiveUserProfile.UserProfileID;
		var importSettings = ImportOptionsStore.GetCpapImportSettings( profileID );

		using var storage = StorageService.Connect();
		storage.Connection.BeginTransaction();

		try
		{
			int startTime = Environment.TickCount;

			var firstDay = startDate ?? storage.GetMostRecentStoredDate( profileID ).AddHours( 12 );
			var lastDay  = endDate ?? DateTime.Today.AddDays( 1 );

			var days = loader.LoadFromFolder( folder, firstDay, lastDay, importSettings );

			if( days == null || days.Count == 0 )
			{
				daysImported = 0;
				
				storage.Connection.Commit();
				return null;
			}

			daysImported = days.Count;

			foreach( var day in days )
			{
				storage.SaveDailyReport( profileID, day );
			}

			storage.Connection.Commit();

			var elapsed = Environment.TickCount - startTime;
			Debug.WriteLine( $"Time to load CPAP data ({days.Count} days): {elapsed / 1000.0f:F3} seconds" );
			
			return storage.GetMostRecentStoredDate( profileID );
		}
		catch
		{
			storage.Connection.Rollback();
			throw;
		}
	}

	#endregion
}
