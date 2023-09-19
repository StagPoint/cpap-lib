﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

using cpaplib;

using Microsoft.Win32;

using ModernWpf;

namespace cpapviewer;

public partial class WelcomeNotice
{
	public WelcomeNotice()
	{
		InitializeComponent();
	}
	
	private void Import_Clicked( object sender, RoutedEventArgs e )
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

				var messageBoxResult = MessageBox.Show( $"There appears to be a CPAP data folder structure on Drive {drive.Name}\nMachine: {machineID.ProductName}, Serial #: {machineID.SerialNumber}\n\nDo you want to import this data from {drive.Name}?", $"Import from {drive.Name}?", MessageBoxButton.YesNoCancel );
				if( messageBoxResult == MessageBoxResult.Yes )
				{
					NavigationService.Navigate( new DataBrowser( drive.RootDirectory.FullName ) );
					NavigationService.RemoveBackEntry();
					return;
				}

				if( messageBoxResult == MessageBoxResult.Cancel )
				{
					return;
				}
			}
		}

		OpenFileDialog ofd = new OpenFileDialog()
		{
			Title           = "Open CPAP Data",
			Filter          = "ResMed AirSense 10 Settings File|str.edf",
			CheckPathExists = true,
		};

		var ofdResult = ofd.ShowDialog();
		if( !ofdResult ?? false )
		{
			return;
		}

		var path = Path.GetDirectoryName( ofd.FileName );
		if( !ResMedDataLoader.HasCorrectFolderStructure( path ) )
		{
			MessageBox.Show( $"Folder {path} does not appear to contain CPAP data" );
			return;
		}

		if( NavigationService != null )
		{
			NavigationService.Navigate( new DataBrowser( path ) );
			NavigationService.RemoveBackEntry();
		}
	}
	
	private void importCpapDataFrom( string rootDirectoryFullName )
	{
		throw new System.NotImplementedException();
	}
	
	private void OnThemeButtonClick( object sender, RoutedEventArgs e )
	{
		DispatcherHelper.RunOnMainThread(() =>
		{
			if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
			{
				ThemeManager.Current.ApplicationTheme     = ApplicationTheme.Light;
				Application.Current.MainWindow.Background = new SolidColorBrush( Color.FromRgb( 253, 253, 253 ) );
			}
			else
			{
				ThemeManager.Current.ApplicationTheme     = ApplicationTheme.Dark;
				Application.Current.MainWindow.Background = new SolidColorBrush( Color.FromRgb( 25, 25, 25 ) );
			}
		});	
	}
}

