using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using cpaplib;

namespace example_viewer.Loaders;

public class OxyLinkLoader : ISessionDataLoader
{
	// NOTES:
	// Sample filename: "Oxylink 1250_1692103555000.csv"
	// Header: "Time,Oxygen Level,Pulse Rate,Motion"
	
	#region Public properties 
	
	public string FriendlyName  { get => "OxyLink Pulse Oximeter"; }

	public string Source { get => "Viatom OxyLink Ring"; }

	public string FileExtension { get => "csv"; }

	public string FilenameFilter { get => "OxyLink Pulse Oximeter File|OxyLink*.csv"; }

	public string FilenameMatchPattern { get => @"Oxylink \d{4}_\d{10,}\.csv"; }
	
	#endregion 
	
	#region Private fields 

	private static string[] _expectedHeaders = { "Time", "Oxygen Level", "Pulse Rate", "Motion" };
	
	#endregion 
	
	#region Public functions 

	public (List<Session>, List<ReportedEvent>) Load( Stream stream )
	{
		using var reader = new StreamReader( stream, Encoding.Default, leaveOpen: true );

		var firstLine = reader.ReadLine();
		if( string.IsNullOrEmpty( firstLine ) )
		{
			return (null, null);
		}

		var headers = firstLine.Split( ',' );
		if( !_expectedHeaders.SequenceEqual( headers ) )
		{
			return (null, null);
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

		Session result = new()
		{
			Source = this.Source,
			Signals = { oxygen, pulse, movement }
		};

		bool isStartRecord    = true;
		byte lastGoodOxy      = 0;
		byte lastGoodHR       = 0;
		byte lastGoodMovement = 0;

		while( !reader.EndOfStream )
		{
			var line = reader.ReadLine();

			if( string.IsNullOrEmpty( line ) )
			{
				return (null, null);
			}

			var lineData = line.Split( ',' );
				
			if( !DateTime.TryParse( lineData[ 0 ], out DateTime dateTimeValue ) )
			{
				return (null, null);
			}
			else if( isStartRecord )
			{
				result.StartTime = dateTimeValue;
				isStartRecord    = false;
			}

			result.EndTime = dateTimeValue;

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

		oxygen.StartTime = pulse.StartTime = movement.StartTime = result.StartTime;
		oxygen.EndTime   = pulse.EndTime   = movement.EndTime   = result.EndTime;

		return (new List<Session>() { result }, null);
	}
	
	#endregion 
}
