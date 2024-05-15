using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

// ReSharper disable UseIndexFromEndExpression
// ReSharper disable BadChildStatementIndent
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable StringLiteralTypo

namespace cpaplib
{
	public class PRS1DataLoader : ICpapDataLoader
	{
		#region Private fields

		private const string DATA_ROOT = "P-Series";

		private static Dictionary<string, string> _modelToProductName = new Dictionary<string, string>()
		{
			{ "760P", "BiPAP Auto (System One 60 Series)" },
			{ "761P", "BiPAP Auto (System One 60 Series)" },
			{ "750P", "BiPAP Auto (System One)" },
			{ "960P", "BiPAP autoSV Advanced (System One 60 Series)" },
			{ "961P", "BiPAP autoSV Advanced (System One 60 Series)" },
			{ "960T", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
			{ "961TCA", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
			{ "950P", "BiPAP AutoSV Advanced System One" },
			{ "951P", "BiPAP AutoSV Advanced System One" },
			{ "1160P", "BiPAP AVAPS 30 (System One 60 Series)" },
			{ "660P", "BiPAP Pro (System One 60 Series)" },
			{ "650P", "BiPAP Pro (System One)" },
			{ "1061401", "BiPAP S/T (C Series)" },
			{ "1061T", "BiPAP S/T 30 (System One 60 Series)" },
			{ "501V", "Dorma 500 Auto (System One 60 Series)" },
			{ "420X150C", "DreamStation 2 Advanced CPAP" },
			{ "410X150C", "DreamStation 2 CPAP" },
			{ "700X110", "DreamStation Auto BiPAP" },
			{ "700X120", "DreamStation Auto BiPAP" },
			{ "700X130", "DreamStation Auto BiPAP" },
			{ "700X150", "DreamStation Auto BiPAP" },
			{ "500X110", "DreamStation Auto CPAP" },
			{ "500X120", "DreamStation Auto CPAP" },
			{ "500X130", "DreamStation Auto CPAP" },
			{ "500X150", "DreamStation Auto CPAP" },
			{ "500X180", "DreamStation Auto CPAP" },
			{ "500X140", "DreamStation Auto CPAP with A-Flex" },
			{ "501X120", "DreamStation Auto CPAP with P-Flex" },
			{ "900X110", "DreamStation BiPAP autoSV" },
			{ "900X120", "DreamStation BiPAP autoSV" },
			{ "900X150", "DreamStation BiPAP autoSV" },
			{ "1130X110", "DreamStation BiPAP AVAPS 30" },
			{ "1130X200", "DreamStation BiPAP AVAPS 30" },
			{ "1131X150", "DreamStation BiPAP AVAPS 30 AE" },
			{ "600X110", "DreamStation BiPAP Pro" },
			{ "600X150", "DreamStation BiPAP Pro" },
			{ "1030X110", "DreamStation BiPAP S/T 30" },
			{ "1030X150", "DreamStation BiPAP S/T 30 with AAM" },
			{ "200X110", "DreamStation CPAP" },
			{ "400X110", "DreamStation CPAP Pro" },
			{ "400X120", "DreamStation CPAP Pro" },
			{ "400X130", "DreamStation CPAP Pro" },
			{ "400X150", "DreamStation CPAP Pro" },
			{ "401X150", "DreamStation CPAP Pro with Auto-Trial" },
			{ "400G110", "DreamStation Go" },
			{ "500G110", "DreamStation Go Auto" },
			{ "500G120", "DreamStation Go Auto" },
			{ "500G150", "DreamStation Go Auto" },
			{ "502G150", "DreamStation Go Auto" },
			{ "560P", "REMstar Auto (System One 60 Series)" },
			{ "560PBT", "REMstar Auto (System One 60 Series)" },
			{ "561P", "REMstar Auto (System One 60 Series)" },
			{ "562P", "REMstar Auto (System One 60 Series)" },
			{ "550P", "REMstar Auto (System One)" },
			{ "551P", "REMstar Auto (System One)" },
			{ "552P", "REMstar Auto (System One)" },
			{ "261CA", "REMstar Plus (System One 60 Series)" },
			{ "261P", "REMstar Plus (System One 60 Series)" },
			{ "251P", "REMstar Plus (System One)" },
			{ "460P", "REMstar Pro (System One 60 Series)" },
			{ "460PBT", "REMstar Pro (System One 60 Series)" },
			{ "461CA", "REMstar Pro (System One 60 Series)" },
			{ "461P", "REMstar Pro (System One 60 Series)" },
			{ "462P", "REMstar Pro (System One 60 Series)" },
			{ "450P", "REMstar Pro (System One)" },
			{ "451P", "REMstar Pro (System One)" },
			{ "452P", "REMstar Pro (System One)" },
		};

		#endregion

		#region Public API

		public bool HasCorrectFolderStructure( string rootFolder )
		{
			return TryFindPropertiesFile( rootFolder, out _ );
		}

		public MachineIdentification LoadMachineIdentificationInfo( string rootFolder )
		{
			if( !TryFindPropertiesFile( rootFolder, out string propertyFilePath ) )
			{
				return null;
			}

			var properties = ReadKeyValueFile( propertyFilePath );
			if( properties == null || properties.Count == 0 )
			{
				return null;
			}

			return MachineIdentificationFromProperties( properties );
		}

		public List<DailyReport> LoadFromFolder( string rootFolder, DateTime? minDate = null, DateTime? maxDate = null, CpapImportSettings importSettings = null )
		{
			if( !TryFindPropertiesFile( rootFolder, out string propertyFilePath ) )
			{
				return null;
			}

			var properties = ReadKeyValueFile( propertyFilePath );
			if( properties == null || properties.Count == 0 )
			{
				return null;
			}

			var family            = int.Parse( properties[ "Family" ] );
			var familyVersion     = int.Parse( properties[ "FamilyVersion" ] );
			var dataFormatVersion = int.Parse( properties[ "DataFormatVersion" ] );

			if( family != 0 || familyVersion != 4 || dataFormatVersion != 2 )
			{
				throw new NotSupportedException( $"Unsupported data format version: Family {family}, FamilyVersion {familyVersion}, Data Format Version {dataFormatVersion}" );
			}

			var machineInfo = MachineIdentificationFromProperties( properties );
			if( machineInfo == null )
			{
				return null;
			}

			var metaSessions = ImportMetaSessions(
				rootFolder,
				minDate ?? DateHelper.UnixEpoch,
				maxDate ?? DateTime.Today,
				importSettings,
				machineInfo
			);

			var days = ProcessMetaSessions( metaSessions, machineInfo, importSettings );

			return days;
		}

		#endregion

		#region Private functions

		private static List<DailyReport> ProcessMetaSessions( List<MetaSession> metaSessions, MachineIdentification machineInfo, CpapImportSettings importSettings )
		{
			List<DailyReport> days = new List<DailyReport>( metaSessions.Count );

			DailyReport currentDay = null;

			foreach( var meta in metaSessions )
			{
				// Remove any sessions that are shorter than the specified minimum duration 
				meta.Sessions.RemoveAll( x => x.Duration.TotalMinutes < importSettings.MinimumSessionLength );
				
				if( currentDay == null || currentDay.ReportDate != meta.StartTime.Date )
				{
					currentDay = new DailyReport
					{
						ReportDate         = meta.StartTime.Date,
						RecordingStartTime = meta.Sessions.Min( x => x.StartTime ),
						RecordingEndTime   = meta.Sessions.Max( x => x.EndTime ),
						MachineInfo        = machineInfo,
					};

					days.Add( currentDay );
				}

				foreach( var sesh in meta.Sessions )
				{
					currentDay.Events.AddRange( sesh.Events );

					// Generate the missing Tidal Volume, Minute Ventilation, Respiration Rate, Inspiration Time,
					// Expiration Time, and I:E Ratio signals from flow data 
					DerivedSignals.GenerateMissingRespirationSignals( currentDay, sesh.Session );

					// Need to generate the AHI signal after events are added
					DerivedSignals.GenerateApneaIndexSignal( currentDay, sesh.Session );

					currentDay.AddSession( sesh.Session );

					// Each DailyReport only retains the settings of the last Session.
					MergeSettings( currentDay, sesh.Settings );
				}
			}

			foreach( var day in days )
			{
				// Some of the events are apparently out of order on import. Although not seen in my sample data,
				// this could conceivably apply to other timestamped collections as well, so we'll just sort them
				// all to be certain. 
				day.Sessions.Sort();
				day.Events.Sort();

				var signalNames = GetSignalNames( day );
				foreach( var signalName in signalNames )
				{
					if( signalName != SignalNames.FlowRate && signalName != SignalNames.AHI )
					{
						day.UpdateSignalStatistics( signalName );
					}
				}

				day.StatsSummary = GenerateStatsSummary( currentDay );
				day.UpdateEventSummary();
			}

			return days;
		}

		private static StatisticsSummary GenerateStatsSummary( DailyReport currentDay )
		{
			if( currentDay.Statistics.Count == 0 )
			{
				return new StatisticsSummary();
			}

			var leakStats        = currentDay.Statistics.First( x => x.SignalName == SignalNames.LeakRate );
			var respirationStats = currentDay.Statistics.First( x => x.SignalName == SignalNames.RespirationRate );
			var minuteVentStats  = currentDay.Statistics.First( x => x.SignalName == SignalNames.MinuteVent );
			var tidalVolumeStats = currentDay.Statistics.First( x => x.SignalName == SignalNames.TidalVolume );
			var pressureStats    = currentDay.Statistics.First( x => x.SignalName == SignalNames.Pressure );

			return new StatisticsSummary
			{
				Leak95                  = leakStats.Percentile95,
				LeakMedian              = leakStats.Median,
				RespirationRateMax      = respirationStats.Maximum,
				RespirationRate95       = respirationStats.Percentile95,
				RespirationRateMedian   = respirationStats.Median,
				MinuteVentilationMax    = minuteVentStats.Maximum,
				MinuteVentilation95     = minuteVentStats.Percentile95,
				MinuteVentilationMedian = minuteVentStats.Median,
				TidalVolumeMax          = tidalVolumeStats.Maximum,
				TidalVolume95           = tidalVolumeStats.Percentile95,
				TidalVolumeMedian       = tidalVolumeStats.Median,
				PressureMax             = pressureStats.Maximum,
				Pressure95              = pressureStats.Percentile95,
				PressureMedian          = pressureStats.Median,
				TargetIpapMax           = 0,
				TargetIpap95            = 0,
				TargetIpapMedian        = 0,
				TargetEpapMax           = 0,
				TargetEpap95            = 0,
				TargetEpapMedian        = 0
			};
		}

		private static List<string> GetSignalNames( DailyReport day )
		{
			var signalNames = new List<string>();
			foreach( var session in day.Sessions )
			{
				signalNames.AddRange( session.Signals.Select( x => x.Name ) );
			}

			return signalNames.Distinct().ToList();
		}

		private static void MergeSettings( DailyReport currentDay, ParsedSettings sessionSettings )
		{
			foreach( var setting in sessionSettings )
			{
				currentDay.Settings[ setting.Key ] = setting.Value;
			}
		}

		private static List<MetaSession> ImportMetaSessions( string rootFolder, DateTime minDate, DateTime maxDate, CpapImportSettings importSettings, MachineIdentification machineInfo )
		{
			// Instantiate a list of "meta sessions" that will be used to group the imported sessions 
			// so that they can be assigned to the correct days. 
			var         metaSessions       = new List<MetaSession>();
			MetaSession currentMetaSession = null;

			// We need a day's worth of padding in either direction to ensure that we import all Sessions for the 
			// first and last day of the set. 
			var paddedMinDate = minDate > DateHelper.UnixEpoch ? minDate.Date.AddDays( -1 ) : minDate;
			var paddedMaxDate = maxDate.Date.AddDays( 1 );

			// Find all of the summary files and scan each one to determine whether it should be included in the import
			var summaryFiles = Directory.GetFiles( rootFolder, "*.001", SearchOption.AllDirectories );
			foreach( var filename in summaryFiles )
			{
				var lastModified = File.GetLastWriteTime( filename );
				if( lastModified.Date < paddedMinDate || lastModified.Date > paddedMaxDate )
				{
					continue;
				}

				using( var file = File.OpenRead( filename ) )
				using( var reader = new BinaryReader( file ) )
				{
					var chunk = DataChunk.Read( reader );
					if( chunk == null )
					{
						continue;
					}

					var header = chunk.Header;
					if( header.Timestamp.Date < paddedMinDate || header.Timestamp.Date > paddedMaxDate )
					{
						continue;
					}

					// Since all timestamps are based off of the header, we only need to adjust import times in one place
					header.Timestamp = header.Timestamp.AdjustImportTime( importSettings );

					var summary = chunk.ReadSummary( header );
					summary.Machine        = machineInfo;
					summary.Session.Source = machineInfo.ProductName;

					// Discard Sessions that are too short to be meaningful. 
					if( summary.Session.Duration.TotalMinutes < 5 )
					{
						continue;
					}

					var importData = ImportTherapySession( summary, Path.GetDirectoryName( filename ) );

					if( currentMetaSession == null || !currentMetaSession.CanAdd( importData ) )
					{
						currentMetaSession = new MetaSession();
						metaSessions.Add( currentMetaSession );
					}

					currentMetaSession.AddSession( importData );
				}
			}

			// Since we padded the date range in order to ensure that all relevant Sessions were imported, 
			// we can now more correctly eliminate any MetaSession that starts outside of that date range. 
			metaSessions.RemoveAll( x => x.StartTime < minDate || x.StartTime > maxDate );

			return metaSessions;
		}

		private static ImportSession ImportTherapySession( ImportSummary summary, string folder )
		{
			var sessionData = new ImportSession
			{
				Settings = summary.Settings,
				Session  = summary.Session,
			};

			// Sample Events file name: 0000000003.002
			var eventFilename = Path.Combine( folder, $"{summary.Session.ID:0000000000}.002" );
			if( File.Exists( eventFilename ) )
			{
				using( var eventFile = File.OpenRead( eventFilename ) )
				using( var eventReader = new BinaryReader( eventFile ) )
				{
					while( eventFile.Position < eventFile.Length )
					{
						var eventChunk = DataChunk.Read( eventReader );
						var events     = eventChunk.ReadEvents( eventChunk.Header );

						sessionData.Events.AddRange( events.Events );
						sessionData.Stats.AddRange( events.Stats );
					}
				}
			}

			// Sample Waveform file name: 0000000003.005
			var waveFormFilename = Path.Combine( folder, $"{summary.Session.ID:0000000000}.005" );
			if( File.Exists( waveFormFilename ) )
			{
				using( var waveformFile = File.OpenRead( waveFormFilename ) )
				using( var waveformReader = new BinaryReader( waveformFile ) )
				{
					var chunks = new List<DataChunk>();

					while( waveformFile.Position < waveformFile.Length )
					{
						var chunk = DataChunk.Read( waveformReader );
						chunks.Add( chunk );
					}

					var signals = ReadSignals( chunks, sessionData );
					for( int i = 0; i < signals.Count; i++ )
					{
						sessionData.Session.AddSignal( signals[ i ] );
					}

					signals = GenerateStatsSignals( sessionData );
					for( int i = 0; i < signals.Count; i++ )
					{
						sessionData.Session.AddSignal( signals[ i ] );
					}
				}
			}

			return sessionData;
		}

		private static List<Signal> GenerateStatsSignals( ImportSession importSession )
		{
			var outputSignalList = new List<Signal>();

			var startTime = importSession.StartTime;
			var endTime   = importSession.EndTime;

			var pressureSignal = BuildPressureSignal( importSession, SignalNames.Pressure, startTime, endTime );
			outputSignalList.Add( pressureSignal );

			if( importSession.Stats.Any( x => x.Name == SignalNames.EPAP ) )
			{
				var epapSignal = BuildPressureSignal( importSession, SignalNames.EPAP, startTime, endTime );
				outputSignalList.Add( epapSignal );
			}

			if( importSession.Stats.Any( x => x.Name == SignalNames.TotalLeak ) )
			{
				var totalLeakSignal = BuildStatsSignal(
					importSession,
					SignalNames.TotalLeak,
					0.0,
					120.0,
					"L/min",
					startTime,
					endTime
				);

				outputSignalList.Add( totalLeakSignal );

				var leakRateSignal = BuildLeakSignal(
					importSession,
					pressureSignal,
					0.0,
					120.0,
					"L/min",
					startTime,
					endTime
				);

				outputSignalList.Add( leakRateSignal );
			}

			return outputSignalList;
		}

		private static List<Signal> ReadSignals( List<DataChunk> chunks, ImportSession importSession )
		{
			const double SAMPLE_FREQUENCY = 0.2;

			if( chunks == null || chunks.Count == 0 )
			{
				return new List<Signal>();
			}

			var events     = importSession.Events;
			var samples    = new List<byte>();
			var firstChunk = chunks[ 0 ];
			var lastChunk  = chunks[ chunks.Count - 1 ];
			var startTime  = firstChunk.Header.Timestamp;
			var duration   = lastChunk.Header.EndTimestamp - firstChunk.Header.Timestamp;
			var numSignals = firstChunk.Header.SignalInfo.Waveforms.Count;

			Debug.Assert( numSignals == 1, "Unexpected number of signals" );

			DataChunk previousChunk = null;
			foreach( var chunk in chunks )
			{
				// If there is a gap between chunks, then we need to fill it
				if( previousChunk != null )
				{
					var gapLength = (chunk.Header.Timestamp - previousChunk.Header.EndTimestamp).TotalSeconds;

					// The only reason I've found (so far) for a gap between chunks is when the machine has flagged
					// a period of "No Breathing Detected", so we'll just fill the gap in the Signal data with zeros
					// to match. It would probably be better to generate two Sessions instead of filling the gap, 
					// but that would require more refactoring than I have time and motivation for at this moment. 
					if( gapLength > SAMPLE_FREQUENCY )
					{
						Debug.Assert( previousChunk.Header.Duration.TotalSeconds > 0, "Previous chunk was unexpectedly zero duration" );
						{
							// Fill in the gap with zeros
							for( int i = 0; i < gapLength / SAMPLE_FREQUENCY; i++ )
							{
								samples.Add( 0 );
							}

							// Add an event to flag the gap
							events.Add( new ReportedEvent
							{
								Type       = EventType.BreathingNotDetected,
								SourceType = SourceType.CPAP,
								StartTime  = previousChunk.Header.EndTimestamp,
								Duration   = TimeSpan.FromSeconds( gapLength ),
							} );
						}
					}
					else if( gapLength < -1 )
					{
						// There is an extremely rare but annoying issue in Session 551 of the sample data where the 
						// timestamp of the first chunk is *wildly* incorrect. Previously a workaround was implemented
						// to patch up the data, but I was never comfortable with it so have decided the best thing
						// to do is simply discard the entire session.
						return new List<Signal>();
					}
					else if( gapLength < 0 )
					{
						// Assuming that a single second overlap can be safely ignored seems to be born out when 
						// looking at the Signal data and how it still seamlessly matches up between Chunks. 
						duration += TimeSpan.FromSeconds( -gapLength );
					}
				}

				samples.AddRange( chunk.BlockData );
				previousChunk = chunk;
			}

			// Calculate the sample rate. Note that the sample rate for this machine is extremely low compared to ResMed machines. 
			var sampleRate = duration.TotalSeconds / samples.Count;
			Debug.Assert( Math.Abs( SAMPLE_FREQUENCY - sampleRate ) < 0.001 );

			Debug.Assert( previousChunk != null, nameof( previousChunk ) + " != null" );

			var outputSignalList = new List<Signal>();

			// Build a Signal from the sample data that matches the expected sample frequency and value ranges 
			var flowSignal = BuildFlowSignal( samples, startTime, previousChunk.Header.EndTimestamp );
			outputSignalList.Add( flowSignal );

			return outputSignalList;
		}

		private static Signal BuildLeakSignal( ImportSession session, Signal pressureSignal, double minValue, double maxValue, string units, DateTime startTime, DateTime endTime )
		{
			var points = session.Stats.Where( x => x.Name == SignalNames.TotalLeak ).ToArray();
			Debug.Assert( points.Length > 0 );

			var signal = new Signal
			{
				Name              = SignalNames.LeakRate,
				FrequencyInHz     = 1,
				MinValue          = minValue,
				MaxValue          = maxValue,
				UnitOfMeasurement = units,
				StartTime         = startTime,
				EndTime           = endTime,
			};

			var outputSamples = signal.Samples;
			var lastIndex     = 0;
			var currentIndex  = 0;

			for( var currentTime = startTime; currentTime < endTime; currentTime += TimeSpan.FromSeconds( signal.FrequencyInHz ) )
			{
				while( currentIndex < points.Length - 1 && points[ currentIndex ].Timestamp <= currentTime )
				{
					lastIndex    =  currentIndex;
					currentIndex += 1;
				}

				var reportedLeak = 0.0;
				if( lastIndex == currentIndex )
				{
					reportedLeak = points[ currentIndex ].Value;
				}
				else
				{
					var t = DateHelper.InverseLerp( points[ lastIndex ].Timestamp, points[ currentIndex ].Timestamp, currentTime );

					reportedLeak = MathUtil.Lerp( points[ lastIndex ].Value, points[ currentIndex ].Value, t );
				}

				var tPressure = MathUtil.InverseLerp( 4, 20, MathUtil.Clamp( 4, 20, pressureSignal[ outputSamples.Count ] ) );

				// TODO: Replace these "Mask-defined leak rate" constants with configurable values. I know I've seen a table for different mask types, can't find it right now.
				// This should be based on mask type or configurable options in the user's preferences, but from 
				// what I've been able to find so far, the differences in "intentional leak" pressure curve for
				// different masks and mask types are relatively close (which makes sense, as it serves the same
				// purpose regardless of mask type).
				var maskDefinedLeakRate = MathUtil.Lerp( 19, 48, tPressure );

				outputSamples.Add( Math.Max( reportedLeak - maskDefinedLeakRate, 0 ) );
			}

			return signal;
		}

		private static Signal BuildStatsSignal( ImportSession session, string key, double minValue, double maxValue, string units, DateTime startTime, DateTime endTime )
		{
			var points = session.Stats.Where( x => x.Name == key ).ToArray();
			Debug.Assert( points.Length > 0 );

			var signal = new Signal
			{
				Name              = key,
				FrequencyInHz     = 1,
				MinValue          = minValue,
				MaxValue          = maxValue,
				UnitOfMeasurement = units,
				StartTime         = startTime,
				EndTime           = endTime,
			};

			var outputSamples = signal.Samples;
			var lastIndex     = 0;
			var currentIndex  = 0;

			for( var currentTime = startTime; currentTime < endTime; currentTime += TimeSpan.FromSeconds( signal.FrequencyInHz ) )
			{
				while( currentIndex < points.Length - 1 && points[ currentIndex ].Timestamp <= currentTime )
				{
					lastIndex    =  currentIndex;
					currentIndex += 1;
				}

				if( lastIndex == currentIndex )
				{
					outputSamples.Add( points[ currentIndex ].Value );
				}
				else
				{
					var t           = DateHelper.InverseLerp( points[ lastIndex ].Timestamp, points[ currentIndex ].Timestamp, currentTime );
					var outputValue = MathUtil.Lerp( points[ lastIndex ].Value, points[ currentIndex ].Value, t );

					outputSamples.Add( outputValue );
				}
			}

			return signal;
		}

		private static Signal BuildPressureSignal( ImportSession session, string key, DateTime startTime, DateTime endTime )
		{
			var points = session.Stats.Where( x => x.Name == key ).ToArray();
			Debug.Assert( points.Length > 0 );

			var signal = new Signal
			{
				Name              = key,
				FrequencyInHz     = 1,
				MinValue          = 0,
				MaxValue          = 30,
				UnitOfMeasurement = "cmH2O",
				StartTime         = startTime,
				EndTime           = endTime,
			};

			var outputSamples = signal.Samples;
			var lastIndex     = 0;
			var currentIndex  = 0;

			for( var currentTime = startTime; currentTime < endTime; currentTime += TimeSpan.FromSeconds( signal.FrequencyInHz ) )
			{
				while( currentIndex < points.Length - 1 && points[ currentIndex ].Timestamp <= currentTime )
				{
					lastIndex    =  currentIndex;
					currentIndex += 1;
				}

				if( lastIndex == currentIndex )
				{
					outputSamples.Add( points[ currentIndex ].Value );
				}
				else
				{
					var t           = DateHelper.InverseLerp( points[ lastIndex ].Timestamp, points[ currentIndex ].Timestamp, currentTime );
					var outputValue = MathUtil.Lerp( points[ lastIndex ].Value, points[ currentIndex ].Value, t );

					outputSamples.Add( outputValue );
				}
			}

			return signal;
		}

		private static Signal BuildFlowSignal( List<byte> samples, DateTime startTime, DateTime endTime )
		{
			var signal = new Signal
			{
				Name              = SignalNames.FlowRate,
				FrequencyInHz     = 25,
				MinValue          = -127,
				MaxValue          = 127,
				UnitOfMeasurement = "L/min",
				StartTime         = startTime,
				EndTime           = endTime,
			};

			var outputSamples = signal.Samples;
			var lastSample    = 0.0;

			for( int i = 0; i < samples.Count; i++ )
			{
				var sample = (double)(sbyte)samples[ i ];

				if( i == 0 )
				{
					outputSamples.Add( sample );
					lastSample = sample;

					continue;
				}

				for( int j = 0; j < 4; j++ )
				{
					var lerp = MathUtil.Lerp( lastSample, sample, (j + 1) * 0.2 );
					outputSamples.Add( lerp );
				}

				outputSamples.Add( sample );
				lastSample = sample;
			}

			return signal;
		}

		private static MachineIdentification MachineIdentificationFromProperties( Dictionary<string, string> properties )
		{
			var modelNumber = properties[ "ModelNumber" ];

			if( !_modelToProductName.TryGetValue( modelNumber, out string productName ) )
			{
				return null;
			}

			return new MachineIdentification
			{
				Manufacturer = MachineManufacturer.PhilipsRespironics,
				ProductName  = productName,
				SerialNumber = properties[ "SerialNumber" ],
				ModelNumber  = modelNumber
			};
		}

		private static bool TryFindPropertiesFile( string rootFolder, out string propertyFilePath )
		{
			propertyFilePath = string.Empty;

			var seriesFolder = Path.Combine( rootFolder, DATA_ROOT );
			if( !Directory.Exists( seriesFolder ) )
			{
				if( string.Equals( Path.GetFileName( rootFolder.TrimEnd( Path.DirectorySeparatorChar ) ), DATA_ROOT, StringComparison.OrdinalIgnoreCase ) )
				{
					seriesFolder = rootFolder;
				}
				else
				{
					return false;
				}
			}

			var propertyFiles = Directory.GetFiles( seriesFolder, "properties.txt", SearchOption.AllDirectories );
			if( propertyFiles.Length != 1 )
			{
				return false;
			}

			propertyFilePath = propertyFiles[ 0 ];
			return true;
		}

		private static Dictionary<string, string> ReadKeyValueFile( string path )
		{
			var fields = new Dictionary<string, string>();

			using( var input = File.OpenText( path ) )
			{
				while( !input.EndOfStream )
				{
					var line = input.ReadLine();
					if( string.IsNullOrEmpty( line ) )
					{
						break;
					}

					var parts = line.Split( '=' );
					Debug.Assert( 2 == parts.Length );

					fields[ parts[ 0 ] ] = parts[ 1 ];
				}
			}

			return fields;
		}

		#endregion

		#region Nested types

		private class MetaSession
		{
			public List<ImportSession> Sessions  { get; set; } = new List<ImportSession>();
			public DateTime            StartTime { get; set; } = DateTime.MaxValue;
			public DateTime            EndTime   { get; set; } = DateTime.MinValue;

			public void AddSession( ImportSession session )
			{
				Sessions.Add( session );

				StartTime = DateHelper.Min( StartTime, session.StartTime );
				EndTime   = DateHelper.Max( EndTime, session.EndTime );
			}

			public bool CanAdd( ImportSession session )
			{
				return session.StartTime <= EndTime.AddHours( 4 ) && (session.StartTime - StartTime).TotalHours < 24;
			}

			public override string ToString()
			{
				return $"Sessions: {Sessions.Count},  Start: {StartTime:g},   End: {EndTime:g},   Duration: {EndTime - StartTime}";
			}
		}

		private class ImportSession
		{
			public ParsedSettings      Settings { get; set; }
			public Session             Session  { get; set; }
			public List<ReportedEvent> Events   { get; set; } = new List<ReportedEvent>();
			public List<ValueAtTime>   Stats    { get; set; } = new List<ValueAtTime>();

			public DateTime StartTime { get => Session.StartTime; }

			public DateTime EndTime { get => Session.EndTime; }
			
			public TimeSpan Duration { get => Session.Duration; }

			public override string ToString()
			{
				return $"ID: {Session.ID},  Start: {StartTime:g},  End: {EndTime:g},  Duration: {EndTime - StartTime}";
			}
		}

		private enum HeaderType
		{
			Standard  = 0,
			Signal    = 1,
			MAX_VALUE = 1,
		}

		private class DataChunk
		{
			private const double SCALE = 0.1;

			public HeaderRecord Header    { get; set; }
			public byte[]       BlockData { get; set; }
			public ushort       Checksum  { get; set; }

			public static DataChunk Read( BinaryReader reader )
			{
				var startPosition = (int)reader.BaseStream.Position;

				var header = HeaderRecord.Read( reader );
				if( header == null )
				{
					return null;
				}

				var chunk = new DataChunk()
				{
					Header = header,
				};

				var checksumSize = header.DataFormatVersion == 3 ? 4 : 2;

				if( header.DataFormatVersion == 2 )
				{
					var headerSize        = (int)reader.BaseStream.Position - startPosition;
					var dataSize          = header.BlockLength - headerSize - checksumSize;
					var blockData         = reader.ReadBytes( dataSize );
					var blockChecksum     = reader.ReadUInt16();
					var calcBlockChecksum = CRC16.Calc( blockData, blockData.Length );

					if( calcBlockChecksum != blockChecksum )
					{
						throw new Exception( $"Block checksum mismatch. Expected: {calcBlockChecksum}, Actual: {blockChecksum}" );
					}

					chunk.BlockData = blockData;
					chunk.Checksum  = blockChecksum;
				}
				else
				{
					// TODO: Obtain sample data for DataFormat==3 
					// The only sample data I have available to me uses Version==2, so instead of guessing
					// that I can get Version==3 correct we will just throw an exception instead
					throw new NotSupportedException( $"Data Format Version {header.DataFormatVersion} in session {header.SessionNumber} is not yet supported" );
				}

				return chunk;
			}

			public EventImportData ReadEvents( HeaderRecord header )
			{
				var events     = new List<ReportedEvent>();
				var statistics = new List<ValueAtTime>();

				var timestamp = header.Timestamp;

				using( var reader = new BinaryReader( new MemoryStream( BlockData ) ) )
					while( reader.BaseStream.Position < reader.BaseStream.Length )
					{
						var eventCode = reader.ReadByte();

						timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );

						switch( eventCode )
						{
							case 0x01:
							{
								// Pressure set

								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.Pressure,
									Value     = reader.ReadByte() * 0.1,
								} );
							}
								break;
							case 0x02:
							{
								// Pressure set (bilevel)

								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.Pressure,
									Value     = reader.ReadByte() * 0.1,
								} );
								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.EPAP,
									Value     = reader.ReadByte() * 0.1,
								} );
							}
								break;
							case 0x03:
								// Change to Opti-Start pressure? Not imported
								reader.ReadByte();
								break;
							case 0x04:
								// Pressure pulse. Not imported.
								reader.ReadByte();
								break;
							case 0x05:
							{
								// RERA

								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.RERA,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = TimeSpan.Zero,
								} );
							}
								break;
							case 0x06:
							{
								// Obstructive Apnea

								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.ObstructiveApnea,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = TimeSpan.Zero,
								} );
							}
								break;
							case 0x07:
							{
								// Central Apnea

								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.ClearAirway,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = TimeSpan.Zero,
								} );
							}
								break;
							case 0x0A:
							{
								// Hypopnea Type 1?

								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.Hypopnea,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = TimeSpan.Zero,
								} );
							}
								break;
							case 0x0B:
							{
								// Hypopnea Type 2? Maybe "Hypopnea, with duration?"
								// Not convinced that the first parameter is a duration, but will graph it and see if it lines up.
								// TODO: Looks like these "Hypopnea type 2" events frequently (if not always) overlap with the other type of Hypopnea event. Find out what that means.

								var duration  = TimeSpan.FromSeconds( reader.ReadByte() );
								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.Hypopnea,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = duration,
								} );
							}
								break;
							case 0x0C:
							{
								// Flow Limitation

								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed;

								events.Add( new ReportedEvent
								{
									Type       = EventType.FlowLimitation,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = TimeSpan.Zero,
								} );
							}
								break;
							case 0x0D:
								// Vibratory Snore
								events.Add( new ReportedEvent
								{
									Type       = EventType.VibratorySnore,
									SourceType = SourceType.CPAP,
									StartTime  = timestamp,
									Duration   = TimeSpan.Zero,
								} );
								break;
							case 0x0E:
							{
								// Variable Breathing

								var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed - duration;

								events.Add( new ReportedEvent
								{
									Type       = EventType.VariableBreathing,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = duration,
								} );
							}
								break;
							case 0x0F:
							{
								// Periodic Breathing

								var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed - duration;

								events.Add( new ReportedEvent
								{
									Type       = EventType.PeriodicBreathing,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = duration,
								} );
							}
								break;
							case 0x10:
							{
								// Large Leak

								var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
								var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
								var startTime = timestamp - elapsed - duration;

								events.Add( new ReportedEvent
								{
									Type       = EventType.LargeLeak,
									SourceType = SourceType.CPAP,
									StartTime  = startTime,
									Duration   = duration,
								} );
							}
								break;
							case 0x11:
							{
								// Statistics 

								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.TotalLeak,
									Value     = reader.ReadByte(),
								} );

								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.Snore,
									Value     = reader.ReadByte(),
								} );

								statistics.Add( new ValueAtTime
								{
									Timestamp = timestamp,
									Name      = SignalNames.EPAP,
									Value     = reader.ReadByte() * 0.1,
								} );
							}
								break;
							case 0x12:
							{
								// Appears to be related to snore statistics, but also doesn't appear
								// to be relevant to anything I care about. Not imported.
								reader.ReadUInt16();
							}
								break;
							default:
								throw new NotSupportedException( $"Unknown event code {eventCode} in session {header.SessionNumber}" );
						}
					}

				return new EventImportData()
				{
					Events = events,
					Stats  = statistics,
				};
			}

			public ImportSummary ReadSummary( HeaderRecord header )
			{
				var timestamp = header.Timestamp;

				var settings = new ParsedSettings();
				var sessions = new List<Session>();

				DateTime? lastMaskOn = null;

				using( var reader = new BinaryReader( new MemoryStream( BlockData ) ) )
					while( reader.BaseStream.Position < reader.BaseStream.Length )
					{
						var code = reader.ReadByte();

						var blockStartPosition = reader.BaseStream.Position;

						switch( code )
						{
							case 0x00:
								// Equipment On
								ReadSettings( reader, settings );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 24 );
								break;
							case 0x01:
								// Equipment Off
								timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
								reader.ReadBytes( 5 );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
								break;
							case 0x02:
								// Mask On
								Debug.Assert( lastMaskOn == null, "Mismatched MaskOn/MaskOff" );
								timestamp  += TimeSpan.FromSeconds( reader.ReadUInt16() );
								lastMaskOn =  timestamp;
								reader.ReadBytes( 3 );
								ReadHumidifierSettings( reader, settings );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
								break;
							case 0x03:
								// Mask Off
								Debug.Assert( lastMaskOn != null, "Mismatched MaskOn/MaskOff" );
								timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
								sessions.Add( new Session()
								{
									ID         = header.SessionNumber,
									StartTime  = lastMaskOn.Value,
									EndTime    = timestamp,
									SourceType = SourceType.CPAP,
								} );
								lastMaskOn = null;
								reader.ReadBytes( 34 );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 36 );
								break;
							case 0x04:
								// Time elapsed? Time correction? Not sure. Not encountered in sample data
								timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 2 );
								break;
							case 0x05:
							case 0x06:
								// Nothing to do here? Not encountered in sample data.
								break;
							case 0x07:
								// Humidifier settings changed between one Session and another? Or in any
								// case they are stored/retrieved again only after a Session has started. 
								Debug.Assert( lastMaskOn != null );
								timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
								ReadHumidifierSettings( reader, settings );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 4 );
								break;
							case 0x08:
								// Related to Cpap-Check mode? Not seen in sample data. 
								timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
								reader.ReadBytes( 9 );
								Debug.Assert( reader.BaseStream.Position - blockStartPosition == 11 );
								break;
							default:
								throw new NotSupportedException( $"Unexpected code ({code:x}) reading chunk data in session {header.SessionNumber}" );
						}
					}

				// Merge the Sessions, because a Summary File only contains multiple sessions when a single Session
				// has been split (such as when a "No Breathing Detected" event occurs), and this is additionally 
				// handled when imported Signal data which will be similarly merged.
				var session = sessions[ 0 ];
				session.EndTime = sessions[ sessions.Count - 1 ].EndTime;

				return new ImportSummary
				{
					Settings = settings,
					Session  = session,
				};
			}

			[SuppressMessage( "ReSharper", "UnusedVariable" )]
			private static void ReadSettings( BinaryReader reader, ParsedSettings settings )
			{
				// Unknown meaning for this byte
				reader.ReadByte();

				var mode                = ReadOperatingMode( reader );
				var minPressure         = reader.ReadByte() * SCALE;
				var maxPressure         = reader.ReadByte() * SCALE;
				var pressure            = (minPressure > 0) ? minPressure : maxPressure; // Only valid for the CPAP modes
				var minPS               = reader.ReadByte() * SCALE;
				var maxPS               = reader.ReadByte() * SCALE;
				var startupMode         = reader.ReadByte();
				var rampTime            = (int)reader.ReadByte();
				var rampPressure        = reader.ReadByte() * SCALE;
				var flexMode            = ReadFlexInfo( reader, mode );
				var humidifierSettings  = ReadHumidifierSettings( reader );
				var resistanceFlags     = reader.ReadByte();
				var maskResistanceLevel = (resistanceFlags >> 3) & 0x07;
				var maskResistanceLock  = (resistanceFlags & 0x40) != 0;
				var hoseDiameter        = (resistanceFlags & 0x01) != 0 ? 15 : 22;
				var tubeTempLock        = (resistanceFlags & 0x02) != 0;
				var unknown1            = reader.ReadByte();
				var generalFlags        = reader.ReadByte();
				var autoOnEnabled       = (generalFlags & 0x40) != 0;
				var autoOffEnabled      = (generalFlags & 0x10) != 0;
				var maskAlertEnabled    = (generalFlags & 0x04) != 0;
				var showAHIEnabled      = (generalFlags & 0x02) != 0;
				var unknown2            = reader.ReadByte();
				var autoTrialDuration   = reader.ReadByte();

				settings[ SettingNames.Mode ]               = mode;
				settings[ SettingNames.MinPressure ]        = minPressure;
				settings[ SettingNames.MaxPressure ]        = maxPressure;
				settings[ SettingNames.Pressure ]           = pressure;
				settings[ SettingNames.MinPressureSupport ] = minPS;
				settings[ SettingNames.MaxPressureSupport ] = maxPS;
				settings[ SettingNames.RampTime ]           = rampTime;
				settings[ SettingNames.RampPressure ]       = rampPressure;
				settings[ SettingNames.FlexMode ]           = flexMode.Mode;
				settings[ SettingNames.FlexLock ]           = flexMode.Locked;
				settings[ SettingNames.FlexLevel ]          = flexMode.Level;
				settings[ SettingNames.HumidifierAttached ] = humidifierSettings.HumidifierPresent;
				settings[ SettingNames.HumidifierMode ]     = humidifierSettings.Mode;
				settings[ SettingNames.HumidityLevel ]      = humidifierSettings.HumidityLevel;
				settings[ SettingNames.MaskResist ]         = maskResistanceLevel;
				settings[ SettingNames.MaskResistLock ]     = maskResistanceLock;
				settings[ SettingNames.AutoOn ]             = autoOnEnabled;
				settings[ SettingNames.AutoOff ]            = autoOffEnabled;
				settings[ SettingNames.AlertMask ]          = maskAlertEnabled;
				settings[ SettingNames.ShowAHI ]            = showAHIEnabled;
				settings[ SettingNames.HoseDiameter ]       = hoseDiameter;
				settings[ SettingNames.TubeTemperature ]    = humidifierSettings.TubeTemperature;
				settings[ SettingNames.TubeTempLocked ]     = tubeTempLock;

				Debug.Assert( unknown1 == 1 );
				Debug.Assert( unknown2 == 0 );

				var reservedBytes = reader.ReadBytes( 7 );
				Debug.Assert( !reservedBytes.Any( x => x != 0 ) );
			}

			private static void ReadHumidifierSettings( BinaryReader reader, ParsedSettings settings )
			{
				var humidifierSettings = ReadHumidifierSettings( reader );

				settings[ SettingNames.HumidifierAttached ] = humidifierSettings.HumidifierPresent;
				settings[ SettingNames.HumidifierMode ]     = humidifierSettings.Mode;
				settings[ SettingNames.HumidityLevel ]      = humidifierSettings.HumidityLevel;

				if( humidifierSettings.Mode == HumidifierMode.HeatedTube )
				{
					settings[ SettingNames.TubeTemperature ] = humidifierSettings.TubeTemperature;
				}
			}

			private static HumidifierSettings ReadHumidifierSettings( BinaryReader reader )
			{
				var flags1 = reader.ReadByte();
				var flags2 = reader.ReadByte();

				var  humidityLevel     = flags1 & 0x07;
				var  tubeHumidityLevel = (flags1 >> 4) & 0x07;
				var  tubeTemp          = (flags1 >> 7) | ((flags2 & 0x03) << 1);
				bool noData            = (flags2 & 0x10) != 0;
				bool isAdaptive        = (flags2 & 0x04) != 0;
				bool heatedTubeEnabled = (flags2 & 0x08) != 0 && !isAdaptive;
				var  humidifierMode    = heatedTubeEnabled ? HumidifierMode.HeatedTube : HumidifierMode.Fixed;

				humidifierMode = isAdaptive ? HumidifierMode.Adaptive : humidifierMode;

				return new HumidifierSettings
				{
					HumidifierPresent = !noData,
					Mode              = humidifierMode,
					HumidityLevel     = humidifierMode == HumidifierMode.HeatedTube ? tubeHumidityLevel : humidityLevel,
					TubeTemperature   = tubeTemp,
				};
			}

			private static FlexSettings ReadFlexInfo( BinaryReader reader, OperatingMode operatingMode )
			{
				var flexFlags = reader.ReadByte();

				// Extract the mode flags 
				bool enabled    = (flexFlags & 0x80) != 0;
				bool locked     = (flexFlags & 0x40) != 0;
				bool plain_flex = (flexFlags & 0x20) != 0;
				bool risetime   = (flexFlags & 0x10) != 0;
				bool plusmode   = (flexFlags & 0x08) != 0;
				int  flexlevel  = flexFlags & 0x03;

				if( !enabled )
				{
					return new FlexSettings() { Mode = FlexMode.None };
				}

				FlexMode flexMode = FlexMode.Unknown;

				if( risetime )
				{
					flexMode = FlexMode.RiseTime;
				}
				else if( plain_flex )
				{
					flexMode = FlexMode.Flex;
				}
				else if( plusmode )
				{
					switch( operatingMode )
					{
						case OperatingMode.Cpap:
							flexMode = FlexMode.CFlexPlus;
							break;
						case OperatingMode.Apap:
							flexMode = FlexMode.AFlex;
							break;
						default:
							throw new NotSupportedException( $"Unexpected Flex mode {flexFlags}" );
					}
				}
				else
				{
					switch( operatingMode )
					{
						case OperatingMode.Cpap:
						case OperatingMode.Apap:
							flexMode = FlexMode.CFlex;
							break;
						case OperatingMode.BilevelFixed:
						case OperatingMode.BilevelAutoFixedPS:
						case OperatingMode.BilevelAutoVariablePS:
							flexMode = FlexMode.BiFlex;
							break;
						default:
							throw new ArgumentOutOfRangeException( nameof( operatingMode ), operatingMode, null );
					}
				}

				return new FlexSettings()
				{
					Mode   = flexMode,
					Locked = locked,
					Level  = flexlevel,
				};
			}

			private static OperatingMode ReadOperatingMode( BinaryReader reader )
			{
				var mode = reader.ReadByte();
				switch( mode )
				{
					case 0x00:
						return OperatingMode.Cpap;
					case 0x20:
						return OperatingMode.BilevelFixed;
					case 0x40:
						return OperatingMode.Apap;
					case 0x60:
						return OperatingMode.BilevelAutoVariablePS;
					case 0x80:
						return OperatingMode.Apap;
					case 0xA0:
						return OperatingMode.Cpap;
					default:
						throw new NotSupportedException( $"Uknown Operating Mode value: {mode}" );
				}
			}
		}

		private class HeaderSignalInfo
		{
			public int IntervalCount  { get; set; }
			public int IntervalLength { get; set; }

			public List<WaveformInfo> Waveforms = new List<WaveformInfo>();

			public TimeSpan Duration
			{
				get => TimeSpan.FromSeconds( IntervalCount * IntervalLength );
			}

			public class WaveformInfo
			{
				public int SampleFormat { get; set; }
				public int Interleave   { get; set; }
			}
		}

		private class HeaderRecord
		{
			private HeaderType HeaderType        { get; set; }
			public  int        DataFormatVersion { get; set; }
			public  int        BlockLength       { get; set; }
			public  int        Family            { get; set; }
			public  int        FamilyVersion     { get; set; }
			public  int        FileExtension     { get; set; }
			public  int        SessionNumber     { get; set; }
			public  DateTime   Timestamp         { get; set; }

			public HeaderSignalInfo SignalInfo { get; set; }

			public DateTime EndTimestamp { get => Timestamp + Duration; }

			public TimeSpan Duration
			{
				get
				{
					if( HeaderType == HeaderType.Signal && SignalInfo != null )
					{
						return SignalInfo.Duration;
					}

					return TimeSpan.Zero;
				}
			}

			public static HeaderRecord Read( string filename )
			{
				using( var file = File.OpenRead( filename ) )
				using( var reader = new BinaryReader( file ) )
				{
					return Read( reader );
				}
			}

			public static HeaderRecord Read( BinaryReader reader )
			{
				var startPosition = reader.BaseStream.Position;

				HeaderRecord header = null;

				var dataFormatVersion = reader.ReadByte();
				var blockLength       = reader.ReadUInt16();
				var headerType        = (HeaderType)reader.ReadByte();
				var family            = reader.ReadByte();
				var familyVersion     = reader.ReadByte();
				var fileExtension     = reader.ReadByte();
				var sessionNumber     = (int)reader.ReadUInt32();
				var timestampNum      = (int)reader.ReadUInt32();
				var timestamp         = DateHelper.UnixEpoch.AddSeconds( timestampNum ).ToLocalTime();

				if( family != 0 || familyVersion != 4 )
				{
					throw new NotSupportedException( $"This data format is not yet supported: Family {family} Version {familyVersion}" );
				}

				if( dataFormatVersion != 0x02 )
				{
					throw new NotSupportedException( $"Data format version {dataFormatVersion} in session {sessionNumber} is not supported." );
				}

				header = new HeaderRecord
				{
					DataFormatVersion = dataFormatVersion,
					BlockLength       = blockLength,
					HeaderType        = headerType,
					Family            = family,
					FamilyVersion     = familyVersion,
					FileExtension     = fileExtension,
					SessionNumber     = sessionNumber,
					Timestamp         = timestamp
				};

				if( headerType == HeaderType.Signal )
				{
					var interleavedRecordCount  = reader.ReadUInt16();
					var interleavedRecordLength = reader.ReadByte();
					var signalCount             = reader.ReadByte();

					header.SignalInfo = new HeaderSignalInfo
					{
						IntervalCount  = interleavedRecordCount,
						IntervalLength = interleavedRecordLength,
					};

					for( int i = 0; i < signalCount; i++ )
					{
						var signalType = reader.ReadByte();
						var interleave = reader.ReadUInt16();

						header.SignalInfo.Waveforms.Add( new HeaderSignalInfo.WaveformInfo
						{
							SampleFormat = signalType,
							Interleave   = interleave,
						} );
					}

					// Read terminator byte
					var terminatorByte = reader.ReadByte();
					Debug.Assert( 0 == terminatorByte );
				}

				// Now that we know the full header size, rewind the stream and read all header
				// bytes as a single array so that we can validate the checksum. 
				// NOTE: This obviously means that the base stream must support random access 
				// and that if the underlying data source is encrypted or compressed, it must 
				// first be fully decrypted/decompressed before calling this function. 
				var headerSize = (int)(reader.BaseStream.Position - startPosition);
				reader.BaseStream.Position = startPosition;
				var headerBytes = reader.ReadBytes( headerSize );

				// Calculate and verify header checksum 
				var headerCheckSum     = reader.ReadByte();
				var calcHeaderChecksum = Checksum8.Calc( headerBytes );
				if( calcHeaderChecksum != headerCheckSum )
				{
					throw new Exception( $"Header checksum mismatch for Session {sessionNumber}" );
				}

				return header;
			}
		}

		private class ParsedSettings : Dictionary<string, object>
		{
		}

		private class ImportSummary
		{
			public MachineIdentification Machine  { get; set; }
			public ParsedSettings        Settings { get; set; }
			public Session               Session  { get; set; }
		}

		private class HumidifierSettings
		{
			public bool           HumidifierPresent { get; set; }
			public HumidifierMode Mode              { get; set; }
			public int            HumidityLevel     { get; set; }
			public double         TubeTemperature   { get; set; }
		}

		private class FlexSettings
		{
			public FlexMode Mode   { get; set; }
			public bool     Locked { get; set; }
			public int      Level  { get; set; }
		}

		private class ValueAtTime
		{
			public DateTime Timestamp { get; set; }
			public string   Name      { get; set; }
			public double   Value     { get; set; }

			public override string ToString()
			{
				return $"{Name} = {Value:F2}    {Timestamp}";
			}
		}

		private class EventImportData
		{
			public List<ReportedEvent> Events { get; set; } = new List<ReportedEvent>();
			public List<ValueAtTime>   Stats  { get; set; } = new List<ValueAtTime>();

			public void Concatenate( EventImportData other )
			{
				Events.AddRange( other.Events );
				Stats.AddRange( other.Stats );
			}
		}

		#endregion
	}
}
