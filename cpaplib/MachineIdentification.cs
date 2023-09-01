using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace cpaplib
{
	public class MachineIdentification
	{
	#region Static fields

		/// <summary>
		/// The list of known Model Numbers for the ResMed CPAP Line from 10 and up
		/// </summary>
		public static Dictionary<string, string> ModelNumbers = new Dictionary<string, string>()
		{
			{ "37201", "ResMed AirStart 10" },
			{ "37202", "ResMed AirStart 10" },
			{ "37203", "ResMed AirSense 10" },
			{ "37028", "ResMed AirSense 10 AutoSet" },
			{ "37209", "ResMed AirSense 10 AutoSet For Her" },
			{ "37382", "ResMed AirSense 10 AutoSet (Card to Cloud)" },
			{ "37205", "ResMed AirSense 10 Elite" },
			{ "37213", "ResMed AirCurve 10 S BiLevel" },
			{ "37211", "ResMed AirCurve 10 VAuto BiLevel" },
			{ "37383", "ResMed AirCurve 10 VAuto BiLevel (Card to Cloud)" },
			{ "39000", "ResMed AirSense 11 AutoSet" },
		};

	#endregion

	#region Public properties

		/// <summary>
		/// The Product Name of the machine, as reported
		/// </summary>
		public string ProductName { get; private set; } = "";

		/// <summary>
		/// The machine's Serial Number, as reported
		/// </summary>
		public string SerialNumber { get; private set; } = "";

		/// <summary>
		/// The machine's Model Number, as reported 
		/// </summary>
		public string ModelNumber { get; private set; } = "";

		/// <summary>
		/// Contains all of the other fields included in the Identification.tgt file 
		/// </summary>
		public Dictionary<string, string> Fields { get; } = new Dictionary<string, string>();

	#endregion

	#region Static functions

		public static MachineIdentification ReadFrom( string filename )
		{
			using( var file = File.OpenRead( filename ) )
			{
				return ReadFrom( file );
			}
		}

		public static MachineIdentification ReadFrom( Stream file )
		{
			var machine = new MachineIdentification();

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

					machine.Fields[ key ] = value;
				}

				machine.ProductName  = machine.Fields[ "PNA" ];
				machine.SerialNumber = machine.Fields[ "SRN" ];
				machine.ModelNumber  = machine.Fields[ "PCD" ];
			}

			return machine;
		}

	#endregion

	#region Private functions

		private string getField( string key )
		{
			if( Fields.TryGetValue( key, out string value ) )
			{
				return value;
			}

			return "NOT FOUND";
		}

	#endregion

	#region Base class overrides

		public override string ToString()
		{
			return $"{ProductName} (SN: {SerialNumber})";
		}

	#endregion
	}
}
