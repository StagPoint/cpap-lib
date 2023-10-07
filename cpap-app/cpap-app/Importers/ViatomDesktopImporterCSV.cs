using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Avalonia.Platform.Storage;

using cpaplib;

namespace cpap_app.Importers;

public class ViatomDesktopImporterCSV : IOximetryImporter
{
	// NOTES:
	// Sample filename: "Oxylink-20231004051903_OXIRecord"
	// Header: "Time,SpO2(%),Pulse Rate(bpm),Motion,SpO2 Reminder,PR Reminder,"
	
	#region Public properties 
	
	public string FriendlyName  { get => "Viatom Desktop CSV"; }

	public string Source { get => "Viatom Pulse Oximeter"; }

	public string FileExtension { get => "csv"; }

	public List<FilePickerFileType> FileTypeFilters { get; } = new()
	{
		new FilePickerFileType( "Viatom Desktop CSV File" )
		{
			Patterns                    = new[] { "*OXIRecord.csv" },
			AppleUniformTypeIdentifiers = new[] { "public.plain-text" },
			MimeTypes                   = new[] { "text/plain" }
		}
	};

	public string FilenameMatchPattern { get => @"\w+-\d{14}_OXIRecord\.csv"; }
	
	#endregion 
	
	#region Private fields 

	private static string[] _expectedHeaders = { "Time", "SpO2(%)", "Pulse Rate(bpm)", "Motion", "SpO2 Reminder", "PR Reminder" };
	
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

		// NOTE: The O2 Insight app adds an additional erroneous comma to every line
		var headers = firstLine.TrimEnd( ',' ).Split( ',' );
		
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
			SourceType = SourceType.PulseOximetry,
		};

		bool isStartRecord    = true;
		int  lastGoodOxy      = 0;
		int  lastGoodHR       = 0;
		int  lastGoodMovement = 0;

		// TODO: Figure out how to add configuration options to importers 
		double timeAjustSeconds = 0;

		while( !reader.EndOfStream )
		{
			var line = reader.ReadLine();

			if( string.IsNullOrEmpty( line ) )
			{
				return null;
			}

			// NOTE: The O2 Insight app adds an additional erroneous comma to every line
			line = line.TrimEnd( ',' );
			
			// The O2 Insight app stores the date/time as a quoted column
			var quoteIndex = line.LastIndexOf( '"' );
			var datePart   = line.Substring( 1, quoteIndex - 1 );

			if( !DateTime.TryParse( datePart, out DateTime dateTimeValue ) )
			{
				return null;
			}
			
			dateTimeValue = dateTimeValue.AddSeconds( timeAjustSeconds );

			// Remove the quoted date column and leave the rest of the data (added 2 to skip the quote and the comma)
			line = line.Substring( quoteIndex + 2 );

			var lineData = line.Split( ',' );
			
			if( isStartRecord )
			{
				session.StartTime = dateTimeValue;
				isStartRecord    = false;
			}

			session.EndTime = dateTimeValue;

			if( byte.TryParse( lineData[ 0 ], out var oxy ) && oxy <= 100 )
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

			if( byte.TryParse( lineData[ 1 ], out var hr ) && hr <= 200 )
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

			if( byte.TryParse( lineData[ 2 ], out var movementValue ) && movementValue <= 100 )
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
