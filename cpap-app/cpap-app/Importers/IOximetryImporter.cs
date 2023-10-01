using System.Collections;
using System.Collections.Generic;
using System.IO;

using Avalonia.Platform.Storage;

namespace cpap_app.Importers;

public interface IOximetryImporter
{
	public string                   FriendlyName         { get; }
	public string                   Source               { get; }
	public string                   FileExtension        { get; }
	public List<FilePickerFileType> FileTypeFilters      { get; }
	public string                   FilenameMatchPattern { get; }

	public ImportedData? Load( Stream stream );
}
