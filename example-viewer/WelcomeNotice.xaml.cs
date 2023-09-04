using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using cpaplib;

using Microsoft.Win32;

namespace example_viewer;

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

			if( CpapDataLoader.HasCorrectFolderStructure( drive.RootDirectory.FullName ) )
			{
				var messageBoxResult = MessageBox.Show( $"Found CPAP data on Drive {drive.Name}. Do you want to import it?", $"Import from {drive.Name}?", MessageBoxButton.YesNo );
				if( messageBoxResult == MessageBoxResult.Yes )
				{
					importCpapDataFrom( drive.RootDirectory.FullName );
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
		if( !CpapDataLoader.HasCorrectFolderStructure( path ) )
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
}

