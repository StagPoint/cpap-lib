namespace cpap_app.Importers;

using System.Collections.Generic;

public static class OximetryImporterRegistry
{
	public static List<IOximetryImporter> RegisteredImporters = new();
	
	static OximetryImporterRegistry()
	{
		RegisteredImporters.Add( new ViatomImporterCSV() );
		RegisteredImporters.Add( new ViatomDesktopImporterCSV() );
		RegisteredImporters.Add( new EmayImporterCSV() );
		//RegisteredLoaders.Add( new EdfLoader() );
	}
}
