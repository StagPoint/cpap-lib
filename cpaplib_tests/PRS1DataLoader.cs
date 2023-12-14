using System.Diagnostics;

using cpap_app.Helpers;

using cpaplib;

// ReSharper disable StringLiteralTypo

namespace cpaplib_tests;

public class PRS1DataLoader
{
	#region Private fields

	private const string DATA_ROOT = "P-Series";

	private static Dictionary<string, string?> _modelToProductName = new Dictionary<string, string?>()
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

		return MachineIdenficationFromProperties( properties );
	}
	
	public List<DailyReport> LoadFromFolder( string rootFolder, DateTime? minDate = null, DateTime? maxDate = null, TimeSpan? timeAdjustment = null )
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
		
		var machineInfo = MachineIdenficationFromProperties( properties );
		if( machineInfo == null )
		{
			return null;
		}

		var startTime = Environment.TickCount;
		
		var metaSessions = ImportMetaSessions( 
			rootFolder, 
			minDate ?? DateTime.MinValue.Date,
			maxDate ?? DateTime.Today,
			timeAdjustment ?? TimeSpan.Zero,
			machineInfo 
		);

		var days = ProcessMetaSessions( metaSessions, machineInfo );

		var elapsed = Environment.TickCount - startTime;
		Console.WriteLine( $"Import took {elapsed / 1000.0:F2} seconds" );

		return days;
	}
	
	#endregion

	#region Private functions

	private static List<DailyReport> ProcessMetaSessions( List<MetaSession> metaSessions, MachineIdentification machineInfo )
	{
		List<DailyReport> days = new List<DailyReport>( metaSessions.Count );

		DailyReport? currentDay = null;

		foreach( var meta in metaSessions )
		{
			if( currentDay == null || currentDay.ReportDate != meta.StartTime.Date )
			{
				currentDay = new DailyReport
				{
					ReportDate         = meta.StartTime.Date,
					RecordingStartTime = meta.StartTime,
					RecordingEndTime   = meta.EndTime,
					MachineInfo        = machineInfo,
				};

				days.Add( currentDay );
			}

			foreach( var sesh in meta.Sessions )
			{
				currentDay.AddSession( sesh.Session );
				currentDay.Events.AddRange( sesh.Events );
			}
		}
		
		// Some of the events are apparently out of order on import. Although not seen, this could conceivably apply
		// to other timestamped collections as well, so we'll just sort them all to be certain. 
		foreach( var day in days )
		{
			day.Sessions.Sort();
			day.Events.Sort();
		}

		return days;
	}

	private static List<MetaSession> ImportMetaSessions( string rootFolder, DateTime minDate, DateTime maxDate, TimeSpan timeAdjustment, MachineIdentification machineInfo )
	{
		// Instantiate a list of "meta sessions" that will be used to group the imported sessions 
		// so that they can be assigned to the correct days. 
		var          metaSessions       = new List<MetaSession>();
		MetaSession? currentMetaSession = null;

		// Find all of the summary files and scan each one to determine whether it should be included in the import
		var summaryFiles = Directory.GetFiles( rootFolder, "*.001", SearchOption.AllDirectories );
		foreach( var filename in summaryFiles )
		{
			using var file   = File.OpenRead( filename );
			using var reader = new BinaryReader( file );

			var chunk = DataChunk.Read( reader );
			if( chunk == null )
			{
				continue;
			}

			var header = chunk.Header;
			if( header.Timestamp.Date < minDate || header.Timestamp.Date > maxDate )
			{
				continue;
			}

			// Since all timestamps are based off of the header, we only need to adjust import times in one place
			header.Timestamp += timeAdjustment;

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

		return metaSessions;
	}

	private static ImportSession ImportTherapySession( ImportSummary summary, string folder )
	{
		var sessionData = new ImportSession
		{
			Settings = summary.Settings,
			Session = summary.Session,
		};

		// Sample Events file name: 0000000003.002
		var eventFilename = Path.Combine( folder, $"{summary.Session.ID:0000000000}.002" );
		if( File.Exists( eventFilename ) )
		{
			using var eventFile   = File.OpenRead( eventFilename );
			using var eventReader = new BinaryReader( eventFile );

			while( eventFile.Position < eventFile.Length )
			{
				var eventChunk = DataChunk.Read( eventReader );
				var events     = eventChunk.ReadEvents( eventChunk.Header );

				sessionData.Events.AddRange( events.Events );
				sessionData.Stats.AddRange( events.Stats );
			}
		}
		
		// Sample Waveform file name: 0000000003.005
		var waveFormFilename = Path.Combine( folder, $"{summary.Session.ID:0000000000}.005" );
		if( File.Exists( waveFormFilename ) )
		{
			using var waveformFile   = File.OpenRead( waveFormFilename );
			using var waveformReader = new BinaryReader( waveformFile );

			var chunks = new List<DataChunk>();

			while( waveformFile.Position < waveformFile.Length )
			{
				var chunk = DataChunk.Read( waveformReader );
				chunks.Add( chunk );
			}

			var signals = ReadSignals( chunks );
			for( int i = 0; i < signals.Count; i++ )
			{
				sessionData.Session.AddSignal( signals[ i ] );
			}
		}

		return sessionData;
	}
	
	private static List<Signal> ReadSignals( List<DataChunk> chunks )
	{
		const double SAMPLE_FREQUENCY = 0.2;
		
		if( chunks.Count == 0 )
		{
			return new List<Signal>();
		}

		var samples    = new List<byte>();
		var firstChunk = chunks[ 0 ];
		var lastChunk  = chunks[ chunks.Count - 1 ];
		var duration   = lastChunk.Header.EndTimestamp - firstChunk.Header.Timestamp;
		var numSignals = firstChunk.Header.SignalInfo!.Waveforms.Count;

		Debug.Assert( numSignals == 1, "Unexpected number of signals" );
		
		DataChunk previousChunk = null;
		foreach( var chunk in chunks )
		{
			// If there is a gap between chunks, then we need to fill it
			if( previousChunk != null )
			{
				var gapLength = (chunk.Header.Timestamp - previousChunk.Header.EndTimestamp).TotalSeconds;
				if( gapLength > SAMPLE_FREQUENCY )
				{
					for( int i = 0; i < gapLength / SAMPLE_FREQUENCY; i++ )
					{
						samples.Add( 0 );
					}
				}
				else if( gapLength < -1 )
				{
					// There is an extremely rare but annoying issue in Session 551 of the sample data where the 
					// timestamp of the first chunk is *wildly* incorrect. On the assumption that if it happened
					// once it can happen again, we'll try to work around it.
					// It might be better to just throw away this Session, though.  
					previousChunk.Header.Timestamp = chunk.Header.Timestamp - previousChunk.Header.Duration;

					// Recalculate the Session duration to match the new timestamps 
					duration = lastChunk.Header.EndTimestamp - firstChunk.Header.Timestamp;
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
		Assert.AreEqual( SAMPLE_FREQUENCY, sampleRate, 0.001 );
		
		var signal = new Signal
		{
			Name              = SignalNames.FlowRate,
			FrequencyInHz     = sampleRate,
			MinValue          = -127,
			MaxValue          = 127,
			UnitOfMeasurement = "L/min",
			StartTime         = firstChunk.Header.Timestamp,
			EndTime           = previousChunk.Header.EndTimestamp,
		};
		
		signal.Samples.AddRange( samples.Select( x => (double)(sbyte)x ) );
		
		return new List<Signal> { signal };
	}

	private static MachineIdentification MachineIdenficationFromProperties( Dictionary<string, string> properties )
	{
		var modelNumber = properties[ "ModelNumber" ];

		if( !_modelToProductName.TryGetValue( modelNumber, out string? productName ) )
		{
			return null;
		}

		return new MachineIdentification
		{
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

	private static Dictionary<string, string> ReadKeyValueFile( string path, string separator = "=" )
	{
		var fields = new Dictionary<string, string>();

		using var input = File.OpenText( path );

		while( !input.EndOfStream )
		{
			var line = input.ReadLine();
			if( string.IsNullOrEmpty( line ) )
			{
				break;
			}

			var parts = line.Split( separator );
			Assert.AreEqual( 2, parts.Length );

			fields[ parts[ 0 ] ] = parts[ 1 ];
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
		public List<ReportedEvent> Events   { get; init; } = new List<ReportedEvent>();
		public List<ValueAtTime>   Stats    { get; init; } = new List<ValueAtTime>();
		
		public DateTime StartTime { get { return Session.StartTime; } }
		public DateTime EndTime   { get { return Session.EndTime; } }

		public override string ToString()
		{
			return $"ID: {Session.ID},  Start: {StartTime:g},  End: {EndTime:g},  Duration: {EndTime - StartTime}";
		}
	}
	
	private enum HeaderType
	{
		Standard = 0,
		Signal = 1,
		MAX_VALUE = 1,
	}

	private class DataChunk
	{
		private const double SCALE = 0.1;
		
		public HeaderRecord Header    { get; set; }
		public byte[]       BlockData { get; set; }
		public ushort       Checksum  { get; set; }

		public static DataChunk? Read( BinaryReader reader )
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

			using var reader = new BinaryReader( new MemoryStream( BlockData ) );

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
								Name      = SignalNames.LeakRate,
								Value     = reader.ReadByte() * 0.1,
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
								Name      = SignalNames.Pressure,
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
				Stats = statistics,
			};
		}
		
		public ImportSummary ReadSummary( HeaderRecord header )
		{
			var timestamp = header.Timestamp;

			var settings = new ParsedSettings();
			var sessions = new List<Session>();

			DateTime? lastMaskOn = null;
			var       startTime  = timestamp;
			var       endTime    = timestamp;

			using var reader = new BinaryReader( new MemoryStream( BlockData ) );
			
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
						endTime   =  timestamp;
						reader.Advance( 5 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 0x02:
						// Mask On
						Debug.Assert( lastMaskOn == null, "Mismatched MaskOn/MaskOff" );
						timestamp  += TimeSpan.FromSeconds( reader.ReadUInt16() );
						lastMaskOn =  timestamp;
						var maskOnParams = reader.ReadBytes( 3 );
                        ReadHumidifierSettings( reader, settings );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 0x03:
						// Mask Off
						Debug.Assert( lastMaskOn != null, "Mismatched MaskOn/MaskOff" );
						timestamp  += TimeSpan.FromSeconds( reader.ReadUInt16() );
						sessions.Add( new Session()
						{
							ID         = header.SessionNumber,
							StartTime  = lastMaskOn.Value,
							EndTime    = timestamp,
							SourceType = SourceType.CPAP,
						} );
						lastMaskOn =  null;
						reader.Advance( 34 );
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
						reader.Advance( 9 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 11 );
						break;
					default:
						throw new NotSupportedException( $"Unexpected code ({code:x}) reading chunk data in session {header.SessionNumber}" );
				}
			}

			// In the sample data that I have there may be one or two Sessions per file, but never zero.
			Assert.IsTrue( sessions.Count > 0, "No Sessions found" );
			Assert.AreEqual( startTime, sessions[ 0 ].StartTime );
			
			// It's not very common, but the Equipment Off and final Mask Off times are not always equal.
			// Equipment Off is however still always equal to or later than final Mask Off. 
			Assert.IsTrue( endTime >= sessions[ ^1 ].EndTime );

			// Merge the Sessions, because a Summary File only contains multiple sessions when a single Session
			// has been split (such as when a "No Breathing Detected" event occurs), and this is additionally 
			// handled when imported Signal data which will be similarly merged.
			var session = sessions[ 0 ];
			session.EndTime = sessions[ sessions.Count - 1 ].EndTime;

			return new ImportSummary
			{
				Settings = settings,
				Session = session,
			};
		}
		
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
			var rampTime            = reader.ReadByte();
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
			settings[ SettingNames.Humidifier ]         = humidifierSettings.HumidifierPresent;
			settings[ SettingNames.HumidifierMode ]     = humidifierSettings.Mode;
			settings[ SettingNames.HumidityLevel ]      = humidifierSettings.HumidityLevel;
			settings[ SettingNames.MaskResist ]         = maskResistanceLevel;
			settings[ SettingNames.MaskResistLock ]     = maskResistanceLock;
			settings[ SettingNames.AutoOn ]             = autoOnEnabled;
			settings[ SettingNames.AutoOff ]            = autoOffEnabled;
			settings[ SettingNames.AlertMask ]          = maskAlertEnabled;
			settings[ SettingNames.ShowAHI ]            = showAHIEnabled;
			settings[ SettingNames.HoseDiameter ]       = hoseDiameter;

			if( mode == OperatingMode.AutoTrial )
			{
				settings[ SettingNames.AutoTrialDuration ] = autoTrialDuration;
			}

			if( humidifierSettings.Mode == HumidifierMode.HeatedTube )
			{
				settings[ SettingNames.TubeTemperature ] = humidifierSettings.TubeTemperature;
				settings[ SettingNames.TubeTempLocked ]  = tubeTempLock;
			}

			Debug.Assert( unknown1 == 1 );
			Debug.Assert( unknown2 == 0 );

			var reservedBytes = reader.ReadBytes( 7 );
			Debug.Assert( !reservedBytes.Any( x => x != 0 ) );
		}

		private static void ReadHumidifierSettings( BinaryReader reader, ParsedSettings settings )
		{
			var humidifierSettings = ReadHumidifierSettings( reader );
			
			settings[ SettingNames.Humidifier ]     = humidifierSettings.HumidifierPresent;
			settings[ SettingNames.HumidifierMode ] = humidifierSettings.Mode;
			settings[ SettingNames.HumidityLevel ]  = humidifierSettings.HumidityLevel;
			
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
				flexMode = operatingMode switch
				{
					OperatingMode.CPAP       => FlexMode.CFlexPlus,
					OperatingMode.CPAP_Check => FlexMode.CFlexPlus,
					OperatingMode.AutoCPAP   => FlexMode.AFlex,
					OperatingMode.AutoTrial  => FlexMode.AFlex,
					_                        => throw new NotSupportedException( $"Unexpected Flex mode {flexFlags}" )
				};
			}
			else
			{
				flexMode = operatingMode switch
				{
					OperatingMode.CPAP_Check  => FlexMode.CFlex,
					OperatingMode.CPAP        => FlexMode.CFlex,
					OperatingMode.AutoCPAP    => FlexMode.CFlex,
					OperatingMode.AutoTrial   => FlexMode.CFlex,
					OperatingMode.Bilevel     => FlexMode.BiFlex,
					OperatingMode.AutoBilevel => FlexMode.BiFlex,
					_                         => throw new ArgumentOutOfRangeException( nameof( operatingMode ), operatingMode, null )
				};
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
			return mode switch
			{
				0x00 => OperatingMode.CPAP,
				0x20 => OperatingMode.Bilevel,
				0x40 => OperatingMode.AutoCPAP,
				0x60 => OperatingMode.AutoBilevel,
				0x80 => OperatingMode.AutoTrial,
				0xA0 => OperatingMode.CPAP_Check,
				_    => throw new NotSupportedException( $"Uknown Operating Mode value: {mode}" )
			};
		}
	}

	private class HeaderSignalInfo
	{
		public int IntervalCount  { get; set; }
		public int IntervalLength { get; set; }

		public List<WaveformInfo> Waveforms = new();
		
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
		public HeaderType HeaderType        { get; set; }
		public int        DataFormatVersion { get; set; }
		public int        BlockLength       { get; set; }
		public int        Family            { get; set; }
		public int        FamilyVersion     { get; set; }
		public int        FileExtension     { get; set; }
		public int        SessionNumber     { get; set; }
		public DateTime   Timestamp         { get; set; }

		public HeaderSignalInfo? SignalInfo { get; set; }

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

		public static HeaderRecord? Read( string filename )
		{
			using var file   = File.OpenRead( filename );
			using var reader = new BinaryReader( file );

			return Read( reader );
		}

		public static HeaderRecord? Read( BinaryReader reader )
		{
			var startPosition = reader.BaseStream.Position;

			HeaderRecord? header = null;

			var dataFormatVersion = reader.ReadByte();
			var blockLength       = reader.ReadUInt16();
			var headerType        = (HeaderType)reader.ReadByte();
			var family            = reader.ReadByte();
			var familyVersion     = reader.ReadByte();
			var fileExtension     = reader.ReadByte();
			var sessionNumber     = (int)reader.ReadUInt32();
			var timestampNum      = (int)reader.ReadUInt32();
			var timestamp         = DateTime.UnixEpoch.AddSeconds( timestampNum ).ToLocalTime();

			if( family != 0 || familyVersion != 4 )
			{
				throw new NotSupportedException( $"This data format is not yet supported: Family {family} Version {familyVersion}" );
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

			if( dataFormatVersion != 0x02 )
			{
				throw new NotSupportedException( $"Data format version {dataFormatVersion} in session {sessionNumber} is not yet supported." );
			}
			
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
				throw new Exception( $"Header checksum mismatch. Expected: {calcHeaderChecksum}, Actual: {headerCheckSum}" );
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

	private enum HumidifierMode
	{
		Fixed, 
		Adaptive, 
		HeatedTube, 
		Passover, 
		Error,
	}

	private class FlexSettings
	{
		public FlexMode Mode   { get; set; }
		public bool     Locked { get; set; }
		public int      Level  { get; set; }
	}

	private enum FlexMode
	{
		Unknown = -1,
		None, 
		CFlex,
		CFlexPlus,
		AFlex,
		RiseTime, 
		BiFlex,
		PFlex, 
		Flex, 
	};

	private enum OperatingMode
	{
		UNKNOWN    = -1,
		CPAP_Check = 0,
		CPAP,
		AutoCPAP,
		AutoTrial,
		Bilevel,
		AutoBilevel,
		ASV,
		S,
		ST,
		PC,
		ST_AVAPS,
		PC_AVAPS,
	};

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
		public List<ReportedEvent> Events { get; init; } = new List<ReportedEvent>();
		public List<ValueAtTime>   Stats  { get; init; } = new List<ValueAtTime>();
		
		public void Concatenate( EventImportData other )
		{
			Events.AddRange( other.Events );
			Stats.AddRange( other.Stats );
		}
	}

	#endregion 
}
