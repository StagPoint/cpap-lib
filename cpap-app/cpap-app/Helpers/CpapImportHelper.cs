using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Helpers;

public class ImportFolderResult
{
	public required string          Folder { get; init; }
	public required ICpapDataLoader Loader { get; init; }
}

public class CpapImportHelper
{
	#region Private fields 
	
	private static ResMedDataLoader _resMedDataLoader = new ResMedDataLoader();
	private static PRS1DataLoader   _prs1DataLoader   = new PRS1DataLoader();
	
	#endregion 
	
	#region Public functions 
	
	public static async Task<ImportFolderResult?> GetImportFolder( Window owner )
	{
		ICpapDataLoader? loader = null;

		string? importPath = null;

		var drives = DriveInfo.GetDrives();
		foreach( var drive in drives )
		{
			if( !drive.IsReady )
			{
				continue;
			}

			var drivePath = drive.RootDirectory.FullName; 

			loader = FindLoaderForFolder( drivePath );
			if( loader != null )
			{
				var machineID = loader.LoadMachineIdentificationInfo( drivePath );

				var dialog = MessageBoxManager.GetMessageBoxStandard(
					$"Import from {drive.Name}?",
					$"There appears to be a CPAP data folder structure on Drive {drive.Name}\nMachine: {machineID.ProductName}, Serial #{machineID.SerialNumber}\n\nDo you want to import this data from {drive.Name}?",
					ButtonEnum.YesNoCancel,
					Icon.Database );

				var result = await dialog.ShowWindowDialogAsync( owner );

				if( result == ButtonResult.Cancel )
				{
					return null;
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
			var sp = owner.StorageProvider;
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
				return null;
			}

			importPath = folder[ 0 ].Path.LocalPath;
		}

		if( !Directory.Exists( importPath ) )
		{
			return null;
		}

		loader = FindLoaderForFolder( importPath );
		if( loader == null )
		{
			var dialog = MessageBoxManager.GetMessageBoxStandard(
				$"Import from folder",
				$"Folder {importPath} does not appear to contain CPAP data",
				ButtonEnum.Ok,
				Icon.Database );

			await dialog.ShowWindowDialogAsync( owner );
			return null;
		}

		return new ImportFolderResult
		{
			Folder = importPath,
			Loader = loader
		};
	}
	
	#endregion 
	
	#region Private functions 
	
	private static ICpapDataLoader? FindLoaderForFolder( string folder )
	{
		if( _resMedDataLoader.HasCorrectFolderStructure( folder ) )
		{
			return _resMedDataLoader;
		}

		if( _prs1DataLoader.HasCorrectFolderStructure( folder ) )
		{
			return _prs1DataLoader;
		}

		return null;
	}
	
	#endregion 
}
