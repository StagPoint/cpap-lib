using System.Data;
using System.Diagnostics;
using System.Text;
using System.Xml;

using cpaplib;

using DynamicData;

// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable StringLiteralTypo

namespace cpaplib_tests;

[TestClass]
public class PRS1_tests
{
	private const string SOURCE_FOLDER = @"D:\Data Files\CPAP Sample Data\REMStar Auto\P-Series\P1192913945CE";

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

	[TestMethod]
	public void PropertiesFileExistsAndCanBeParsed()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields = ReadKeyValueFile( propertyFilePath );

		Assert.AreEqual( fields[ "SerialNumber" ],     "P1192913945CE" );
		Assert.AreEqual( fields[ "ModelNumber" ],      "560P" );
		Assert.AreEqual( fields[ "ProductType" ],      "0x35" );
		Assert.AreEqual( fields[ "FirstDate" ],        "1404914403" );
		Assert.AreEqual( fields[ "LastDate" ],         "1431907200" );
		Assert.AreEqual( fields[ "PatientFolderNum" ], "8" );
		Assert.AreEqual( fields[ "PatientFileNum" ],   "430" );

		Assert.IsTrue( _modelToProductName.TryGetValue( fields[ "ModelNumber" ], out string? productName ) );
		Assert.AreEqual( "REMstar Auto (System One 60 Series)", productName );
	}

	[TestMethod]
	public void PatientFolderExists()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );

		Assert.IsTrue( Directory.Exists( patientFolderPath ) );

		Assert.IsTrue( int.TryParse( fields[ "PatientFileNum" ], out int correctFileCount ) );
		Assert.AreEqual( 430, correctFileCount );

		var dataFiles = Directory.GetFiles( patientFolderPath, "*.00?" );
		Assert.AreEqual( dataFiles.Length, correctFileCount );
	}

	[TestMethod]
	public void CanReadDataFileHeader()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );
		var dataFiles         = Directory.GetFiles( patientFolderPath, "*.00?" );

		var filename = dataFiles.FirstOrDefault( x => x.EndsWith( ".001" ) );
		Assert.IsNotNull( filename );

		using var file  = File.Open( filename, FileMode.Open );
		using var reader = new BinaryReader( file, Encoding.ASCII );

		Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

		var header = HeaderRecord.Read( reader );

		// For .001 files there should only be one chunk, so BlockLength should match file size. This
		// won't be true for .002 and .005 files which contain multiple chunks. 
		Assert.AreEqual( file.Length, header.BlockLength );
		
		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( int.Parse( fields[ "Family" ] ),                           header.Family );
		Assert.AreEqual( int.Parse( fields[ "FamilyVersion" ] ),                    header.FamilyVersion );
		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( new DateTime( 2015, 4, 12 ),                               header.Timestamp.Date );
		Assert.AreEqual( 1,                                                         header.FileExtension );
		Assert.AreEqual( int.Parse( Path.GetFileNameWithoutExtension( filename ) ), header.SessionNumber );
	}

	[TestMethod]
	public void CanReadSummaryFileChunks()
	{
		var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.001", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );

				chunk.ReadSummary( chunk.Header );
			}
		}
	}

	[TestMethod]
	public void CanReadEventFileChunks()
	{
		var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.002", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );

				chunk.ReadEvents( chunk.Header );
			}
		}
	}

	[TestMethod]
	public void CanReadWaveformFileChunks()
	{
		var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.005", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );
				
				chunk.ReadSignals( chunk.Header );
			}
		}
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

	private enum HeaderType
	{
		Standard = 0,
		Interleaved = 1,
		MAX_VALUE = 1,
	}

	private class DataChunk
	{
		private const double SCALE = 0.1;
		
		public        HeaderRecord Header    { get; set; }
		public        byte[]       BlockData { get; set; }

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

		public void ReadSignals( HeaderRecord header )
		{
			int timestamp = 0;

			using var reader = new BinaryReader( new MemoryStream( BlockData ) );

			while( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				throw new NotImplementedException();
			}
		}

		public void ReadEvents( HeaderRecord header )
		{
			int timestamp = 0;

			using var reader = new BinaryReader( new MemoryStream( BlockData ) );

			while( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				var code = reader.ReadByte();

				timestamp += reader.ReadUInt16();

				switch( code )
				{
					case 0x01:
						{
							// Pressure set
							var pressure = reader.ReadByte() * 0.1;
						}
						break;
					case 0x02:
						// Pressure set (bilevel)
						var ipap = reader.ReadByte() * 0.1;
						var epap = reader.ReadByte() * 0.1;
						break;
					case 0x03:
						// Change to Opti-Start pressure
						var newPressure = reader.ReadByte();
						break;
					case 0x04:
						// Pressure pulse
						var pressurePulseParam = reader.ReadByte();
						break;
					case 0x05:
						{
							// RERA
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed;
						}
						break;
					case 0x06:
						{
							// Obstructive Apnea
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed;
						}
						break;
					case 0x07:
						{
							// Central Apnea
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed;
						}
						break;
					case 0x0A:
						{
							// Hypopnea Type 1?
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed;
						}
						break;
					case 0x0B:
						{
							// Hypopnea Type 2?
							// TODO: Looks like these can be safely ignored? They don't seem particularly informative, as they are usually followed by the other so-called "Hypopnea"
							var unknownParameter = reader.ReadByte();
							var elapsed          = reader.ReadByte();
							var startTime        = timestamp - elapsed;
							var start            = header.Timestamp.AddSeconds( startTime );
						}
						break;
					case 0x0C:
						{
							// Flow Limitation
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed;
						}
						break;
					case 0x0D:
						// Vibratory Snore
						break;
					case 0x0E:
						{
							// // Variable Breathing
							// var duration  = reader.ReadUInt16() * 2;
							// var elapsed   = reader.ReadByte();
							// var startTime = timestamp - elapsed - duration;
							reader.Advance( 3 );
						}
						break;
					case 0x0F:
						{
							// // Periodic Breathing
							// var duration  = reader.ReadUInt16() * 2;
							// var startTime = timestamp - duration;
							reader.Advance( 3 );
						}
						break;
					case 0x10:
						{
							// Large Leak
							var duration  = reader.ReadUInt16() * 2;
							var elapsed   = reader.ReadByte();
							var startTime = timestamp - elapsed - duration;
						}
						break;
					case 0x11:
						{
							// Statistics 
							var totalLeak  = reader.ReadByte() * 0.1;
							var snoreLevel = reader.ReadByte();
							var pressure   = reader.ReadByte() * 0.1;
						}
						break;
					case 0x12:
						{
							reader.Advance( 2 );
						}
						break;
					default:
						throw new NotSupportedException( $"Unknown event code {code} in session {header.SessionNumber}" );
				}
			}
		}
		
		public void ReadSummary( HeaderRecord header )
		{
			int timestamp = 0;

			using var reader = new BinaryReader( new MemoryStream( BlockData ) );
			
			while( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				var code = reader.ReadByte();
				
				var blockStartPosition = reader.BaseStream.Position;
				
				switch( code )
				{
					case 0:
                        // Equipment On
                        ReadSettings( reader );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 24 );
						break;
					case 1:
						// Equipment Off
						timestamp += reader.ReadUInt16();
						reader.Advance( 5 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 2:
						// Mask On
						timestamp += reader.ReadUInt16();
						reader.Advance( 3 );
                        ReadHumidifierSettings( reader );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 3:
						// Mask Off
						timestamp += reader.ReadUInt16();
						reader.Advance( 34 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 36 );
						break;
					case 4:
						// Time elapsed? Not encountered in sample data
						timestamp += reader.ReadUInt16();
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 2 );
						break;
					case 5:
					case 6:
						// Nothing to do here? Not encountered in sample data.
						break;
					case 7:
						// Humidifier settings changed between one Session and another? Not seen in sample data. 
						timestamp += reader.ReadUInt16();
                        ReadHumidifierSettings( reader );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 4 );
						break;
					case 8:
						// Related to Cpap-Check mode? Not seen in sample data. 
						timestamp += reader.ReadUInt16();
						reader.Advance( 9 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 11 );
						break;
					default:
						throw new NotSupportedException( $"Unexpected code ({code:x}) reading chunk data in session {header.SessionNumber}" );
				}
			}
		}
		
		private static void ReadSettings( BinaryReader reader )
		{
			// Unknown meaning for this byte
			reader.ReadByte();
			
			var mode               = ReadOperatingMode( reader );
			var minPressure        = reader.ReadByte() * SCALE;
			var maxPressure        = reader.ReadByte() * SCALE;
			var minPS              = reader.ReadByte() * SCALE;
			var maxPS              = reader.ReadByte() * SCALE;
			var startupMode        = reader.ReadByte();
			var rampTime           = reader.ReadByte();
			var rampPressure       = reader.ReadByte() * SCALE;
			var flexMode           = ReadFlexInfo( reader, mode );
			var humidifierSettings = ReadHumidifierSettings( reader );
			
			// TODO: Which criteria is used to determine whether to proceed past this point?

			var resistanceFlags     = reader.ReadByte();
			var maskResistanceLevel = (resistanceFlags >> 3) & 0x07;
			var maskResistanceLock  = (resistanceFlags & 0x40) != 0;
			var hoseDiameter        = (resistanceFlags & 0x01) != 0 ? 15 : 22;
			var tubingLock          = (resistanceFlags & 0x02) != 0;

			var unknown1 = reader.ReadByte();
			Debug.Assert( unknown1 == 1 );

			var generalFlags     = reader.ReadByte();
			var autoOnEnabled    = (generalFlags & 0x40) != 0;
			var autoOffEnabled   = (generalFlags & 0x10) != 0;
			var maskAlertEnabled = (generalFlags & 0x04) != 0;
			var showAHIEnabled   = (generalFlags & 0x02) != 0;

			var unknown2 = reader.ReadByte();
			Debug.Assert( unknown2 == 0 );

			var autoTrialDuration = reader.ReadByte();

			var reservedBytes = reader.ReadBytes( 7 );
			Debug.Assert( !reservedBytes.Any( x => x != 0 ) );
		}
		
		private static HumidifierSettings ReadHumidifierSettings( BinaryReader reader )
		{
			var flags1 = reader.ReadByte();
			var flags2 = reader.ReadByte();

			var  humidityLevel     = flags1 & 0x07;
			var  tubeHumidityLevel = (flags1 >> 4) & 0x07;
			var  tubeTemp          = (flags1 >> 7) | ((flags2 & 3) << 1);
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
		
		public int Duration
		{
			get => IntervalCount * IntervalLength;
		}

		public class WaveformInfo
		{
			public int SampleFormat { get; set; }
			public int Interleave   { get; set; }
		}
	}

	private class HeaderRecord
	{
		public int        DataFormatVersion { get; set; }
		public int        BlockLength       { get; set; }
		public HeaderType HeaderType        { get; set; }
		public int        Family            { get; set; }
		public int        FamilyVersion     { get; set; }
		public int        FileExtension     { get; set; }
		public int        SessionNumber     { get; set; }
		public DateTime   Timestamp         { get; set; }
		
		public HeaderSignalInfo?     SignalInfo { get; set; }

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

			Console.WriteLine( $"Data format version: {dataFormatVersion}" );

			if( dataFormatVersion != 0x02 )
			{
				throw new NotSupportedException( $"Data format version {dataFormatVersion} in session {sessionNumber} is not yet supported." );
			}
			
			if( headerType == HeaderType.Interleaved )
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
}

public static class BinaryReaderExtensions
{
	public static void Advance( this BinaryReader reader, int count )
	{
		if( reader.BaseStream.Position + count > reader.BaseStream.Length )
		{
			throw new EndOfStreamException();
		}
			
		reader.BaseStream.Position += count;
	}
}
