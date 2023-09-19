using System.Diagnostics;

using cpaplib;

namespace cpaplib_tests;

[TestClass]
public class MachineInfo_Tests
{
	[TestMethod]
	public void ReadMachineIdentificationFile()
	{
		string testFolder = Path.Combine( Environment.CurrentDirectory, "Files" );

		var machineID = ResMedDataLoader.LoadMachineIdentificationInfo( testFolder );

		Assert.IsNotNull( machineID );
		Assert.IsTrue( machineID.ProductName.Contains( "AirSense", StringComparison.OrdinalIgnoreCase ) );
		Assert.IsTrue( !string.IsNullOrEmpty( machineID.SerialNumber ) );
		Assert.IsTrue( !string.IsNullOrEmpty( machineID.ModelNumber ) );
	}
}
