using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class MainView : UserControl
{
	public MainView()
	{
		InitializeComponent();

		NavView.SelectionChanged += NavViewOnSelectionChanged;
		NavView.Content          =  new DailyReportView() { DataContext = new DailyReportViewModel( DateTime.Today ) };
	}
	
	private void NavViewOnSelectionChanged( object? sender, NavigationViewSelectionChangedEventArgs e )
	{
	}

	private void Exit_OnClick( object? sender, RoutedEventArgs e )
	{
		if( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp )
		{
			desktopApp.Shutdown();
		}
		else if( Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime viewApp )
		{
			viewApp.MainView = null;
		}
	}

	private async void ImportCPAP_Click( object? sender, RoutedEventArgs e )
	{
		return;
		
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

		var path = await new OpenFolderDialog()
		{
			Title = "Open Folder Dialog"
		}.ShowAsync( this.FindAncestorOfType<Window>() );

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
		// DEBUG: Get rid of this
		const string databasePath = @"D:\Temp\CPAP.db";
		File.Delete( databasePath );
		
		using( var storage = new StorageService( databasePath ) )
		{
			storage.Connection.BeginTransaction();

			try
			{
				int startTime = Environment.TickCount;
				
				var loader    = new ResMedDataLoader();
				var days      = loader.LoadFromFolder( folder, DateTime.Today.AddDays( -30 ) );

				foreach( var day in days )
				{
					storage.SaveDailyReport( day );
				}

				storage.Connection.Commit();
				
				var elapsed = Environment.TickCount - startTime;
				Debug.WriteLine( $"Time to load CPAP data ({days.Count} days): {elapsed / 1000.0f:F3} seconds" );
		
				var mostRecentDay = storage.GetMostRecentDay();

				//Navigation.DataContext = new DailyReportViewModel( mostRecentDay );
			}
			catch( Exception err )
			{
				storage.Connection.Rollback();
				throw;
			}
		}
	}
}