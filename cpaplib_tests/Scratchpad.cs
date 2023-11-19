using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class Scratchpad
{
	public static double InverseLerp( double a, double b, double v )
	{
		return (v - a) / (b - a);
	}

	[TestMethod]
	public void DateTimeFromGoogleAPI()
	{
		var startTime = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds( 1700028990000 );
		var endTime   = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds( 1700055270000 );

		var compareEndTime = new DateTime( 1970, 1, 1 ).ToLocalTime().AddMilliseconds( 1700055270000 );

		Assert.AreEqual( endTime, compareEndTime );

		Debug.WriteLine( $"Start: {startTime},    End: {endTime},    Duration: {endTime - startTime}" );
	}

	[TestMethod]
	public void ReadViatomBinaryFile()
	{
		const int HEADER_SIZE = 40;
		const int RECORD_SIZE = 5;

		string path = @"D:\Data Files\Viatom\23072C0009\20231004031747";

		Assert.IsTrue( File.Exists( path ) );

		using var file   = File.OpenRead( path );
		using var reader = new BinaryReader( file );

		int fileVersion = reader.ReadInt16();
		Assert.IsTrue( fileVersion is 3 or 5 ); // Never actually encountered FileVersion=5, but apparently someone else has and it's binary compatible

		int year = reader.ReadInt16();
		Assert.AreEqual( 2023, year );

		int month = reader.ReadByte();
		Assert.IsTrue( month >= 1 && month <= 12 );
				
		int day = reader.ReadByte();
		Assert.IsTrue( day is >= 1 and <= 31 );
				
		int hour = reader.ReadByte();
		Assert.IsTrue( hour is >= 0 and <= 24 );
				
		int minute = reader.ReadByte();
		Assert.IsTrue( minute is >= 0 and <= 60 );
				
		int second = reader.ReadByte();
		Assert.IsTrue( second is >= 0 and <= 60 );

		var expectedTimestamp = DateTime.Parse( "2023-10-04 03:17:47.000" );
		var headerTimestamp   = new DateTime( year, month, day, hour, minute, second );
		Assert.AreEqual( expectedTimestamp, headerTimestamp );

		// Read timestamp (NOTE: The filename also appears to be a timestamp. Do they always match?)
		var filenameTimestamp = DateTime.ParseExact( Path.GetFileName( path ), "yyyyMMddHHmmsss", CultureInfo.InvariantCulture );
		Assert.AreEqual( expectedTimestamp, filenameTimestamp );

		// Read and validate file size 
		int fileSize = reader.ReadInt32();
		Assert.AreEqual( file.Length, fileSize );
				
		// Read duration of recording 
		var duration = TimeSpan.FromSeconds( reader.ReadInt16() );
		Assert.AreEqual( TimeSpan.FromSeconds( 5316 ), duration );

		// Skip the rest of the header, as it doesn't provide any useful information for us. 
		file.Position = HEADER_SIZE;
		
		// The rest of the file should be an exact multiple of RECORD_SIZE
		Assert.AreEqual( 0, (fileSize - HEADER_SIZE) % RECORD_SIZE );

		int    recordCount = (fileSize - HEADER_SIZE) / RECORD_SIZE;
		double frequency   = 1.0 / (duration.TotalSeconds / recordCount);

		for( int i = 0; i < recordCount; i++ )
		{
			var spO2      = reader.ReadByte();
			var pulse     = reader.ReadByte();
			var isInvalid = reader.ReadByte();
			var motion    = reader.ReadByte();
			var vibration = reader.ReadByte();
			
			Debug.WriteLine( $"OX: {spO2}, PULSE: {pulse}, MOT: {motion}, INVALID: {isInvalid}, VIB: {vibration}" );
		}
	}

	[TestMethod]
	public void BinaryHeapForStatCalculation()
	{
		var signal = new Signal
		{
			Name              = "TestSignal",
			FrequencyInHz     = 1,
			UnitOfMeasurement = "px",
			StartTime         = DateTime.MinValue,
			EndTime           = DateTime.MaxValue
		};

		const int COUNT = 1000;

		for( int i = 0; i < COUNT; i++ )
		{
			signal.Samples.Add( i );
		}

		var calculator = new SignalStatCalculator();
		calculator.AddSignal( signal );

		var stats = calculator.CalculateStats();
		
		Assert.AreEqual( 1,     stats.Minimum ); // "Minimum" for the stats calculator means "minimum value above zero"
		Assert.AreEqual( COUNT - 1, stats.Maximum );
		Assert.AreEqual( COUNT * 0.95, stats.Percentile95 );
		Assert.AreEqual( COUNT * 0.99, stats.Percentile995 );
	}

	// [TestMethod]
	// public void BinaryHeapSortsNumbers()
	// {
	// 	const int NUMBER_OF_ELEMENTS = 256000;
	//
	// 	var heap = new BinaryHeap<double>( NUMBER_OF_ELEMENTS );
	// 	for( int i = 0; i < NUMBER_OF_ELEMENTS; i++ )
	// 	{
	// 		var sign = (Random.Shared.Next() % 2 == 0) ? 1.0 : -1.0;
	// 		heap.Enqueue( Random.Shared.NextDouble() * short.MaxValue * sign );
	// 	}
	//
	// 	var lastValue = double.MinValue;
	// 	while( heap.Count > 0 )
	// 	{
	// 		var nextValue = heap.Dequeue();
	// 		Debug.Assert( nextValue >= lastValue );
	//
	// 		lastValue = nextValue;
	// 	}
	// }

	[TestMethod]
	public void InverseLerpWorksInBothDirections()
	{
		Assert.AreEqual( 0.3, InverseLerp( 0,  10, 3 ), 0.01 );
		Assert.AreEqual( 0.7, InverseLerp( 10, 0,  3 ), 0.01 );
	}
	
	// [TestMethod]
	// public void RadixSortIsActuallySorting()
	// {
	// 	const int NUMBER_OF_SAMPLES = 100000;
	//
	// 	var sorter = new Sorter( NUMBER_OF_SAMPLES );
	//
	// 	var list = new ListEx<double>( NUMBER_OF_SAMPLES * 2 );
	// 	var copy = new ListEx<float>( NUMBER_OF_SAMPLES );
	//
	// 	for( int i = 0; i < NUMBER_OF_SAMPLES; i++ )
	// 	{
	// 		var randomValue = Random.Shared.Next( int.MinValue, int.MaxValue ) * 0.001f;
	// 		
	// 		list.Add( randomValue );
	// 		copy.Add( randomValue );
	// 	}
	//
	// 	var startTime = Environment.TickCount;
	//
	// 	sorter.AddRange( list );
	// 	var sortedList = sorter.Sort();
	//
	// 	var elapsedRadix = Environment.TickCount - startTime;
	//
	// 	var last = float.MinValue;
	// 	for( int i = 0; i < NUMBER_OF_SAMPLES; i++ )
	// 	{
	// 		var value = sortedList[ i ];
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
	// 		Assert.AreEqual( copy[ i ], sortedList[ i ] );
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
