using System.Diagnostics;
using System.Text;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class Scratchpad
{
	[TestMethod]
	public void AnonymizeFiles()
	{
		var allFilenames = Directory.GetFiles( Path.Combine( Environment.CurrentDirectory, "Files" ), "*.edf", SearchOption.AllDirectories );
		foreach( var filename in allFilenames )
		{
			var header = new EdfFileHeader();
			
			using( var file = File.OpenRead( filename ) )
			{
				var reader = new BinaryReader( file, Encoding.ASCII, true );
				header.ReadFrom( reader );
			}

			var serialNumberPosition = header.RecordingIdentification.Value.IndexOf( "SRN=", StringComparison.Ordinal );
			if( serialNumberPosition != -1 )
			{
				Debug.WriteLine( $"ANONYMIZE: {header.RecordingIdentification}" );

				header.RecordingIdentification.Value = header.RecordingIdentification.Value.Substring( 0, serialNumberPosition - 1 );

				using( var output = File.Open( filename, FileMode.Open ) )
				{
					var writer = new BinaryWriter( output );
					header.WriteTo( writer );
				}
			}
		}
	}
}
