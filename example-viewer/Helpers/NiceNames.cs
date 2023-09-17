using System.Text;

namespace cpapviewer.Helpers;

internal static class NiceNames
{
	public static string Format( string value )
	{
		var buffer = new StringBuilder();

		var state = -1;

		for( int i = 0; i < value.Length; i++ )
		{
			var newState = -1;
			var ch       = value[ i ];

			if( char.IsLower( ch ) || char.IsWhiteSpace( ch ) )
			{
				newState = 1;
			}
			else if( char.IsUpper( ch ) )
			{
				newState = 2;
			}
			else if( char.IsDigit( ch ) || char.IsSeparator( ch ) || char.IsSymbol( ch ) )
			{
				newState = 3;
			}

			if( i > 0 && newState > state )
			{
				buffer.Append( ' ' );
			}

			if( !char.IsSeparator( ch ) )
			{
				buffer.Append( ch );
			}

			state = newState;
		}

		return buffer.ToString();
	}
}
