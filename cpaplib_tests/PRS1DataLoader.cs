using cpaplib;

// ReSharper disable StringLiteralTypo

namespace cpaplib_tests;

public class PRS1DataLoader
{
	#region Private fields

	private const string DATA_ROOT = "P-Series";

	private static Dictionary<string, string?> _modelToProductName = new Dictionary<string, string?>()
	{
		{ "760P", "BiPAP Auto (System One 60 Series)" },
		{ "761P", "BiPAP Auto (System One 60 Series)" },
		{ "750P", "BiPAP Auto (System One)" },
		{ "960P", "BiPAP autoSV Advanced (System One 60 Series)" },
		{ "961P", "BiPAP autoSV Advanced (System One 60 Series)" },
		{ "960T", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
		{ "961TCA", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
		{ "950P", "BiPAP AutoSV Advanced System One" },
		{ "951P", "BiPAP AutoSV Advanced System One" },
		{ "1160P", "BiPAP AVAPS 30 (System One 60 Series)" },
		{ "660P", "BiPAP Pro (System One 60 Series)" },
		{ "650P", "BiPAP Pro (System One)" },
		{ "1061401", "BiPAP S/T (C Series)" },
		{ "1061T", "BiPAP S/T 30 (System One 60 Series)" },
		{ "501V", "Dorma 500 Auto (System One 60 Series)" },
		{ "420X150C", "DreamStation 2 Advanced CPAP" },
		{ "410X150C", "DreamStation 2 CPAP" },
		{ "700X110", "DreamStation Auto BiPAP" },
		{ "700X120", "DreamStation Auto BiPAP" },
		{ "700X130", "DreamStation Auto BiPAP" },
		{ "700X150", "DreamStation Auto BiPAP" },
		{ "500X110", "DreamStation Auto CPAP" },
		{ "500X120", "DreamStation Auto CPAP" },
		{ "500X130", "DreamStation Auto CPAP" },
		{ "500X150", "DreamStation Auto CPAP" },
		{ "500X180", "DreamStation Auto CPAP" },
		{ "500X140", "DreamStation Auto CPAP with A-Flex" },
		{ "501X120", "DreamStation Auto CPAP with P-Flex" },
		{ "900X110", "DreamStation BiPAP autoSV" },
		{ "900X120", "DreamStation BiPAP autoSV" },
		{ "900X150", "DreamStation BiPAP autoSV" },
		{ "1130X110", "DreamStation BiPAP AVAPS 30" },
		{ "1130X200", "DreamStation BiPAP AVAPS 30" },
		{ "1131X150", "DreamStation BiPAP AVAPS 30 AE" },
		{ "600X110", "DreamStation BiPAP Pro" },
		{ "600X150", "DreamStation BiPAP Pro" },
		{ "1030X110", "DreamStation BiPAP S/T 30" },
		{ "1030X150", "DreamStation BiPAP S/T 30 with AAM" },
		{ "200X110", "DreamStation CPAP" },
		{ "400X110", "DreamStation CPAP Pro" },
		{ "400X120", "DreamStation CPAP Pro" },
		{ "400X130", "DreamStation CPAP Pro" },
		{ "400X150", "DreamStation CPAP Pro" },
		{ "401X150", "DreamStation CPAP Pro with Auto-Trial" },
		{ "400G110", "DreamStation Go" },
		{ "500G110", "DreamStation Go Auto" },
		{ "500G120", "DreamStation Go Auto" },
		{ "500G150", "DreamStation Go Auto" },
		{ "502G150", "DreamStation Go Auto" },
		{ "560P", "REMstar Auto (System One 60 Series)" },
		{ "560PBT", "REMstar Auto (System One 60 Series)" },
		{ "561P", "REMstar Auto (System One 60 Series)" },
		{ "562P", "REMstar Auto (System One 60 Series)" },
		{ "550P", "REMstar Auto (System One)" },
		{ "551P", "REMstar Auto (System One)" },
		{ "552P", "REMstar Auto (System One)" },
		{ "261CA", "REMstar Plus (System One 60 Series)" },
		{ "261P", "REMstar Plus (System One 60 Series)" },
		{ "251P", "REMstar Plus (System One)" },
		{ "460P", "REMstar Pro (System One 60 Series)" },
		{ "460PBT", "REMstar Pro (System One 60 Series)" },
		{ "461CA", "REMstar Pro (System One 60 Series)" },
		{ "461P", "REMstar Pro (System One 60 Series)" },
		{ "462P", "REMstar Pro (System One 60 Series)" },
		{ "450P", "REMstar Pro (System One)" },
		{ "451P", "REMstar Pro (System One)" },
		{ "452P", "REMstar Pro (System One)" },
	};

	#endregion

	#region Public API

	public static bool HasCorrectFolderStructure( string rootFolder )
	{
		return TryFindPropertiesFile( rootFolder, out _ );
	}

	public static MachineIdentification LoadMachineIdentificationInfo( string rootFolder )
	{
		if( !TryFindPropertiesFile( rootFolder, out string propertyFilePath ) )
		{
			return null;
		}

		var properties = ReadKeyValueFile( propertyFilePath );
		if( properties == null || properties.Count == 0 )
		{
			return null;
		}

		var modelNumber = properties[ "ModelNumber" ];

		if( !_modelToProductName.TryGetValue( modelNumber, out string? productName ) )
		{
			return null;
		}

		return new MachineIdentification
		{
			ProductName  = productName,
			SerialNumber = properties[ "SerialNumber" ],
			ModelNumber  = modelNumber
		};
	}

	public List<DailyReport> LoadFromFolder( string folderPath, DateTime? minDate = null, DateTime? maxDate = null, TimeSpan? timeAdjustment = null )
	{
		throw new NotImplementedException();
	}

	#endregion

	#region Private functions

	private static bool TryFindPropertiesFile( string rootFolder, out string propertyFilePath )
	{
		propertyFilePath = string.Empty;

		var seriesFolder = Path.Combine( rootFolder, DATA_ROOT );
		if( !Directory.Exists( seriesFolder ) )
		{
			if( string.Equals( Path.GetFileName( rootFolder.TrimEnd( Path.DirectorySeparatorChar ) ), DATA_ROOT, StringComparison.OrdinalIgnoreCase ) )
			{
				seriesFolder = rootFolder;
			}
			else
			{
				return false;
			}
		}

		var propertyFiles = Directory.GetFiles( seriesFolder, "properties.txt", SearchOption.AllDirectories );
		if( propertyFiles.Length != 1 )
		{
			return false;
		}

		propertyFilePath = propertyFiles[ 0 ];
		return true;
	}

	private static Dictionary<string, string> ReadKeyValueFile( string path, string separator = "=" )
	{
		var fields = new Dictionary<string, string>();

		using var input = File.OpenText( path );

		while( !input.EndOfStream )
		{
			var line = input.ReadLine();
			if( string.IsNullOrEmpty( line ) )
			{
				break;
			}

			var parts = line.Split( separator );
			Assert.AreEqual( 2, parts.Length );

			fields[ parts[ 0 ] ] = parts[ 1 ];
		}

		return fields;
	}

	#endregion
}
