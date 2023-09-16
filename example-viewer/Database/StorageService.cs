using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace cpaplib.Database
{
	public class StorageService
	{
		#region Public properties

		public static string DatabasePath { get; }

		#endregion

		#region Private class fields

		private static string _applicationDataPath;

		#endregion

		#region Class constructor

		static StorageService()
		{
			var assembly    = Assembly.GetExecutingAssembly();
			var fvi         = FileVersionInfo.GetVersionInfo( assembly.Location );
			var productName = fvi.ProductName;

			string myDocumentsPath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );

			_applicationDataPath = Path.Combine( myDocumentsPath, productName );
			if( !Directory.Exists( _applicationDataPath ) )
			{
				Directory.CreateDirectory( _applicationDataPath );
			}

			DatabasePath = Path.Combine( _applicationDataPath, $"{productName}.db" );
		}

		#endregion

		#region Public static methods

		public static string GetStorageFolderForDate( DateTime date, bool ensureFolderExists = false )
		{
			var storageFolder = Path.Combine( _applicationDataPath, date.ToString( "yyyy-MM" ) );

			if( ensureFolderExists )
			{
				if( !Directory.Exists( _applicationDataPath ) )
				{
					Directory.CreateDirectory( _applicationDataPath );
				}

				if( !Directory.Exists( storageFolder ) )
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
}
