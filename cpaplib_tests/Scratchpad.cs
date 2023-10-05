using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;

using cpap_app.Helpers;

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

		var calculator = new StatCalculator();
		calculator.AddSignal( signal );

		var stats = calculator.CalculateStats();
		
		Assert.AreEqual( 1,     stats.Minimum ); // "Minimum" for the stats calculator means "minimum value above zero"
		Assert.AreEqual( COUNT - 1, stats.Maximum );
		Assert.AreEqual( COUNT * 0.95, stats.Percentile95 );
		Assert.AreEqual( COUNT * 0.99, stats.Percentile99 );
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
	
	[TestMethod]
	public void IntegerTypes()
	{
		var numType = typeof(INumber<>);
		var result  = typeof( int ).GetTypeInfo().GetInterfaces().Any( i => i.IsGenericType && (i.GetGenericTypeDefinition() == numType) );
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
