using System.Text;

using cpaplib;

using DynamicData;
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace cpaplib_tests;

[TestClass]
public class PRS1_tests
{
	private static string SOURCE_FOLDER = @"D:\Data Files\CPAP Sample Data\P-Series\P1192913945CE";
	
	[TestMethod]
	public void PropertiesFileExistsAndCanBeParsed()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );
		Assert.IsTrue( File.Exists( propertyFilePath ) );

		var fields = ReadKeyValueFile( propertyFilePath );

		Assert.AreEqual( fields[ "SerialNumber" ], "P1192913945CE" );
		Assert.AreEqual( fields[ "ModelNumber" ], "560P" );
		Assert.AreEqual( fields[ "ProductType" ], "0x35" );
		Assert.AreEqual( fields[ "FirstDate" ], "1404914403" );
		Assert.AreEqual( fields[ "LastDate" ], "1431907200" );
		Assert.AreEqual( fields[ "PatientFolderNum" ], "8" );
		Assert.AreEqual( fields[ "PatientFileNum" ], "430" );
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
		var dataFiles = Directory.GetFiles( patientFolderPath, "*.00?" );

		var filename = dataFiles.FirstOrDefault( x => x.EndsWith( ".001" ) );
		Assert.IsNotNull( filename );

		using var input  = File.Open( filename, FileMode.Open );
		using var reader = new BinaryReader( input, Encoding.UTF8 );

		Assert.IsTrue( input.Length >= 15, "Headers are 15 bytes in length" );

		var header = FileHeader.Read( reader );

		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( int.Parse( fields[ "Family" ] ),                           header.Family );
		Assert.AreEqual( int.Parse( fields[ "FamilyVersion" ] ),                    header.FamilyVersion );
		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( new DateTime( 2015, 4, 13 ),                               header.Timestamp.Date );
		Assert.AreEqual( 1,                                                         header.FileExtension );
		Assert.AreEqual( int.Parse( Path.GetFileNameWithoutExtension( filename ) ), header.SessionNumber );
	}

	private Dictionary<string, string> ReadKeyValueFile( string path, string separator = "=" )
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

			var separatorIndex = line.IndexOf( separator, StringComparison.Ordinal );
			Assert.IsTrue( separatorIndex >= 0 );

			var key   = line.Substring( 0, separatorIndex );
			var value = line.Substring( separatorIndex + 1 );

			fields[ key ] = value;
		}

		return fields;
	}

	private class FileHeader
	{
		public int      DataFormatVersion { get; set; }
		public int      BlockLength       { get; set; }
		public int      FileType          { get; set; }
		public int      Family            { get; set; }
		public int      FamilyVersion     { get; set; }
		public int      FileExtension     { get; set; }
		public int      SessionNumber     { get; set; }
		public DateTime Timestamp         { get; set; }

		public static FileHeader Read( BinaryReader reader )
		{
			int dataFormatVersion = reader.ReadByte();
			int blockLength       = reader.ReadUInt16();
			int fileType          = reader.ReadByte();
			int family            = reader.ReadByte();
			int familyVersion     = reader.ReadByte();
			int fileExtension     = reader.ReadByte();
			int sessionNumber     = (int)reader.ReadUInt32();
			int timestampNum      = (int)reader.ReadUInt32();
			var timestamp         = DateTime.UnixEpoch.AddSeconds( timestampNum );

			return new FileHeader
			{
				DataFormatVersion = dataFormatVersion,
				BlockLength       = blockLength,
				FileType          = fileType,
				Family            = family,
				FamilyVersion     = familyVersion,
				FileExtension     = fileExtension,
				SessionNumber     = sessionNumber,
				Timestamp         = timestamp
			};
		}
	}
}
