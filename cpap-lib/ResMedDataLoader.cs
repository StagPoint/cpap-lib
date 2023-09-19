using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using StagPoint.EDF.Net;

// ReSharper disable ConvertToUsingDeclaration

namespace cpaplib
{
	public class ResMedDataLoader
	{
		public List<DailyReport> Days { get; } = new List<DailyReport>();
		
		private TimeSpan _timeAjustment = new TimeSpan( 0, 0, 1, 10 );

		private static string[] expectedFiles = new[]
		{
			"STR.edf",
			"Identification.tgt",
		};

		private static string[] expectedFolders = new[]
		{
			// "SETTINGS",
			"DATALOG",
		};

		private MachineIdentification _machineInfo = new MachineIdentification();

		public void LoadFromFolder( string folderPath, DateTime? minDate = null, DateTime? maxDate = null, TimeSpan? timeAdjustment = null )
		{
			if( timeAdjustment.HasValue )
			{
				_timeAjustment = (TimeSpan)timeAdjustment;
			}
			
			EnsureCorrectFolderStructure( folderPath );

			_machineInfo = LoadMachineIdentificationInfo( folderPath );

			var indexFilename = Path.Combine( folderPath, "STR.edf" );
			LoadIndexAndSettings( indexFilename, minDate, maxDate );
			
#if ALLOW_ASYNC
			var tasks = new Task[ Days.Count ];

			for( int i = 0; i < Days.Count; i++ )
			{
				var day = Days[ i ];
				
				tasks[i] = Task.Run( () =>
				{
					LoadSessionsForDay( folderPath, day );
				} );
			}

			Task.WaitAll( tasks );
#else
			foreach( var day in Days )
			{
				LoadSessionsForDay( folderPath, day );
			}
#endif

			// Make sure that each Session has its Source set (sessions may be created by other processes, such as
			// pulse oximeter import, etc.)
			foreach( var day in Days )
			{
				foreach( var session in day.Sessions )
				{
					session.Source = _machineInfo.ProductName;
				}
			}
		}

		public static MachineIdentification LoadMachineIdentificationInfo( string rootFolder )
		{
			var filename    = Path.Combine( rootFolder, "Identification.tgt" );
			var machineInfo = new MachineIdentification();

			using( var file = File.OpenRead( filename ) )
			{
				using( var reader = new StreamReader( file ) )
				{
					while( !reader.EndOfStream )
					{
						var line = reader.ReadLine()?.Trim();
						if( string.IsNullOrEmpty( line ) || !line.StartsWith( "#", StringComparison.Ordinal ) )
						{
							continue;
						}

						int spaceIndex = line.IndexOf( " ", StringComparison.Ordinal );
						Debug.Assert( spaceIndex != -1 );

						var key   = line.Substring( 1, spaceIndex - 1 );
						var value = line.Substring( spaceIndex + 1 ).Trim().Replace( '_', ' ' );

						machineInfo.Fields[ key ] = value;
					}

					machineInfo.ProductName  = machineInfo.Fields[ "PNA" ];
					machineInfo.SerialNumber = machineInfo.Fields[ "SRN" ];
					machineInfo.ModelNumber  = machineInfo.Fields[ "PCD" ];
				}
			}

			return machineInfo;
		}

