using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class MainView : UserControl
{
	public static readonly RoutedEvent<RoutedEventArgs> ImportRequestEvent =
		RoutedEvent.Register<MainView, RoutedEventArgs>( "ImportRequested", RoutingStrategies.Bubble );

	public event EventHandler<RoutedEventArgs> ImportRequested
	{
		add => AddHandler( ImportRequestEvent, value );
		remove => RemoveHandler( ImportRequestEvent, value );
	}

	public MainView()
	{
		InitializeComponent();
		
		NavView.Content = new HomeView();

		AddHandler( ImportRequestEvent, HandleImportRequest );
		btnImportCPAP.Tapped += HandleImportRequest;
	}

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
			if( string.IsNullOrEmpty( navViewItem.Tag as string ) )
			{
				return;
			}

			var typeName = $"{typeof( MainView ).Namespace}.{navViewItem.Tag}View";
			var pageType = Type.GetType( typeName );

			if( pageType == null )
			{
				throw new Exception( $"Unhandled page type: {navView.Tag}" );
			}

			var page = Activator.CreateInstance( pageType );
			navView.Content         = page;
			navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
		}
	}

	private async void HandleImportRequest( object? sender, RoutedEventArgs e )
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
		
		// ReSharper disable UnusedParameter.Local
		td.Opened += async ( eventOrigin, eventArgs ) =>
		{
			// ReSharper enable UnusedParameter.Local
			
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
				await Task.Delay( Math.Max( 0, 1500 - (Environment.TickCount - startTime) ) );
		
				Dispatcher.UIThread.Post( () =>
				{
					// TODO: How to navigate to the Daily Report view after import happens?
					
					td.Hide( TaskDialogStandardResult.OK );

					using( var store = StorageService.Connect() )
					{
						NavView.SelectedItem = navDailyReport;
						DataContext          = store.LoadDailyReport( mostRecentDay );
					}
					
				} );
			} );
		};

		td.XamlRoot = (Visual)VisualRoot!;
		await td.ShowAsync();	
	}
	
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
}
