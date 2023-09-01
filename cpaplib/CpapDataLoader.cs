using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;

using StagPoint.EDF.Net;

namespace cpaplib;

public class CpapDataLoader
{
	public MachineIdentification MachineID = new MachineIdentification();
	
	public List<DailyReport>     Days { get; } = new();

	private static string[] expectedFiles = new[]
	{
		"STR.edf",
		"Identification.tgt",
	};

	private static string[] expectedFolders = new[]
	{
		"SETTINGS",
		"DATALOG",
	};

	public void LoadFromFolder( string folderPath )
	{
		LoadMachineIdentificationInfo( folderPath );
		EnsureCorrectFolderStructure( folderPath );

		var indexFilename = Path.Combine( folderPath, "STR.edf" );
		LoadIndexFile( indexFilename );

		foreach( var day in Days )
		{
			LoadSessionData( folderPath, day );
		}
	}

	private void LoadMachineIdentificationInfo( string rootFolder )
	{
		var filename = Path.Combine( rootFolder, "Identification.tgt" );
		MachineID = MachineIdentification.ReadFrom( filename );
	}

	private void EnsureCorrectFolderStructure( string rootFolder )
	{
		foreach( var folder in expectedFolders )
		{
			var directoryPath = Path.Combine( rootFolder, folder );
			if( !Directory.Exists( directoryPath ) )
			{
				throw new DirectoryNotFoundException( $"Directory {directoryPath} does not exist" );
			}
		}

		foreach( var filename in expectedFiles )
		{
			var filePath = Path.Combine( rootFolder, filename );
			if( !File.Exists( filePath ) )
			{
				throw new FileNotFoundException( $"File {filePath} does not exist" );
			}
		}
	}

	private void LoadSessionData( string rootFolder, DailyReport day )
	{
		var logFolder = Path.Combine( rootFolder, $@"DATALOG\{day.Date:yyyyMMdd}" );
		if( !Directory.Exists( logFolder ) )
		{
			return;
		}

		var filenames = Directory.GetFiles( logFolder, "*.edf" );
		foreach( var filename in filenames )
		{
			var baseFilename = Path.GetFileNameWithoutExtension( filename );
			
			// Ignore CSL files, for now
			if( baseFilename.EndsWith( "_CSL", StringComparison.InvariantCultureIgnoreCase ) )
			{
				continue;
			}

			var fileDate = DateTime
			               .ParseExact( baseFilename.Substring( 0, baseFilename.Length - 4 ), "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture )
			               .Trim( TimeSpan.TicksPerMinute);

			foreach( var session in day.Sessions )
			{
				// The start times will probably not match exactly, but also shouldn't differ by more than a minute.
				// Typically the difference will be that the session starts on an even minute boundary, and the 
				// file does not, so stripping the seconds from the file start time should make them match exactly. 
				if( !session.StartTime.Equals( fileDate ) )
				{
					continue;
				}

				using( var file = File.OpenRead( filename ) )
				{
					// Sigh. There are some EDF headers that have invalid values, and we need to check that first.
					var header = new EdfFileHeader( file );
					if( header.NumberOfDataRecords <= 0 )
					{
						break;
					}

					// Now rewind the file, because there's currently no way to "continue reading the file"
					file.Position = 0;

					// Read in the EDF file 
					var edf = new EdfFile();
					edf.ReadFrom( file );

					Debug.WriteLine( $"Attach session: {session.StartTime}  - {baseFilename}" );

					foreach( var signal in edf.Signals )
					{
						if( signal.Label.Value.StartsWith( "crc", StringComparison.InvariantCultureIgnoreCase ) )
						{
							continue;
						}

						Debug.WriteLine( $"        {signal.Label}" );
					}
				}

				break;
			}
		}
	}

	private void LoadIndexFile( string filename )
	{
		var file = EdfFile.Open( filename );

		// Copy all raw and single-value settings 
		for( int i = 0; i < file.Signals[ 0 ].Samples.Count; i++ )
		{
			// Gather a hash table of settings for a single day from across the signals 
			var lookup = new Dictionary<string, double>();
			for( int j = 0; j < file.Signals.Count; j++ )
			{
				lookup[ file.Signals[ j ].Label ] = file.Signals[ j ].Samples[ i ];
			}

			// Read in and process the settings for a single day
			var settings = DailyReport.Read( lookup );

			Days.Add( settings );
		}

		// Mask On and Mask Off times are stored as the number of seconds since the day started.
		// Remember that according to ResMed, the day starts at 12pm (noon) instead of the more conventional 
		// and sane 12am (midnight).
		// There will be a maximum of ten MaskOn/MaskOff events per day (always true?)
		var maskOnSignal  = GetSignalByName( file, "MaskOn",  "Mask On" );
		var maskOffSignal = GetSignalByName( file, "MaskOff", "Mask Off" );

		// There will be an even number of MaskOn/MaskOff times for each day
		var numberOfEntriesPerDay = maskOnSignal.Samples.Count / Days.Count;
		Debug.Assert( maskOnSignal.Samples.Count % numberOfEntriesPerDay == 0, "Invalid calculation of Number of Sessions Per Day" );

		for( int dayIndex = 0; dayIndex < Days.Count; dayIndex++ )
		{
			var day = Days[ dayIndex ];

			if( day.Duration.TotalMinutes < 5 )
			{
				continue;
			}

			for( int i = 0; i < day.MaskEvents; i++ )
			{
				var sampleIndex = dayIndex * numberOfEntriesPerDay + i;

				// Stop processing MaskOn/MaskOff when we encounter a -1
				if( maskOnSignal.Samples[ sampleIndex ] < 0 )
				{
					break;
				}

				// Mask times are stored as the number of seconds since the "day" started. Remember that
				// the ResMed "day" starts at 12pm (noon) and continues until the next calendar day at 12pm.
				var maskOn  = day.Date.AddMinutes( maskOnSignal.Samples[ sampleIndex ] );
				var maskOff = day.Date.AddMinutes( maskOffSignal.Samples[ sampleIndex ] );

				var session = new MaskSession()
				{
					StartTime = maskOn,
					EndTime   = maskOff,
				};

				day.Sessions.Add( session );
			}
		}
	}
	
	private EdfStandardSignal GetSignalByName( EdfFile file, params string[] labels )
	{
		// This isn't possible under normal usage, but...
		if( labels == null || labels.Length == 0 )
		{
			throw new ArgumentException( nameof( labels ) );
		}
			
		foreach( var label in labels )
		{
			var signal = file.GetSignalByName( label ) as EdfStandardSignal;
			if( signal != null )
			{
				return signal;
			}
		}

		throw new KeyNotFoundException( $"Failed to find a signal named '{labels[0]}" );
	}
}
