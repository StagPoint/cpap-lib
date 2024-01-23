using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using cpap_app.ViewModels;

using cpaplib;

using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

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
				
				var dialogParams = new MessageBoxCustomParams
				{
					ShowInCenter          = true,
					ContentTitle          = "Import CPAP Data",
					ContentHeader         = $"Import from {drive.Name}?",
					ContentMessage        = $"There appears to be a CPAP data folder structure on Drive {drive.Name}\nMachine: {machineID.ProductName}, Serial #{machineID.SerialNumber}\n\nDo you want to import this data from {drive.Name}?",
					SizeToContent         = SizeToContent.Manual,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					Icon                  = Icon.Database,
					ButtonDefinitions     = new List<ButtonDefinition>()
					{
						new ButtonDefinition() { Name = "Yes", IsDefault = true },	
						new ButtonDefinition() { Name = "Browse", IsDefault = true },	
						new ButtonDefinition() { Name = "Cancel", IsCancel = true},	
					}
				};

				var dialog = MessageBoxManager.GetMessageBoxCustom( dialogParams );
				var result = await dialog.ShowWindowDialogAsync( owner );

				// Note that "Browse" will also break out of this loop, as it is assumed that only one removable
				// drive will contain CPAP data, and I don't think it's unreasonable tp to require that condition.
				switch( result )
				{
					case "Cancel":
						return null;
					case "Yes":
						importPath = drive.RootDirectory.FullName;
						break;
				}

				break;
			}
		}

		if( string.IsNullOrEmpty( importPath ) )
		{
			var sp = owner.StorageProvider;
			if( sp == null )
			{
				throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
			}

			var myDocumentsFolder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
			var defaultFolder     = ApplicationSettingsStore.GetStringSetting( ApplicationSettingNames.CpapImportPath, myDocumentsFolder );
			var startFolder       = await sp.TryGetFolderFromPathAsync( defaultFolder );
			
			var folder = await sp.OpenFolderPickerAsync( new FolderPickerOpenOptions
			{
				Title                  = "CPAP Data Import - Select the folder containing your CPAP data",
				SuggestedStartLocation = startFolder,
				AllowMultiple          = false
			} );

			if( folder.Count == 0 )
			{
				return null;
			}

			importPath = folder[ 0 ].Path.LocalPath;

			if( Directory.Exists( importPath ) )
			{
				ApplicationSettingsStore.SaveStringSetting( ApplicationSettingNames.CpapImportPath, importPath );
			}
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
