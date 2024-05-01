using Avalonia.Platform.Storage;

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
        //RegisteredLoaders.Add( new ViatomBinaryImporter() );
    }

    public static List<FilePickerFileType> GetFileTypeFilters()
    {
        var filters = new List<FilePickerFileType>();

        foreach( var importer in RegisteredImporters )
        {
            filters.AddRange( importer.FileTypeFilters );
        }

        return filters;
    }

    public static List<IOximetryImporter> FindCompatibleImporters( string filename )
    {
        var result = new List<IOximetryImporter>();

        foreach( var importer in RegisteredImporters )
        {
            if( importer.FilenameMatchPattern.IsMatch( filename ) )
            {
                result.Add( importer );
            }
        }

        return result;
    }
}