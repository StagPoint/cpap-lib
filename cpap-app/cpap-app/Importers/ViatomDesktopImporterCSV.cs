﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Avalonia.Platform.Storage;

using cpaplib;

namespace cpap_app.Importers;

public class ViatomDesktopImporterCSV : IOximetryImporter
{
    // NOTES:
    // Sample filename: "Oxylink-20231004051903_OXIRecord"
    // Header: "Time,SpO2(%),Pulse Rate(bpm),Motion,SpO2 Reminder,PR Reminder,"

	#region Public properties

    public string FriendlyName { get => "Viatom Desktop CSV"; }

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

    public Regex FilenameMatchPattern { get => new Regex( @"\w+-\d{14}_OXIRecord\.csv", RegexOptions.IgnoreCase ); }

	#endregion

	#region Private fields

    private static string[] _expectedHeaders = { "Time", "SpO2(%)", "Pulse Rate(bpm)", "Motion", "SpO2 Reminder", "PR Reminder" };

	#endregion

	#region Public functions

    public ImportedData? Load( string filename, Stream stream, PulseOximetryImportOptions importOptions )
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

        List<ReportedEvent> faultEvents     = new();
        ReportedEvent?      invalidDataFlag = null;

        var baseFilename = filename = Path.GetFileName( filename );
        int nameEndIndex = baseFilename.IndexOf( '-' );
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

        while( !reader.EndOfStream )
        {
            lastDateTime = currentDateTime;

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

            if( !parseDate( datePart, out currentDateTime ) )
            {
                return null;
            }

            currentDateTime = currentDateTime.AddSeconds( importOptions.TimeAdjust );

            // Remove the quoted date column and leave the rest of the data (added 2 to skip the quote and the comma)
            line = line.Substring( quoteIndex + 2 );

            var lineData = line.Split( ',' );

            if( isStartRecord )
            {
                session.StartTime = currentDateTime;
                isStartRecord     = false;
            }

            session.EndTime = currentDateTime;

            if( byte.TryParse( lineData[ 0 ], out var oxy ) && oxy <= 100 )
            {
                oxygen.Samples.Add( oxy + importOptions.CalibrationAdjust );
                lastGoodOxy = oxy;
            }
            else
            {
                // OxyLink pulse oximeters may use "--" to indicate an invalid reading.
                // The O2 Insight application will use the "out of range" value of 255 to indicate an invalid reading.
                oxygen.Samples.Add( lastGoodOxy );

                if( invalidDataFlag == null )
                {
                    invalidDataFlag = new ReportedEvent
                    {
                        Type       = EventType.PulseOximetryFault,
                        SourceType = SourceType.PulseOximetry,
                        StartTime  = currentDateTime,
                        Duration   = TimeSpan.FromSeconds( 1.0 / oxygen.FrequencyInHz ),
                    };

                    faultEvents.Add( invalidDataFlag );
                }
                else
                {
                    invalidDataFlag.Duration += TimeSpan.FromSeconds( 1.0 / oxygen.FrequencyInHz );
                }
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

        var frequency = 1.0 / (currentDateTime - lastDateTime).TotalSeconds;
        oxygen.FrequencyInHz   = frequency;
        pulse.FrequencyInHz    = frequency;
        movement.FrequencyInHz = frequency;

        oxygen.StartTime = pulse.StartTime = movement.StartTime = session.StartTime;
        oxygen.EndTime   = pulse.EndTime   = movement.EndTime   = session.EndTime;

        var result = new ImportedData
        {
            StartTime = session.StartTime,
            EndTime   = session.EndTime,
            Sessions  = new List<Session>() { session },
        };

        if( importOptions.GenerateEvents )
        {
            result.Events = OximetryEventGenerator.GenerateEvents( importOptions, oxygen, pulse );
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

	#region Private functions

    private static bool parseDate( string lineData, out DateTime currentDateTime )
    {
        // Apparently Viatom/Wellue will occasionally just randomly change their file format for no apparent reason, 
        // and one of those recent changes involves the timestamp format. Sheesh. 

        // Example format "10:00:45 PM Oct 25 2023"
        if( DateTime.TryParse( lineData, out currentDateTime ) )
        {
            return true;
        }

        // Example format "22:48:36 26/10/2023"
        if( DateTime.TryParseExact( lineData, "HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out currentDateTime ) )
        {
            return true;
        }

        return false;
    }

	#endregion
}