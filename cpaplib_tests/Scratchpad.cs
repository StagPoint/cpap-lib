using System.Diagnostics;
using System.Text;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class Scratchpad
{
	// [TestMethod]
	// public void RadixSortIsActuallySorting()
	// {
	// 	const int NUMBER_OF_SAMPLES = 100000;
	//
	// 	var list = new ListEx<float>( NUMBER_OF_SAMPLES * 2 );
	// 	
	// 	for( int i = 0; i < NUMBER_OF_SAMPLES; i++ )
	// 	{
	// 		list.Add( Random.Shared.Next( int.MinValue, int.MaxValue ) * 0.001f );
	// 	}
	//
	// 	var copy = new ListEx<float>( NUMBER_OF_SAMPLES );
	// 	copy.AddRange( list );
	//
	// 	var startTime = Environment.TickCount;
	// 	
	// 	RadixSort.Sort( list );
	//
	// 	var elapsedRadix = Environment.TickCount - startTime;
	//
	// 	var last = float.MinValue;
	// 	for( int i = 0; i < NUMBER_OF_SAMPLES; i++ )
	// 	{
	// 		var value = list[ i ];
	// 		Assert.IsTrue( last <= value );
	//
	// 		last = value;
	// 	}
	//
	// 	startTime = Environment.TickCount;
	//
	// 	// Now use the built-in Sort() function for comparison
	// 	copy.Sort();
	//
	// 	var elapsedBuiltIn = Environment.TickCount - startTime;
	//
	// 	// Ensure that the results are identical
	// 	for( int i = 0; i < copy.Count; i++ )
	// 	{
	// 		Assert.AreEqual( copy[ i ], list[ i ] );
	// 	}
	//
	// 	Debug.WriteLine( $"Radix: {elapsedRadix}ms    Built-In: {elapsedBuiltIn}ms" );
	// 	
	// 	// Radix sort should be faster, or in any case not slower
	// 	Assert.IsTrue( elapsedRadix <= elapsedBuiltIn );
	// }
	
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
