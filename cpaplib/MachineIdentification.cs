using System.Diagnostics;

namespace cpaplib;

public class MachineIdentification
{
	public string ProductName  { get; private set; } = "";
	public string SerialNumber { get; private set; } = "";
	public string ProductCode  { get; private set; } = "";

	private Dictionary<string, string> _fields = new();

	public static MachineIdentification ReadFrom( string filename )
	{
		using( var file = File.OpenRead( filename ) )
		{
			var machine = new MachineIdentification();
			machine.ReadFrom( file );
			
			return machine;
		}
	}

	public void ReadFrom( Stream file )
	{
		using( var reader = new StreamReader( file ) )
		{
			while( !reader.EndOfStream )
			{
				var line = reader.ReadLine()?.Trim();
				if( string.IsNullOrEmpty( line ) || !line.StartsWith( "#", StringComparison.Ordinal ) )
				{
					continue;
				}

				int spaceIndex = line.IndexOf( " ", StringComparison.Ordinal );
				Debug.Assert( spaceIndex != -1 );

				var key   = line.Substring( 1, spaceIndex - 1 );
				var value = line.Substring( spaceIndex + 1 ).Trim().Replace( '_', ' ' );

				_fields[ key ] = value;
			}

			ProductName  = _fields[ "PNA" ];
			SerialNumber = _fields[ "SRN" ];
			ProductCode  = _fields[ "PCD" ];
		}
	}

	private string getField( string key )
	{
		if( _fields.TryGetValue( key, out string value ) )
		{
			return value;
		}

		return "NOT FOUND";
	}
}
