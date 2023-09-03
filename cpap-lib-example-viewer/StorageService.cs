using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class StorageService
{
	#region Private class fields

	private static string _appDocumentsPath = string.Empty;

	#endregion

	#region Class constructor

	static StorageService()
	{
		string appDataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		string outputFolder = Path.Combine(appDataFilePath, Application.ProductName);
		if ( !Directory.Exists( outputFolder ) )
		{
			Directory.CreateDirectory( outputFolder );
		}

		string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		_appDocumentsPath = Path.Combine( myDocumentsPath, Application.ProductName );

		if ( !Directory.Exists( _appDocumentsPath ) )
		{
			Directory.CreateDirectory( _appDocumentsPath );
		}
	}

	#endregion

	#region Public static methods

	public static string GetStorageFolderForDate( DateTime date, bool ensureFolderExists = false )
	{
		var storageFolder = Path.Combine(_appDocumentsPath, date.ToString("yyyy-MM"));

		if ( ensureFolderExists )
		{
			if ( !Directory.Exists( _appDocumentsPath ) )
			{
				Directory.CreateDirectory( _appDocumentsPath );
			}

			if ( !Directory.Exists( storageFolder ) )
			{
				Directory.CreateDirectory( storageFolder );
			}
		}

		return storageFolder;
	}

	public static string GetSuggestedFilenameForDateTime( DateTime date )
	{
		return $"Session {date:yyyy-MM-dd_HH-mm-ss}.edf";
	}

	#endregion
}
