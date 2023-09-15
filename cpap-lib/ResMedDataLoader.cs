using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class ResMedDataLoader
	{
		public MachineIdentification MachineID = new MachineIdentification();

		public List<DailyReport> Days { get; } = new List<DailyReport>();

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

		public void LoadFromFolder( string folderPath, DateTime? minDate = null, DateTime? maxDate = null )
		{
			EnsureCorrectFolderStructure( folderPath );
			LoadMachineIdentificationInfo( folderPath );

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

			// Make sure that each Session has its Source set (anticipating other potential sources for Session data in the future)
			foreach( var day in Days )
			{
				foreach( var session in day.Sessions )
				{
					session.Source = MachineID.ProductName;
				}
			}
		}

		private void LoadMachineIdentificationInfo( string rootFolder )
		{
			var filename = Path.Combine( rootFolder, "Identification.tgt" );
			MachineID = MachineIdentification.ReadFrom( filename );
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
			// and time, we'll explicitly sort them. 
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
							
							// Some signals should be scaled to match the other signals
							var scaleToLitersPerMinute = signalName.Equals( "Flow Rate", StringComparison.Ordinal ) ||
							                             signalName.Equals( "Leak Rate", StringComparison.Ordinal );
							if( scaleToLitersPerMinute )
							{
								// Convert from L/s to L/m
								signal.PhysicalDimension.Value =  "L/m";
								signal.PhysicalMaximum.Value   *= 60;
								signal.PhysicalMinimum.Value   *= 60;
								
								for( int i = 0; i < signal.Samples.Count; i++ )
								{
									signal.Samples[ i ] *= 60;
								}
							}
							else if( signalName.Equals( "Tidal Volume", StringComparison.Ordinal ) )
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
							// time of each Signal separately. 
							var startTime  = header.StartTime.Value;
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
			
			var lastRecordedTime  = DateTime.MinValue;
			var firstRecordedTime = DateTime.MaxValue;
			
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
			day.Duration           = (lastRecordedTime - day.RecordingStartTime);

			CalculateSignalStatistics( day );
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
			var sortBuffer = new Sorter( maxBufferSize );

			day.Statistics.Add( calculateStatistics( "Mask Pressure",       sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Pressure",            sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Expiratory Pressure", sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Leak Rate",           sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Respiration Rate",    sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Tidal Volume",        sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Minute Vent",         sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Snore",               sortBuffer ) );
			day.Statistics.Add( calculateStatistics( "Flow Limit",          sortBuffer ) );

			// In most cases, there will be no SpO2 or Pulse data available 
			if( day.Sessions.Any( x => x.GetSignalByName( "SpO2" ) != null ) )
			{
				day.Statistics.Add( calculateStatistics( "SpO2",  sortBuffer ) );
				day.Statistics.Add( calculateStatistics( "Pulse", sortBuffer ) );
			}

			SignalStatistics calculateStatistics( string signalName, Sorter sorter )
			{
				// Reset the sorter for the next iteration 
				sorter.Clear();

				// Signal index will be consistent for all sessions (at this stage in the loading process), so grab that
				// to avoid having to look it up each time
				var signalIndex = day.Sessions[ 0 ].Signals.FindIndex( x => x.Name.Equals( signalName, StringComparison.Ordinal ) );

				// The signal wasn't found... Typically only happens when a session is essentially empty because the
				// base session data wasn't found. Should never get to this point if that's the case, but worth checking.
				if( signalIndex == -1 )
				{
					throw new IndexOutOfRangeException( $"Failed to find signal {signalName} for day {day.ReportDate.ToShortDateString()}" );
				}
				
				// Copy all available samples from all sessions into a single array that can be sorted and
				// used to calculate the statistics. 
				foreach( var session in day.Sessions )
				{
					var sourceSamples = session.Signals[ signalIndex ].Samples;
					sorter.AddRange( sourceSamples );
				}

				// Sort the aggregated samples and calculate statistics on the results 
				var sortedSamples = sorter.Sort();
				var bufferLength  = sortedSamples.Count;

				var stats = new SignalStatistics
				{
					SignalName   = signalName,
					Minimum      = sortedSamples[ (int)(bufferLength * 0.01) ],
					Average      = sortedSamples.Average(),
					Maximum      = sortedSamples.Max(),
					Median       = sortedSamples[ bufferLength / 2 ],
					Percentile95 = sortedSamples[ (int)(bufferLength * 0.95) ],
					Percentile99 = sortedSamples[ (int)(bufferLength * 0.995) ],
				};
				
				return stats;
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
				day.RecordingStartTime = file.Header.StartTime;
				
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
								Duration    = annotation.Onset - csrStartTime,
								Type        = EventType.CSR,
							};

							totalTimeInCSR += newEvent.Duration;

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
			day.EventSummary.CSR = totalTimeInCSR / day.OnDuration.TotalSeconds;
		}
		
		private void LoadEventsAndAnnotations( string logFolder, DailyReport day )
		{
			var filenames = Directory.GetFiles( logFolder, "*_EVE.edf" );
			foreach( var filename in filenames )
			{
				var file = EdfFile.Open( filename );
				day.RecordingStartTime = file.Header.StartTime;
				
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

			day.EventSummary.ObstructiveApneaCount = day.Events.Count( x => x.Type == EventType.ObstructiveApnea );
			day.EventSummary.HypopneaCount = day.Events.Count( x => x.Type == EventType.Hypopnea );
			day.EventSummary.UnclassifiedApneaCount = day.Events.Count( x => x.Type == EventType.Unclassified );
			day.EventSummary.ClearAirwayCount = day.Events.Count( x => x.Type == EventType.ClearAirway );
			day.EventSummary.RespiratoryEffortCount = day.Events.Count( x => x.Type == EventType.RERA );

			day.EventSummary.TotalTimeInApnea = TimeSpan.FromSeconds( day.Events.Sum( x => x.Duration ) );
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
				var settings = DailyReport.Read( lookup );

				Days.Add( settings );
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

				if( day.Duration.TotalMinutes < 5 )
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
				if( Days[ dayIndex ].Duration.TotalMinutes <= 5 )
				{
					Days.RemoveAt( dayIndex );
					continue;
				}

				dayIndex += 1;
			}
		}
	}
}
