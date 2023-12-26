using System;
using System.Collections.Generic;
using System.Reflection;

using cpaplib;

public class ActiveUserProfile : UserProfile
{
	#region Public properties 
	
	public List<EventType> HistoricalEvents  = new();
	public List<string>    HistoricalSignals = new();
	
	#endregion 
	
	#region Constructor

	public ActiveUserProfile( UserProfile profile )
	{
		Copy( profile );
	}
	
	#endregion 
	
	#region Private functions 
	
	private void Copy( UserProfile source )
	{
		if( source == null )
		{
			throw new ArgumentNullException( $"{nameof( source )} cannot be NULL" );
		}
		
		// Copy all of the source's property values to this instance
		var properties = typeof( UserProfile ).GetTypeInfo().GetProperties( BindingFlags.Instance | BindingFlags.Public );
		foreach( var prop in properties )
		{
			if( prop is { CanRead: true, CanWrite: true } )
			{
				prop.SetValue( this, prop.GetValue( source ) );
			}
		}
	}

	#endregion 
}