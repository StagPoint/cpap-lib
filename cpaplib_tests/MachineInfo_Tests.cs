using System.Diagnostics;

using cpaplib;

namespace cpaplib_tests;

[TestClass]
public class MachineInfo_Tests
{
	[TestMethod]
	public void TestMethod1()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Files", "Identification.tgt" );
		Assert.IsTrue( File.Exists( filename ), "Test file does not exist" );

		var machineID = MachineIdentification.ReadFrom( filename );

		Assert.IsNotNull( machineID );
		Assert.IsTrue( machineID.ProductName.Contains( "AirSense", StringComparison.OrdinalIgnoreCase ) );
		Assert.IsTrue( !string.IsNullOrEmpty( machineID.SerialNumber ) );
		Assert.IsTrue( !string.IsNullOrEmpty( machineID.ProductCode ) );
	}
}
