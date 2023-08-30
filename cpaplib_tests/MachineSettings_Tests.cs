using System.Diagnostics;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class MachineSettings_Tests
{
	[TestMethod]
	public void ReadMachineSettings()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Files", "STR.edf" );
		Assert.IsTrue( File.Exists( filename ), "Test file does not exist" );

		var file = EdfFile.Open( filename );
		
		// foreach( var signal in file.Signals )
		// {
		// 	var propertyName = signal.Label.Value.Replace( '.', '_' );
		// 	Debug.WriteLine( $"{propertyName} = file.GetSignalByName(\"{signal.Label}\").Samples[ index ];" );
		// }

		var settings = new MachineSettings();
		var lookup   = new Dictionary<string, double>();

		for( int i = 0; i < file.Signals[0].Samples.Count; i++ )
		{
			for( int j = 0; j < file.Signals.Count; j++ )
			{
				lookup[ file.Signals[ j ].Label ] = file.Signals[ j ].Samples[ i ];
			}

			settings.ReadFrom( lookup );

			Debug.WriteLine( $"Test date: {settings.Date}" );
		}
	}
}
