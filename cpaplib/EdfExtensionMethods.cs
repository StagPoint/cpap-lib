using System;
using System.Collections.Generic;
using System.Numerics;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public static class EdfExtensionMethods
	{
		public static EdfStandardSignal GetSignalByName( this EdfFile file, params string[] labels )
		{
			// This isn't possible under normal usage, but...
			if( labels == null || labels.Length == 0 )
			{
				throw new ArgumentException( nameof( labels ) );
			}

			foreach( var label in labels )
			{
				var signal = file.GetSignalByName( label ) as EdfStandardSignal;
				if( signal != null )
				{
					return signal;
				}
			}

			throw new KeyNotFoundException( $"Failed to find a signal named '{labels[ 0 ]}" );
		}

		public static double GetValue( this Dictionary<string, double> map, params string[] keys )
		{
			foreach( var key in keys )
			{
				if( map.TryGetValue( key, out double value ) )
				{
					return value;
				}
			}

			throw new KeyNotFoundException( $"Failed to find a value named '{keys[ 0 ]}" );
		}
	}
}
