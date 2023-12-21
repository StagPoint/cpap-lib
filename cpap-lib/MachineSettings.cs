using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace cpaplib
{
	public class MachineSettings : IEnumerable<KeyValuePair<string, object>>
	{
		#region Public properties

		public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

		#endregion

		#region Indexer properties

		public object this[ string key ]
		{
			get { return Values[ key ]; }
			set { Values[ key ] = value; }
		}

		#endregion

		#region Public functions

		public bool TryGetValue<T>( string key, out T value )
		{
			if( !Values.TryGetValue( key, out object storedValue ) )
			{
				value = default( T );
				return false;
			}

			value = (T)storedValue;
			return true;
		}

		public T GetValue<T>( string key )
		{
			return (T)Values[ key ];
		}

		#endregion

		#region IEnumerable interface implementation

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		#endregion
	}
}
