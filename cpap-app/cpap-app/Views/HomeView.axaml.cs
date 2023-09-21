using System;
using System.Diagnostics;
using System.IO;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class HomeView : UserControl
{
	public HomeView()
	{
		InitializeComponent();
	}
	
	private async void BtnImport_OnClick( object? sender, RoutedEventArgs e )
	{
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
					ImportFrom( drive.RootDirectory.FullName );
					return;
				}
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS8604 // Possible null reference argument.
		
		var path = await new OpenFolderDialog()
		{
			Title = "Open Folder Dialog"
		}.ShowAsync( this.FindAncestorOfType<Window>() );
		
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS0618 // Type or member is obsolete

		if( !Directory.Exists( path ) )
		{
			return;
		}

		if( !ResMedDataLoader.HasCorrectFolderStructure( path ) )
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from folder",
				$"Folder {path} does not appear to contain CPAP data",
				ButtonEnum.Ok,
				Icon.Database );

			await dialog.ShowWindowDialogAsync( this.FindAncestorOfType<Window>() );
			return;
		}

		ImportFrom( path );
	}

	private void ImportFrom( string folder )
	{
		using var storage = new StorageService( StorageService.GetApplicationDatabasePath() );
		storage.Connection.BeginTransaction();

		try
		{
			int startTime = Environment.TickCount;

			var mostRecentDay = storage.GetMostRecentDay().AddHours( 12 );
				
			var loader = new ResMedDataLoader();
			var days   = loader.LoadFromFolder( folder, mostRecentDay );

			foreach( var day in days )
			{
				storage.SaveDailyReport( day );
			}

			storage.Connection.Commit();
				
			var elapsed = Environment.TickCount - startTime;
			Debug.WriteLine( $"Time to load CPAP data ({days.Count} days): {elapsed / 1000.0f:F3} seconds" );
		
			mostRecentDay = storage.GetMostRecentDay();

			this.DataContext = new DailyReportViewModel( mostRecentDay );
		}
		catch
		{
			storage.Connection.Rollback();
			throw;
		}
	}
}


