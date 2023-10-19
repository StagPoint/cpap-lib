using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_app.Events;
using cpap_app.Importers;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class MainView : UserControl
{
	#region Public events

	public static readonly RoutedEvent<RoutedEventArgs> ImportRequestEvent =
		RoutedEvent.Register<MainView, RoutedEventArgs>( nameof( ImportRequested ), RoutingStrategies.Bubble );

	public event EventHandler<RoutedEventArgs> ImportRequested
	{
		add => AddHandler( ImportRequestEvent, value );
		remove => RemoveHandler( ImportRequestEvent, value );
	}

	#endregion 
	
	#region Constructor 
	
	public MainView()
	{
		InitializeComponent();
		
		NavView.Content = new HomeView();

		AddHandler( ImportRequestEvent, HandleImportRequestCPAP );

		btnImportCPAP.Tapped += HandleImportRequestCPAP;
		
		foreach( var importer in OximetryImporterRegistry.RegisteredImporters )
		{
			btnImportOximetry.MenuItems.Add( new NavigationViewItem()
			{
				Content = importer.FriendlyName,
				Tag = importer, 
			} );
		}
	}
	
	#endregion 
	
	#region Event handlers

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
			if( navViewItem.Tag == null )
			{
				return;
			}
			
			if( navViewItem.Tag is System.Type pageType )
			{
				var page = Activator.CreateInstance( pageType );
				navView.Content         = page;
				navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;

				if( page is InputElement control )
				{
					control.Focusable = true;
					Dispatcher.UIThread.Post( () => control.Focus(), DispatcherPriority.Loaded );
				}

				return;
			}

			if( navViewItem.Tag is IOximetryImporter importer )
			{
				HandleImportRequestOximetry( importer );
			}
		}
	}

	private async void HandleImportRequestOximetry( IOximetryImporter importer )
	{
		var sp = TopLevel.GetTopLevel( this )?.StorageProvider;
		if( sp == null )
		{
			throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
		}

		var filePicker = await sp.OpenFilePickerAsync( new FilePickerOpenOptions()
		{
			Title                  = $"Import from {importer.FriendlyName}",
			SuggestedStartLocation = null,
			AllowMultiple          = true,
			FileTypeFilter         = importer.FileTypeFilters
		} );

		if( filePicker.Count == 0 )
		{
			return;
		}

		var                matcher      = new Regex( importer.FilenameMatchPattern, RegexOptions.IgnoreCase );
		List<MetaSession>  metaSessions = new List<MetaSession>();
		List<ImportedData> importedData = new List<ImportedData>();
		MetaSession?       metaSession  = null;

		var td = new TaskDialog
		{
			Title           = $"Import from {importer.FriendlyName}",
			ShowProgressBar = true,
			IconSource      = new SymbolIconSource { Symbol = Symbol.Upload },
			SubHeader       = "Performing Import",
			Content         = "Please wait while your data is imported. This may take a few seconds.",
			Buttons =
			{
				TaskDialogButton.CancelButton
			}
		};

		var appWindow = TopLevel.GetTopLevel( this ) as AppWindow;
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.Indeterminate );

		bool dataWasImported        = false;
		bool operationWasCanccelled = false;

		td.Opened += async ( _, _ ) =>
		{
			// Show an animated indeterminate progress bar
			td.SetProgressBarState( 0, TaskDialogProgressState.Indeterminate );

			await Task.Run( async () =>
			{
				for( int i = 0; i < filePicker.Count; i++ )
				{
					var fileItem = filePicker[ i ];
					if( !matcher.IsMatch( fileItem.Name ) )
					{
						// TODO: Need to log import failures and show them to the user
						//Debug.WriteLine( $"File '{fileItem.Name}' does not match the file naming convention for '{importer.FriendlyName}'" );
						continue;
					}

					Dispatcher.UIThread.Post( () =>
					{
						td.Content = $"Scanning data file: \n{fileItem.Name}";
					} );
					
					try
					{
						await using var file = File.OpenRead( fileItem.Path.LocalPath );

						var data = importer.Load( fileItem.Name, file );
						if( data is { Sessions.Count: > 0 } )
						{
							importedData.Add( data );
						}
					}
					catch( IOException e )
					{
						// TODO: Need to display a message to the user when a file cannot be opened
						Console.WriteLine( e );
						throw;
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
						// Copy the current date, because otherwise captures might reference the wrong value
						var date = loop;

						Dispatcher.UIThread.Post( () =>
						{
							td.Content = $"Merging sessions and events for \n{date:D}";
						} );

						using var db  = StorageService.Connect();

						var day = db.LoadDailyReport( date );
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
							day.UpdateSignalStatistics( SignalNames.SpO2 );
							day.UpdateSignalStatistics( SignalNames.Pulse );

							db.SaveDailyReport( day );

							dataWasImported = true;
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

		td.XamlRoot = (Visual)VisualRoot!;
		await td.ShowAsync();	

		if( !operationWasCanccelled && metaSessions.Count == 0 || !dataWasImported )
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from {importer.FriendlyName}",
				$"There was no data imported. \nThe files you selected were not the correct format or could not be matched to any existing sessions.",
				ButtonEnum.Ok,
				Icon.Warning );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
		}
		else
		{
			if( NavView.Content is DailyReportView { DataContext: DailyReport dailyReport } dailyReportView )
			{
				using var db = StorageService.Connect();
					
				dailyReportView.DataContext = db.LoadDailyReport( dailyReport.ReportDate.Date );
			}
		}
	}

	private async void HandleImportRequestCPAP( object? sender, RoutedEventArgs e )
	{
		string importPath = string.Empty;

		var drives = DriveInfo.GetDrives();
		foreach( var drive in drives )
		{
			if( !drive.IsReady )
			{
				continue;
			}

			if( ResMedDataLoader.HasCorrectFolderStructure( drive.RootDirectory.FullName ) )
			{
				var machineID = ResMedDataLoader.LoadMachineIdentificationInfo( drive.RootDirectory.FullName );

				var dialog = MessageBoxManager.GetMessageBoxStandard(
					$"Import from {drive.RootDirectory}?",
					$"There appears to be a CPAP data folder structure on Drive {drive.Name}\nMachine: {machineID.ProductName}, Serial #: {machineID.SerialNumber}\n\nDo you want to import this data from {drive.Name}?",
					ButtonEnum.YesNoCancel,
					Icon.Database );

				var result = await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );

				if( result == ButtonResult.Cancel )
				{
					return;
				}

				if( result == ButtonResult.Yes )
				{
					importPath = drive.RootDirectory.FullName;
					break;
				}
			}
		}

		if( string.IsNullOrEmpty( importPath ) )
		{
			var sp = TopLevel.GetTopLevel( this )?.StorageProvider;
			if( sp == null )
			{
				throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
			}

			var folder = await sp.OpenFolderPickerAsync( new FolderPickerOpenOptions
			{
				Title                  = "CPAP Data Import - Select the folder containing your CPAP data",
				SuggestedStartLocation = null,
				AllowMultiple          = false
			} );

			if( folder.Count == 0 )
			{
				return;
			}

			importPath = folder[ 0 ].Path.LocalPath;
		}

		if( !Directory.Exists( importPath ) )
		{
			return;
		}

		if( !ResMedDataLoader.HasCorrectFolderStructure( importPath ) )
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from folder",
				$"Folder {importPath} does not appear to contain CPAP data",
				ButtonEnum.Ok,
				Icon.Database );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
			return;
		}

		var td = new TaskDialog
		{
			Title           = "Import CPAP Data",
			ShowProgressBar = true,
			IconSource      = new SymbolIconSource { Symbol = Symbol.Upload },
			SubHeader       = "Performing Import",
			Content         = "Please wait while your CPAP data is imported. This may take a few seconds.",
			Buttons =
			{
				TaskDialogButton.CancelButton
			}
		};

		var appWindow = TopLevel.GetTopLevel( this ) as AppWindow;
		appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.Indeterminate );

		td.Opened += async ( _, _ ) =>
		{
			// Show an animated indeterminate progress bar
			td.SetProgressBarState( 0, TaskDialogProgressState.Indeterminate );

			var startTime = Environment.TickCount;
		
			await Task.Run( async () =>
			{
				var mostRecentDay = ImportFrom( importPath );
		
				// Sounds cheesy, but showing a progress bar for even a second serves to show 
				// the user that the work was performed. It's otherwise often too fast for them 
				// to notice, and they're left with the feeling that "it didn't work" because
				// they didn't see any visual feedback. 
				await Task.Delay( Math.Max( 0, 1000 - (Environment.TickCount - startTime) ) );
		
				Dispatcher.UIThread.Post( () =>
				{
					// TODO: How to navigate to the Daily Report view after import happens?
					
					td.Hide( TaskDialogStandardResult.OK );

					using( var store = StorageService.Connect() )
					{
						NavView.SelectedItem = navDailyReport;
						DataContext          = store.LoadDailyReport( mostRecentDay );
					}

					appWindow?.PlatformFeatures.SetTaskBarProgressBarState( TaskBarProgressBarState.None );
				} );
			} );
		};

		td.XamlRoot = (Visual)VisualRoot!;
		await td.ShowAsync();	
	}
	
	#endregion 
	
	#region Private functions 
	
	private DateTime ImportFrom( string folder )
	{
		using var storage = StorageService.Connect();
		storage.Connection.BeginTransaction();

		try
		{
			int startTime = Environment.TickCount;

			var mostRecentDay = storage.GetMostRecentStoredDate().AddHours( 12 );

			var loader = new ResMedDataLoader();
			var days   = loader.LoadFromFolder( folder, mostRecentDay );

			foreach( var day in days )
			{
				storage.SaveDailyReport( day );
			}

			storage.Connection.Commit();

			var elapsed = Environment.TickCount - startTime;
			Debug.WriteLine( $"Time to load CPAP data ({days.Count} days): {elapsed / 1000.0f:F3} seconds" );
			
			return storage.GetMostRecentStoredDate();
		}
		catch
		{
			storage.Connection.Rollback();
			throw;
		}
	}
	
	#endregion 
}
