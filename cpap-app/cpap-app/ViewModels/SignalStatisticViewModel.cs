using System.Reflection;

using cpaplib;

namespace cpap_app.ViewModels;

public class SignalStatisticViewModel : SignalStatistics
{
	#region Public properties 
	
	public string Label { get; set; }
	
	#endregion 
	
	#region Private fields

	private static PropertyInfo[]? _typeProperties = null;
	
	#endregion 
	
	#region Constructor 

	public SignalStatisticViewModel( SignalStatistics source )
	{
		Copy( source );
		Label = source.SignalName;
	}
	
	#endregion 
	
	#region Private functions 
	
	private void Copy( SignalStatistics source )
	{
		// Copy all of the source's property values to this instance
		_typeProperties ??= typeof( SignalStatistics ).GetTypeInfo().GetProperties( BindingFlags.Instance | BindingFlags.Public );
		foreach( var prop in _typeProperties )
		{
			if( prop is { CanRead: true, CanWrite: true } )
			{
				prop.SetValue( this, prop.GetValue( source ) );
			}
		}
	}
	
	#endregion 
}
