using System.Diagnostics;
using System.Text;

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
		Assert.AreEqual( new DateTime( 2015, 4, 13 ),                               header.Timestamp.Date );
		Assert.AreEqual( 1,                                                         header.FileExtension );
		Assert.AreEqual( int.Parse( Path.GetFileNameWithoutExtension( filename ) ), header.SessionNumber );
	}

	[TestMethod]
	public void CanReadSummaryFileChunks()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );
		var dataFiles         = Directory.GetFiles( patientFolderPath, "*.00?" );

		foreach( var filename in dataFiles.Where( x => x.EndsWith( ".001" ) ) )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

			var chunk = DataChunk.Read( reader );
			Assert.IsNotNull( chunk );
		}
	}

	[TestMethod]
	public void CanReadWaveformFileChunks()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );
		var dataFiles         = Directory.GetFiles( patientFolderPath, "*.00?" );

		foreach( var filename in dataFiles.Where( x => x.EndsWith( ".005" ) ) )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

			var chunk = DataChunk.Read( reader );
			Assert.IsNotNull( chunk );
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
		public HeaderRecord Header { get; set; }

		public static DataChunk? Read( BinaryReader reader )
		{
			var startPosition = (int)reader.BaseStream.Position;

			var header = HeaderRecord.Read( reader );
			if( header == null )
			{
				return null;
			}
			
			if( header.HeaderType == HeaderType.Standard )
			{
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
				}
				else if( header.DataFormatVersion == 3 )
				{
					throw new NotImplementedException();
				}
			}

			return new DataChunk()
			{
				Header = header,
			};
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

		public static HeaderRecord? Read( BinaryReader reader )
		{
			var headerBytes = reader.ReadBytes( 15 );
			if( headerBytes.Length < 15 )
			{
				return null;
			}

			// Because we need to calculate the header checksum and the source stream may not support
			// random access (such as an encrypted file stream), we'll first read in the raw header 
			// bytes and then create our own binary reader to read *that*.
			using var headerReader = new BinaryReader( new MemoryStream( headerBytes ) );
			
			var dataFormatVersion = headerReader.ReadByte();
			var blockLength       = headerReader.ReadUInt16();
			var headerType        = (HeaderType)headerReader.ReadByte();
			var family            = headerReader.ReadByte();
			var familyVersion     = headerReader.ReadByte();
			var fileExtension     = headerReader.ReadByte();
			var sessionNumber     = (int)headerReader.ReadUInt32();
			var timestampNum      = (int)headerReader.ReadUInt32();
			var timestamp         = DateTime.UnixEpoch.AddSeconds( timestampNum );

			if( headerType == HeaderType.Interleaved )
			{
				var extra = reader.ReadBytes( 4 );
				headerBytes = headerBytes.Concat( extra ).ToArray();

				var dataRecordCount  = extra[ 0 ] | extra[ 1 ] << 8;
				var dataRecordLength = extra[ 2 ];
				var signalCount      = extra[ 3 ];

				// Read the variable-length data + trailing byte.
				int entrySize      = (dataFormatVersion == 3) ? 4 : 3;
				int signalDataSize = signalCount * entrySize;

				extra       = reader.ReadBytes( signalDataSize );
				headerBytes = headerBytes.Concat( extra ).ToArray();

				using var extraReader = new BinaryReader( new MemoryStream( extra ) );

				for( int i = 0; i < signalCount; i++ )
				{
					var signalType = extraReader.ReadByte();
					var interleave = extraReader.ReadUInt16();

					if( dataFormatVersion == 3 )
					{
						extraReader.ReadByte();
					}
				}
				
				// Read terminator byte
				var terminatorByte = reader.ReadByte();
				Debug.Assert( 0 == terminatorByte );
			}
			
			// Calculate and verify header checksum 
			var headerCheckSum     = reader.ReadByte();
			var calcHeaderChecksum = Checksum8.Calc( headerBytes );
			if( calcHeaderChecksum != headerCheckSum )
			{
				throw new Exception( $"Header checksum mismatch. Expected: {calcHeaderChecksum}, Actual: {headerCheckSum}" );
			}

			return new HeaderRecord
			{
				DataFormatVersion = dataFormatVersion,
				BlockLength       = blockLength,
				HeaderType          = headerType,
				Family            = family,
				FamilyVersion     = familyVersion,
				FileExtension     = fileExtension,
				SessionNumber     = sessionNumber,
				Timestamp         = timestamp
			};
		}
	}
}
