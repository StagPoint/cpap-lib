using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Avalonia.Platform.Storage;

using cpaplib;

namespace cpap_app.Importers;

public class EmayImporterCSV : IOximetryImporter
{
	// NOTES:
	// Sample filename: "EMAY SpO2-20230713-045916.csv"
	// Header: "Date,Time,SpO2(%),PR(bpm)"

	#region Public properties

	public string FriendlyName { get => "EMAY Pulse Oximeter"; }

	public string Source { get => "EMAY"; }

	public string FileExtension { get => "csv"; }

	public List<FilePickerFileType> FileTypeFilters { get; } = new()
	{
		new FilePickerFileType( "EMAY Pulse Oximeter File" )
		{
			Patterns                    = new[] { "EMAY*.csv" },
			AppleUniformTypeIdentifiers = new[] { "public.plain-text" },
			MimeTypes                   = new[] { "text/plain" }
		}
	};

	public Regex FilenameMatchPattern { get => new Regex( @"EMAY SpO2-\d{8}-\d{6}\.csv", RegexOptions.IgnoreCase ); }

	#endregion

	#region Private fields

	private static string[] _expectedHeaders = { "Date", "Time", "SpO2(%)", "PR(bpm)" };

	#endregion

	#region Public functions

	public ImportedData? Load( string filename, Stream stream, PulseOximetryImportOptions options, OximetryEventGeneratorConfig? eventConfig = null )
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
			FrequencyInHz     = 1,
			MinValue          = 80,
			MaxValue          = 100,
			UnitOfMeasurement = "%",
		};

		Signal pulse = new Signal
		{
			Name              = SignalNames.Pulse,
			FrequencyInHz     = 1,
			MinValue          = 60,
			MaxValue          = 120,
			UnitOfMeasurement = "bpm",
		};

		Session session = new()
		{
			Source     = this.Source,
			Signals    = { oxygen, pulse },
			SourceType = SourceType.PulseOximetry
		};

		List<ReportedEvent> faultEvents     = new();
		ReportedEvent?      invalidDataFlag = null;

		bool isStartRecord = true;
		byte lastGoodOxy   = 0;
		byte lastGoodHR    = 0;

		while( !reader.EndOfStream )
		{
			var line = reader.ReadLine();

			if( string.IsNullOrEmpty( line ) )
			{
				return null;
			}

			var lineData = line.Split( ',' );

			var dateTimeText = $"{lineData[ 0 ]} {lineData[ 1 ]}";
			if( !DateTime.TryParse( dateTimeText, out DateTime dateTimeValue ) )
			{
				return null;
			}

			dateTimeValue = dateTimeValue.AddSeconds( options.TimeAdjust );

			if( isStartRecord )
			{
				session.StartTime = dateTimeValue;
				isStartRecord     = false;
			}

			session.EndTime = dateTimeValue;

			if( byte.TryParse( lineData[ 2 ], out var oxy ) )
			{
				oxygen.Samples.Add( oxy + options.CalibrationAdjust );
				lastGoodOxy = oxy;
			}
			else
			{
				// EMAY pulse oximeters may leave the SpO2 and PR fields blank to indicate an invalid reading
				// TODO: How to handle invalid records in imported files. Split the file, duplicate last good reading, etc?
				oxygen.Samples.Add( lastGoodOxy );

				if( invalidDataFlag == null )
				{
					invalidDataFlag = new ReportedEvent
					{
						Type       = EventType.PulseOximetryFault,
						SourceType = SourceType.PulseOximetry,
						StartTime  = dateTimeValue,
						Duration   = TimeSpan.FromSeconds( 1.0 / oxygen.FrequencyInHz ),
					};

					faultEvents.Add( invalidDataFlag );
				}
				else
				{
					invalidDataFlag.Duration += TimeSpan.FromSeconds( 1.0 / oxygen.FrequencyInHz );
				}
			}

			if( byte.TryParse( lineData[ 3 ], out var hr ) )
			{
				pulse.Samples.Add( hr );
				lastGoodHR = hr;
			}
			else
			{
				// EMAY pulse oximeters may leave the SpO2 and PR fields blank to indicate an invalid reading
				// TODO: How to handle invalid records in imported files. Split the file, duplicate last good reading, etc?
				pulse.Samples.Add( lastGoodHR );
			}
		}

		oxygen.StartTime = pulse.StartTime = session.StartTime;
		oxygen.EndTime   = pulse.EndTime   = session.EndTime;

		var result = new ImportedData
		{
			StartTime = session.StartTime,
			EndTime   = session.EndTime,
			Sessions  = new List<Session>() { session },
		};

		if( options.GenerateEvents )
		{
			result.Events = OximetryEventGenerator.GenerateEvents( eventConfig ?? new OximetryEventGeneratorConfig(), oxygen, pulse );
		}

		if( faultEvents.Count > 1 )
		{
			// Viatom CSV files always end with two lines of invalid data. Ignore those.
			faultEvents.RemoveAt( faultEvents.Count - 1 );

			result.Events.AddRange( faultEvents );
		}

		return result;
	}

	#endregion
}