		public static bool HasCorrectFolderStructure( string rootFolder )
		{
			foreach( var folder in expectedFolders )
			{
				var directoryPath = Path.Combine( rootFolder, folder );
				if( !Directory.Exists( directoryPath ) )
				{
					return false;
				}
			}

			foreach( var filename in expectedFiles )
			{
				var filePath = Path.Combine( rootFolder, filename );
				if( !File.Exists( filePath ) )
				{
					return false;
				}
			}

			return true;
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

		private void LoadSessionsForDay( string rootFolder, DailyReport day )
		{
			var logFolder = Path.Combine( rootFolder, $@"DATALOG\{day.ReportDate:yyyyMMdd}" );
			if( !Directory.Exists( logFolder ) )
			{
				throw new DirectoryNotFoundException( $"Could not find the session directory for {logFolder}" );
			}

			LoadEventsAndAnnotations( logFolder, day );
			LoadCheyneStokesEvents( logFolder, day );

			var filenames = Directory.GetFiles( logFolder, "*.edf" );
			
			// I believe the filenames are already sorted, but since it's important that they are sorted by date
			// and time, we'll explicitly sort them. Note that this coincidentally sorts the high-resolution 
			// signals before the low-resolution signals, which is a nice bonus.
			Array.Sort( filenames );
			
			foreach( var filename in filenames )
			{
				var baseFilename = Path.GetFileNameWithoutExtension( filename );

				// EVE and CSL files are handled separately
				var ignoreThisFile =
					baseFilename.EndsWith( "_CSL", StringComparison.InvariantCultureIgnoreCase ) ||
					baseFilename.EndsWith( "_EVE", StringComparison.InvariantCultureIgnoreCase );
				if( ignoreThisFile )
				{
					continue;
				}

				// The file's date, extracted from the filename, will be used to search for the correct session.
				// The date/time is incorrect for any other purpose (I think)
				var fileDate = DateTime
				               .ParseExact( baseFilename.Substring( 0, baseFilename.Length - 4 ), "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture )
				               .Trim( TimeSpan.TicksPerMinute );

				int sessionIndex = 0;
				while( sessionIndex < day.Sessions.Count )
				{
					var session = day.Sessions[ sessionIndex++];
					
					// If the file's extracted date does not intersect with the session, then it's not relevant to that session 
					if( session.StartTime > fileDate || session.EndTime < fileDate )
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
						
						// Now rewind the file, because there's currently no way to "continue reading the file from here"
						file.Position = 0;

						// Read in the EDF file 
						var edf = new EdfFile();
						edf.ReadFrom( file );

						foreach( var signal in edf.Signals )
						{
							var signalName = SignalNames.GetStandardName( signal.Label.Value );
							
							// We don't need to keep the CRC checksum signals 
							if( signalName.StartsWith( "crc", StringComparison.InvariantCultureIgnoreCase ) )
							{
								continue;
							}

							if( signalName.Equals( SignalNames.MaskPressureLow, StringComparison.Ordinal ) )
							{
								// If we already have high-resolution Mask Pressure signal data, ignore the low-resolution 
								if( session.Signals.Any( x => x.Name == SignalNames.MaskPressure ) )
								{
									continue;
								}

								// Otherwise, rename the low-resolution mask pressure data for consistency 
								signalName = signal.Label.Value = SignalNames.MaskPressure;
							}
							
							// Some signals should be scaled to match the other signals
							var scaleToLitersPerMinute = signalName.Equals( SignalNames.FlowRate, StringComparison.Ordinal ) ||
							                             signalName.Equals( SignalNames.LeakRate, StringComparison.Ordinal );
							if( scaleToLitersPerMinute )
							{
								// Convert from L/s to L/m
								signal.PhysicalDimension.Value =  "L/min";
								signal.PhysicalMaximum.Value   *= 60;
								signal.PhysicalMinimum.Value   *= 60;
								
								for( int i = 0; i < signal.Samples.Count; i++ )
								{
									signal.Samples[ i ] *= 60;
								}
							}
							else if( signalName.Equals( SignalNames.TidalVolume, StringComparison.Ordinal ) )
							{
								// I don't know why ResMed reports Tidal Volume in Liters, but ml is the standard
								signal.PhysicalDimension.Value =  "ml";
								signal.PhysicalMaximum.Value   *= 1000;
								signal.PhysicalMinimum.Value   *= 1000;
								
								for( int i = 0; i < signal.Samples.Count; i++ )
								{
									signal.Samples[ i ] *= 1000;
								}
							}

							// Not every signal within a Session will have the same start and end time as the others
							// because of differences in sampling rate, so we keep track of the start time and end
							// time of each Signal separately. Note the addition of a time adjustment, which allows
							// the user to calibrate for "drift" of the ResMed machine's internal clock.  
							var startTime  = header.StartTime.Value + _timeAjustment;
							var endTime    = startTime.AddSeconds( header.NumberOfDataRecords * header.DurationOfDataRecord );

							// We need to see if the session already contains a signal by this name, so we know what to do with it. 
							if( session.GetSignalByName( signalName ) == null )
							{
								// Add the signal to the current session
								session.AddSignal( startTime, endTime, signal );
							}
							else
							{
								// If a session already contains a signal by this name, it means that the ResMed machine has
								// given us MaskOn/MaskOff times that do not match the stored files. This happens when there
								// is a brief interruption in recording such as turning the machine off then back on again 
								// within a short enough time period that it doesn't start a new session, which ends up creating
								// a discontinuous session.
								//
								// We don't allow discontinuous sessions, so when this occurs the simplest thing to do is split
								// the session by creating another one with the same start and end times and adding the signal to
								// that session instead. We don't need to worry about the overlapping times because those will
								// be trimmed to match the stored signals below.
								
								// Check to see if there is an in-progress session that does not yet have this signal.
								var splitSession = day.Sessions.FirstOrDefault( x => session.StartTime <= fileDate && session.EndTime >= fileDate && x.GetSignalByName( signalName ) == null );
								
								// If there is, add it. 
								if( splitSession != null )
								{
									splitSession.AddSignal( startTime, endTime, signal );
								}
								else
								{
									// Create a new Session based on the current session's time period, which will contain all of
									// the discontinuous signals (or at least the next set, as there may be more than one).
									var newSession = new Session()
									{
										StartTime = session.StartTime,
										EndTime   = session.EndTime,
									};
									
									newSession.AddSignal( startTime, endTime, signal );
									
									// Add the session to the DailyReport. We'll need to sort sessions afterward because of this. 
									day.Sessions.Add( newSession );
								}
							}
						}
					}

					break;
				}
			}

			// Remove all sessions that did not have signal data. This happens when the ResMed machine reports a 
			// MaskOn/MaskOff session that is too short for any data to be recorded. 
			day.Sessions.RemoveAll( x => x.Signals.Count == 0 );
			foreach( var maskSession in day.Sessions )
			{
				// Remove all signals whose values are all out of range. This is how the AirSense indicates that 
				// there is no SpO2 or Pulse information when a pulse oximeter is not attached, and there may 
				// be other similar situations I haven't encountered yet (and in any case such signals are not valid). 
				maskSession.Signals.RemoveAll( x => !x.Samples.Any( value => value >= x.MinValue ) );
			}
			
			// Sort the sessions by start time. This is only actually needed when we split a session above during
			// signal matching, but doesn't hurt anything when no sessions are split. 
			day.Sessions.Sort( ( lhs, rhs ) => lhs.StartTime.CompareTo( rhs.StartTime ) );
			
			var firstRecordedTime = DateTime.MaxValue;
			var lastRecordedTime  = DateTime.MinValue;
			
			// Now that each Session has all of its Signals added, each with correct StartTime and EndTime values, 
			// we can update the StartTime and EndTime of the Sessions. These values were previously set by the 
			// MaskOn/MaskOff values, which are convenience values used to match up session files and not accurate
			// start and end times. 
			foreach( var session in day.Sessions )
			{
				// Reset the session times to ensure that we don't keep artificial boundary times 
				session.StartTime = DateTime.MaxValue;
				session.EndTime   = DateTime.MinValue;
				
				// Session start and end times must bound all signal start and end times 
				foreach( var signal in session.Signals )
				{
					session.StartTime = DateUtil.Min( session.StartTime, signal.StartTime );
					session.EndTime   = DateUtil.Max( session.EndTime, signal.EndTime );
				}
				
				lastRecordedTime  = DateUtil.Max( lastRecordedTime, session.EndTime );
				firstRecordedTime = DateUtil.Min( firstRecordedTime, session.StartTime );
			}

			// There is probably *some* value in keeping the "reported" start times and durations, but if so I cannot
			// think of what it is and doing so makes working with and displaying the data a lot more frustrating, so
			// we'll use the *actual* recorded session times instead. 
			day.RecordingStartTime = firstRecordedTime;
			day.RecordingEndTime   = lastRecordedTime;
			day.UsageTime           = TimeSpan.FromSeconds( day.Sessions.Sum( x => x.Duration.TotalSeconds ) );

			// Calculate statistics (min, avg, median, max, etc) for each Signal
			CalculateSignalStatistics( day );
			
			// Generate events that are of interest which are not reported by the ResMed machine
			GenerateEvents( day );
			
			// Generate a summary of all reported events
			CalculateEventSummary( day );
		}

		private void GenerateEvents( DailyReport day )
		{
			GenerateLeakEvents( day );
			GenerateFlowLimitEvents( day );
		}
		
		private void GenerateFlowLimitEvents( DailyReport day )
		{
			// TODO: Flow Limitation Redline needs to be a configurable value 
			const double FlowLimitRedline = 0.2;
			
			foreach( var session in day.Sessions )
			{
				var signal = session.GetSignalByName( SignalNames.FlowLimit );
				if( signal != null )
				{
					Annotate( day.Events, EventType.FlowLimitation, signal, 8, (sample, _) => sample >= FlowLimitRedline );
				}
			}
		}

		private void GenerateLeakEvents( DailyReport day )
		{
			// TODO: Leak Redline needs to be a configurable value 
			const double LeakRedline = 24;
			
			foreach( var session in day.Sessions )
			{
				var signal = session.GetSignalByName( SignalNames.LeakRate );
				if( signal != null )
				{
					Annotate( day.Events, EventType.LargeLeak, signal, 2, (sample, _) => sample > LeakRedline );
				}
			}
		}

		private void Annotate( List<ReportedEvent> events, EventType eventType, Signal signal, double minDuration, Func<double, int, bool> predicate )
		{
			int    state      = 0;
			double eventStart = -1;

			var sourceData     = signal.Samples;
			var sampleInterval = 1.0 / signal.FrequencyInHz;
			
			for( int i = 0; i < sourceData.Count; i++ )
			{
				var sample = sourceData[ i ];
				var time   = i * sampleInterval;

				switch( state )
				{
					case 0:
						if( predicate( sample, i ) )
						{
							eventStart = time;
							state      = 1;
						}
						break;

					case 1:
						// Calculate the duration of the event.
						// Note that we use the time of the *last* sample that didn't match the exit predicate.
						double duration = Math.Max( 0.0, time - eventStart - sampleInterval );
						
						if( !predicate( sample, i ) )
						{
							if( duration >= minDuration )
							{
								var annotation = new ReportedEvent
								{
									Type      = eventType,
									StartTime = signal.StartTime.AddSeconds( eventStart ),
									Duration  = TimeSpan.FromSeconds( duration ),
								};

								events.Add( annotation );
							}

							state = 0;
						}
						break;
				}
			}

			if( state == 1 )
			{
				var annotation = new ReportedEvent()
				{
					Type      = eventType,
					StartTime = signal.StartTime.AddSeconds( eventStart ),
					Duration  = TimeSpan.FromSeconds( sourceData.Count - 1 - eventStart ),
				};

				events.Add( annotation );
			}
		}

		private void CalculateSignalStatistics( DailyReport day )
		{
			// Determine the maximum sort buffer size we'll need so that we only need to allocate and reuse one buffer.
			var maxBufferSize = 0;

			// Note that when this was written, every session was guaranteed to have the same number of signals,
			// which appeared in the same order in each session. Since this is by design, that is not expected to
			// ever change, but it is worth noting here.
			for( int i = 0; i < day.Sessions[ 0 ].Signals.Count; i++ )
			{
				var signalSize = 0;
				foreach( var session in day.Sessions )
				{
					signalSize += session.Signals[ i ].Samples.Count;
				}

				maxBufferSize = Math.Max( maxBufferSize, signalSize );
			}

			// Allocate the buffer that we'll sort signal data in. 
			var calculator = new StatCalculator( maxBufferSize );

			foreach( var signal in day.Sessions[ 0 ].Signals )
			{
				// Automatically calculate statistics for all Signals whose value range is zero or above
				if( signal.MinValue >= 0 && signal.MaxValue > signal.MinValue )
				{
					day.Statistics.Add( calculator.CalculateStats( signal.Name, day.Sessions ) );
				}
			}
		}

		private void LoadCheyneStokesEvents( string logFolder, DailyReport day )
		{
			// Need to keep track of the total time spent in CSR
			double totalTimeInCSR = 0;

			var filenames = Directory.GetFiles( logFolder, "*_CSL.edf" );
			foreach( var filename in filenames )
			{
				var file = EdfFile.Open( filename );
				day.RecordingStartTime = file.Header.StartTime.Value + _timeAjustment;
				
				foreach( var annotationSignal in file.AnnotationSignals )
				{
					double csrStartTime = double.MinValue;
					
					foreach( var annotation in annotationSignal.Annotations )
					{
						// Discard all timekeeping annotations, those aren't relevant to anything. 
						if( annotation.IsTimeKeepingAnnotation )
						{
							continue;
						}

						// Try to convert the annotation text into an Enum for easier processing. 
						var eventFlag = ReportedEvent.FromEdfAnnotation( day.RecordingStartTime, annotation );
						
						// We don't need the "Recording Starts" annotations either 
						if( eventFlag.Type == EventType.RecordingStarts )
						{
							continue;
						}

						if( annotation.Annotation.Equals( "CSR Start", StringComparison.OrdinalIgnoreCase ) )
						{
							csrStartTime = annotation.Onset;
						}
						else if( annotation.Annotation.Equals( "CSR End", StringComparison.OrdinalIgnoreCase ) )
						{
							// We're trusting the CPAP machine not to have overlapping CSR events
							Debug.Assert( csrStartTime >= 0, "CSR Start/End pair mismatch found" );
							
							var newEvent = new ReportedEvent
							{
								StartTime   = day.RecordingStartTime.AddSeconds( csrStartTime ),
								Duration    = TimeSpan.FromSeconds( annotation.Onset - csrStartTime ),
								Type        = EventType.CSR,
							};

							totalTimeInCSR += newEvent.Duration.TotalSeconds;

							day.Events.Add( newEvent );

							// Resetting the csrStartTime to an invalid value allows us to detect when Start/End pairs are mismatched
							csrStartTime = double.MinValue;
						}
						else
						{
							Debug.Assert( false, $"Unhandled Event Type in {filename}: {annotation.Annotation}" );
						}
					}
				}
			}

			// "Time spent in CSR" is given as a percentage of the total time 
			day.EventSummary.CSR = totalTimeInCSR / day.UsageTime.TotalSeconds;
		}
		
		private void LoadEventsAndAnnotations( string logFolder, DailyReport day )
		{
			var filenames = Directory.GetFiles( logFolder, "*_EVE.edf" );
			foreach( var filename in filenames )
			{
				var file = EdfFile.Open( filename );
				day.RecordingStartTime = file.Header.StartTime.Value + _timeAjustment;
				
				foreach( var annotationSignal in file.AnnotationSignals )
				{
					foreach( var annotation in annotationSignal.Annotations )
					{
						// Discard all timekeeping annotations, those aren't relevant to anything. 
						if( annotation.IsTimeKeepingAnnotation )
						{
							continue;
						}

						// Try to convert the annotation text into an Enum for easier processing. 
						var eventFlag = ReportedEvent.FromEdfAnnotation( day.RecordingStartTime, annotation );
						
						// We don't need the "Recording Starts" annotations either 
						if( eventFlag.Type == EventType.RecordingStarts )
						{
							continue;
						}

						// Add the event flags to the current day
						day.Events.Add( eventFlag );
					}
				}
			}
		}
		
		private void CalculateEventSummary( DailyReport day )
		{
			day.EventSummary.ObstructiveApneaCount  = day.Events.Count( x => x.Type == EventType.ObstructiveApnea );
			day.EventSummary.HypopneaCount          = day.Events.Count( x => x.Type == EventType.Hypopnea );
			day.EventSummary.UnclassifiedApneaCount = day.Events.Count( x => x.Type == EventType.Unclassified );
			day.EventSummary.ClearAirwayCount       = day.Events.Count( x => x.Type == EventType.ClearAirway );
			day.EventSummary.RespiratoryEffortCount = day.Events.Count( x => x.Type == EventType.RERA );
			day.EventSummary.FlowLimitCount         = day.Events.Count( x => x.Type == EventType.FlowLimitation );
			day.EventSummary.FlowLimitIndex         = day.EventSummary.FlowLimitCount / day.UsageTime.TotalHours;

			day.EventSummary.TotalTimeInApnea      = TimeSpan.FromSeconds( day.Events.Sum( x => x.Duration.TotalSeconds ) );
			day.EventSummary.TotalTimeOfLargeLeaks = TimeSpan.FromSeconds( day.Events.Where( x => x.Type == EventType.LargeLeak ).Sum( x => x.Duration.TotalSeconds ) );
		}

		private void LoadIndexAndSettings( string filename, DateTime? minDate, DateTime? maxDate )
		{
			var file = EdfFile.Open( filename );

			if( file.Signals.Count == 0 )
			{
				return;
			}

			// The STR.edf file is essentially a vertical table containing the settings data for each
			// recorded day. We need to transpose that data and use it to create a DailyReport for 
			// each available day. 
			for( int i = 0; i < file.Signals[ 0 ].Samples.Count; i++ )
			{
				// Gather a hash table of settings for a single day from across the signals 
				var lookup = new Dictionary<string, double>();
				for( int j = 0; j < file.Signals.Count; j++ )
				{
					var signalName = SignalNames.GetStandardName( file.Signals[ j ].Label );
					lookup[ signalName ] = file.Signals[ j ].Samples[ i ];
				}

				// Read in and process the settings for a single day
				var day = ReadDailyReport( lookup );
				day.MachineInfo        =  _machineInfo;
				day.RecordingStartTime += _timeAjustment;

				Days.Add( day );
			}

			// Mask On and Mask Off times are stored as the number of seconds since the day started.
			// Remember that according to ResMed, the day starts at 12pm (noon) instead of the more conventional 
			// and sane 12am (midnight).
			// There will be a maximum of ten MaskOn/MaskOff events per day (always true?)
			var maskOnSignal  = file.GetSignalByName( "MaskOn",  "Mask On" );
			var maskOffSignal = file.GetSignalByName( "MaskOff", "Mask Off" );

			// There will be an even number of MaskOn/MaskOff times for each day
			var numberOfEntriesPerDay = maskOnSignal.Samples.Count / Days.Count;
			Debug.Assert( maskOnSignal.Samples.Count % numberOfEntriesPerDay == 0, "Invalid calculation of Number of Sessions Per Day" );

			for( int dayIndex = 0; dayIndex < Days.Count; dayIndex++ )
			{
				var day = Days[ dayIndex ];

				if( day.UsageTime.TotalMinutes < 5 )
				{
					continue;
				}

				for( int i = 0; i < day.MaskEvents; i++ )
				{
					var sampleIndex = dayIndex * numberOfEntriesPerDay + i;

					// Stop processing MaskOn/MaskOff when we encounter a -1
					if( maskOnSignal.Samples[ sampleIndex ] < 0 || maskOffSignal.Samples[ sampleIndex ] < 0 )
					{
						break;
					}

					// Mask times are stored as the number of seconds since the "day" started. Remember that
					// the ResMed "day" starts at 12pm (noon) and continues until the next calendar day at 12pm.
					var maskOn   = day.ReportDate.AddMinutes( maskOnSignal.Samples[ sampleIndex ] );
					var maskOff  = day.ReportDate.AddMinutes( maskOffSignal.Samples[ sampleIndex ] );

					// Discard empty sessions
					if( maskOff.Subtract( maskOn ).TotalMinutes < 1 )
					{
						continue;
					}

					var session = new Session()
					{
						StartTime = maskOn,
						EndTime   = maskOff,
					};

					day.Sessions.Add( session );
				}
			}

			// Remove all days that are too short to be valid or are otherwise invalid
			RemoveInvalidDays();

			// Remove days that don't match the provided range. It's less efficient to do this after we've already 
			// gathered the basic day information, but it keeps the code much cleaner and more readable, and this 
			// isn't exactly a performance-critical section of code ;)
			FilterDaysByDate( minDate, maxDate );
		}

		/// <summary>
		/// Reads the statistics, settings, and other information from the stored data
		/// </summary>
		private DailyReport ReadDailyReport( Dictionary<string, double> data )
		{
			// I've tried my best to decode what all of the data means, and convert it to meaningful typed
			// values exposed in a reasonable manner, but it's highly likely that there's something I didn't
			// understand correctly, not to mention fields that are different for different models, so the
			// raw data will be kept available for the consumer of this library to make use of if needs be.
			//RawData = data;

			var day = new DailyReport();

			day.MachineInfo = _machineInfo;
			day.ReportDate  = new DateTime( 1970, 1, 1 ).AddDays( data[ "Date" ] ).AddHours( 12 );

			day.Settings = ReadMachineSettings( data );
			day.EventSummary = ReadEventSummary( data );

			day.MaskEvents = (int)(data[ "MaskEvents" ] / 2);
			day.UsageTime  = TimeSpan.FromMinutes( data[ "Duration" ] );
			//OnDuration = TimeSpan.FromMinutes( data[ "OnDuration" ] );

			day.PatientHours = getValue( "PatientHours" );

			day.Fault.Device     = getValue( "Fault.Device" );
			day.Fault.Alarm      = getValue( "Fault.Alarm" );
			day.Fault.Humidifier = getValue( "Fault.Humidifier" );
			day.Fault.HeatedTube = getValue( "Fault.HeatedTube" );

			double getValue( params string[] keys )
			{
				foreach( var key in keys )
				{
					if( data.TryGetValue( key, out double value ) )
					{
						return value;
					}
				}

				return 0;
			}

			return day;
		}

		private ReportedEventCounts ReadEventSummary( Dictionary<string, double> data )
		{
			return new ReportedEventCounts()
			{
				AHI = data[ "AHI" ],
				HI = data[ "HI" ],
				AI = data[ "AI" ],
				OAI = data[ "OAI" ],
				CAI = data[ "CAI" ],
				UAI = data[ "UAI" ],
				RIN = data[ "RIN" ],
				CSR = data[ "CSR" ],
			};
		}

		private MachineSettings ReadMachineSettings( Dictionary<string, double> data )
		{
			var settings = new MachineSettings();
			
			if( data.TryGetValue( "CPAP_MODE", out double legacyMode ) )
			{
				switch( (int)legacyMode )
				{
					case 1:
					case 2:
						settings.Mode = OperatingMode.APAP;
						break;
					case 3:
						settings.Mode = OperatingMode.CPAP;
						break;
					default:
						settings.Mode = OperatingMode.UNKNOWN;
						break;
				}
			}
			else
			{
				var mode = (int)data.GetValue( "Mode" );
				if( mode >= (int)OperatingMode.MAX_VALUE )
				{
					mode = -1;
				}

				settings.Mode = (OperatingMode)mode;
			}

			if(settings. Mode == OperatingMode.ASV )
			{
				settings.ASV = new AsvSettings
				{
					StartPressure = data[ "S.AV.StartPress" ],
					MinPressure   = data[ "S.AV.MinPS" ],
					MaxPressure   = data[ "S.AV.MaxPS" ],
					EPAP          = data[ "S.AV.EPAP" ],
					EpapMin       = data[ "S.AV.EPAP" ],
					EpapMax       = data[ "S.AV.EPAP" ],
					IpapMin       = data[ "S.AV.EPAP" ] + data[ "S.AV.MinPS" ],
					IpapMax       = data[ "S.AV.EPAP" ] + data[ "S.AV.MaxPS" ],
				};
			}
			else if( settings.Mode == OperatingMode.ASV_VARIABLE_EPAP )
			{
				settings.ASV = new AsvSettings
				{
					StartPressure = data[ "S.AA.StartPress" ],
					MinPressure   = data[ "S.AA.MinPS" ],
					MaxPressure   = data[ "S.AA.MaxPS" ],
					EPAP          = data[ "S.AA.MinEPAP" ],
					EpapMin       = data[ "S.AA.MinEPAP" ],
					EpapMax       = data[ "S.AA.MaxEPAP" ],
				};
			}
			else if( settings.Mode == OperatingMode.AVAPS )
			{
				settings.AVAP = new AvapsSettings
				{
					StartPressure = data[ "S.i.StartPress" ],
					MinPressure   = data[ "S.i.MinPS" ],
					MaxPressure   = data[ "S.i.MaxPS" ],
					EpapAuto      = data[ "S.i.EPAPAuto" ] > 0.5,
					EPAP          = data[ "S.i.EPAP" ],
					EpapMin       = data[ "S.i.EPAP" ],
					EpapMax       = data[ "S.i.EPAP" ],
				};

				if( settings.AVAP.EpapAuto )
				{
					settings.AVAP.EpapMin = data[ "S.i.MinEPAP" ];
					settings.AVAP.EpapMax = data[ "S.i.MaxEPAP" ];
					
					settings.AVAP.IPAP    = settings.AVAP.EpapMin + settings.AVAP.MinPressure;
					settings.AVAP.IpapMin = settings.AVAP.EpapMin + settings.AVAP.MinPressure;
					settings.AVAP.IpapMax = settings.AVAP.EpapMax + settings.AVAP.MaxPressure;
				}
				else 
				{
					settings.AVAP.IPAP    = settings. AVAP.EPAP + settings.AVAP.MinPressure;
					settings.AVAP.IpapMin = settings.AVAP.EPAP + settings.AVAP.MinPressure;
					settings.AVAP.IpapMax = settings.AVAP.EPAP + settings.AVAP.MaxPressure;
				}
			}

			settings.HeatedTube = data["HeatedTube"] > 0.5;
			settings.Humidifier = data["Humidifier"] > 0.5;

			settings.CPAP.StartPressure = data["S.C.StartPress"];
			settings.CPAP.Pressure      = data["S.C.Press"];

			settings.EPR.ClinicianEnabled = data["S.EPR.ClinEnable"] >= 0.5;
			settings.EPR.EprEnabled       = data["S.EPR.EPREnable"] >= 0.5;
			settings.EPR.Level            = (int)data["S.EPR.Level"];
			settings.EPR.Mode             = (EprType)(int)(data["S.EPR.EPRType"] + 1);

			settings.AutoSet.ResponseType  = (AutoSetResponseType)(int)data["S.AS.Comfort"];
			settings.AutoSet.StartPressure = data["S.AS.StartPress"];
			settings.AutoSet.MaxPressure   = data["S.AS.MaxPress"];
			settings.AutoSet.MinPressure   = data["S.AS.MinPress"];

			settings.RampMode = (RampModeType)(int)data["S.RampEnable"];
			settings.RampTime = data["S.RampTime"];

			settings.SmartStart          = data["S.SmartStart"] > 0.5 ? OnOffType.On : OnOffType.Off;
			settings.AntibacterialFilter = data["S.ABFilter"] >= 0.5;

			settings.ClimateControl     = (ClimateControlType)(int)data["S.ClimateControl"];
			settings.Tube               = data["S.Tube"];
			settings.HumidifierStatus   = data["S.HumEnable"] > 0.5 ? OnOffType.On : OnOffType.Off;
			settings.HumidityLevel      = data["S.HumLevel"];
			settings.TemperatureEnabled = data["S.TempEnable"] > 0.5;
			settings.Temperature        = data["S.Temp"] * 1.8 + 32; // Converted from Celsius to Fahrenheit

			settings.Mask = (MaskType)(int)data["S.Mask"];

			settings.Essentials = data["S.PtAccess"] > 0.5 ? EssentialsMode.On : EssentialsMode.Plus;

			settings.PtAccess = data["S.PtAccess"];

			return settings;
		}

		private void FilterDaysByDate( DateTime? minDate, DateTime? maxDate )
		{
			int dayIndex = 0;
			if( minDate.HasValue || maxDate.HasValue )
			{
				while( dayIndex < Days.Count )
				{
					var date = Days[ dayIndex ].ReportDate.Date;

					if( minDate.HasValue && date < minDate )
					{
						Days.RemoveAt( dayIndex );
						continue;
					}

					if( maxDate.HasValue && date > maxDate )
					{
						Days.RemoveAt( dayIndex );
						continue;
					}

					dayIndex += 1;
				}
			}
		}

		private void RemoveInvalidDays()
		{
			int dayIndex = 0;
			while( dayIndex < Days.Count )
			{
				if( Days[ dayIndex ].UsageTime.TotalMinutes <= 5 )
				{
					Days.RemoveAt( dayIndex );
					continue;
				}

				dayIndex += 1;
			}
		}
	}
}
