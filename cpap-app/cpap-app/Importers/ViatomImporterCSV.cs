using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Avalonia.Platform.Storage;

using cpaplib;

namespace cpap_app.Importers;

public class ViatomImporterCSV : IOximetryImporter
{
	// NOTES:
	// Sample filename: "Oxylink 1250_1692103555000.csv"
	// Sample filename: "O2Ring 0009_1696377606000.csv"
	// Header: "Time,Oxygen Level,Pulse Rate,Motion"
	
	#region Public properties 
	
	public string FriendlyName  { get => "Viatom Mobile CSV"; }

	public string Source { get => "Viatom Pulse Oximeter"; }

	public string FileExtension { get => "csv"; }

	public List<FilePickerFileType> FileTypeFilters { get; } = new()
	{
		new FilePickerFileType( "Viatom Mobile CSV File" )
		{
			Patterns                    = new[] { "OxyLink*.csv", "O2Ring*.csv" },
			AppleUniformTypeIdentifiers = new[] { "public.plain-text" },
			MimeTypes                   = new[] { "text/plain" }
		}
	};

	public string FilenameMatchPattern { get => @"\w+\s*\d{4}_\d{13}\.csv"; }
	
	#endregion 
	
	#region Private fields 

	private static string[] _expectedHeaders = { "Time", "Oxygen Level", "Pulse Rate", "Motion" };
	
	#endregion 
	
	#region Public functions 

	public ImportedData? Load( string filename, Stream stream )
	{
		using var reader = new StreamReader( stream, Encoding.Default, leaveOpen: true );

		var firstLine = reader.ReadLine();
		if( string.IsNullOrEmpty( firstLine ) )
		{
			return null;
		}

		var headers = firstLine.Split( ',' );
		if( !_expectedHeaders.SequenceEqual( headers ) )
		{
			return null;
		}

		Signal oxygen = new Signal
		{
			Name              = SignalNames.SpO2,
			FrequencyInHz     = 0.25,
			MinValue          = 80,
			MaxValue          = 100,
			UnitOfMeasurement = "%",
		};
		
		Signal pulse = new Signal
		{
			Name              = SignalNames.Pulse,
			FrequencyInHz     = 0.25,
			MinValue          = 60,
			MaxValue          = 120,
			UnitOfMeasurement = "bpm",
		};
		
		Signal movement = new Signal
		{
			Name              = SignalNames.Movement,
			FrequencyInHz     = 0.25,
			MinValue          = 0,
			MaxValue          = 100,
			UnitOfMeasurement = "",
		};

		Session session = new()
		{
			Source     = this.Source,
			Signals    = { oxygen, pulse, movement },
			SourceType = SourceType.PulseOximetry
		};

		// Attempt to extract the device name from the filename (Viatom/Wellue devices prepend the device name)
		var baseFilename = filename = Path.GetFileName( filename );
		int nameEndIndex = baseFilename.IndexOf( '_' );
		if( nameEndIndex != -1 )
		{
			session.Source = baseFilename[ ..nameEndIndex ];
		}

		bool isStartRecord    = true;
		int  lastGoodOxy      = 0;
		int  lastGoodHR       = 0;
		int  lastGoodMovement = 0;

		DateTime currentDateTime = DateTime.MinValue;
		DateTime lastDateTime    = DateTime.MinValue;

		// TODO: Figure out how to add configuration options to importers 
		double timeAjustSeconds = 0;

		while( !reader.EndOfStream )
		{
			lastDateTime = currentDateTime;
			
			var line = reader.ReadLine();

			if( string.IsNullOrEmpty( line ) )
			{
				return null;
			}

			var lineData = line.Split( ',' );
				
			if( !DateTime.TryParse( lineData[ 0 ], out currentDateTime ) )
			{
				return null;
			}

			currentDateTime = currentDateTime.AddSeconds( timeAjustSeconds );
			
			if( isStartRecord )
			{
				session.StartTime = currentDateTime;
				isStartRecord    = false;
			}

			session.EndTime = currentDateTime;

			if( int.TryParse( lineData[ 1 ], out var oxy ) && oxy <= 100 )
			{
				oxygen.Samples.Add( oxy );
				lastGoodOxy = oxy;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading.
				// The O2 Insight application will use the "out of range" value of 255 to indicate an invalid reading.
				oxygen.Samples.Add( lastGoodOxy );
			}

			if( int.TryParse( lineData[ 2 ], out var hr ) && hr <= 200 )
			{
				pulse.Samples.Add( hr );
				lastGoodHR = hr;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading
				// The O2 Insight application will use the "out of range" value of 65535 to indicate an invalid reading
				pulse.Samples.Add( lastGoodHR );
			}

			if( byte.TryParse( lineData[ 3 ], out var movementValue ) && movementValue <= 100 )
			{
				movement.Samples.Add( movementValue );
				lastGoodMovement = movementValue;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading
				// The O2 Insight application may use out-of-range values to indicate an invalid reading 
				movement.Samples.Add( lastGoodMovement );
			}
		}

		var frequency = 1.0 / (currentDateTime - lastDateTime).TotalSeconds;
		oxygen.FrequencyInHz   = frequency;
		pulse.FrequencyInHz    = frequency;
		movement.FrequencyInHz = frequency;

		oxygen.StartTime = pulse.StartTime = movement.StartTime = session.StartTime;
		oxygen.EndTime   = pulse.EndTime   = movement.EndTime   = session.EndTime;

		return new ImportedData
		{
			StartTime = session.StartTime,
			EndTime   = session.EndTime,
			Sessions  = new List<Session>() { session },
			Events    = OximetryEventGenerator.GenerateEvents( oxygen, pulse )
		};
	}
	
	#endregion 
}
