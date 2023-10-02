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
	// Header: "Time,Oxygen Level,Pulse Rate,Motion"
	
	#region Public properties 
	
	public string FriendlyName  { get => "OxyLink Pulse Oximeter"; }

	public string Source { get => "Viatom OxyLink Ring"; }

	public string FileExtension { get => "csv"; }

	public List<FilePickerFileType> FileTypeFilters { get; } = new()
	{
		new FilePickerFileType( "OxyLink Pulse Oximeter File" )
		{
			Patterns                    = new[] { "OxyLink*.csv" },
			AppleUniformTypeIdentifiers = new[] { "public.plain-text" },
			MimeTypes                   = new[] { "text/plain" }
		}
	};

	public string FilenameMatchPattern { get => @"Oxylink \d{4}_\d{10,}\.csv"; }
	
	#endregion 
	
	#region Private fields 

	private static string[] _expectedHeaders = { "Time", "Oxygen Level", "Pulse Rate", "Motion" };
	
	#endregion 
	
	#region Public functions 

	public ImportedData? Load( Stream stream )
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
			Source = this.Source,
			Signals = { oxygen, pulse, movement }
		};

		bool isStartRecord    = true;
		byte lastGoodOxy      = 0;
		byte lastGoodHR       = 0;
		byte lastGoodMovement = 0;

		// TODO: Figure out how to add configuration options to importers 
		double timeAjustSeconds = 0;

		while( !reader.EndOfStream )
		{
			var line = reader.ReadLine();

			if( string.IsNullOrEmpty( line ) )
			{
				return null;
			}

			var lineData = line.Split( ',' );
				
			if( !DateTime.TryParse( lineData[ 0 ], out DateTime dateTimeValue ) )
			{
				return null;
			}

			dateTimeValue = dateTimeValue.AddSeconds( timeAjustSeconds );
			
			if( isStartRecord )
			{
				session.StartTime = dateTimeValue;
				isStartRecord    = false;
			}

			session.EndTime = dateTimeValue;

			if( byte.TryParse( lineData[ 1 ], out var oxy ) )
			{
				oxygen.Samples.Add( oxy );
				lastGoodOxy = oxy;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading
				// TODO: How to handle invalid records in imported files. Split the file, duplicate last good reading, etc?
				oxygen.Samples.Add( lastGoodOxy );
			}

			if( byte.TryParse( lineData[ 2 ], out var hr ) )
			{
				pulse.Samples.Add( hr );
				lastGoodHR = hr;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading
				// TODO: How to handle invalid records in imported files. Split the file, duplicate last good reading, etc?
				pulse.Samples.Add( lastGoodHR );
			}

			if( byte.TryParse( lineData[ 3 ], out var movementValue ) )
			{
				movement.Samples.Add( movementValue );
				lastGoodMovement = movementValue;
			}
			else
			{
				// OxyLink pulse oximeters may use "--" to indicate an invalid reading
				// TODO: How to handle invalid records in imported files. Split the file, duplicate last good reading, etc?
				movement.Samples.Add( lastGoodMovement );
			}
		}

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
