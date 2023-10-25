using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using cpap_app.Configuration;
using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.ViewModels;

public class EventTypesIncludedViewModel
{
	public List<EventTypeIncludedItem> Items { get; set; } = new();

	public SignalChartConfiguration Config { get; set; }

	public EventTypesIncludedViewModel( SignalChartConfiguration config )
	{
		Config = config;

		var allEventTypes = EventTypes.RespiratoryDisturbance.Concat( EventTypes.OxygenSaturation.Concat( EventTypes.Pulse ) );
		foreach( var eventType in allEventTypes )
		{
			var item = new EventTypeIncludedItem()
			{
				// TODO: The label of the event type should be retrieved from configuration data 
				Label = NiceNames.Format( eventType.ToString() ),
				
				EventType = eventType,
				IsIncluded = config.DisplayedEvents.Contains( eventType )
			};

			item.PropertyChanged += ( sender, args ) =>
			{
				if( !item.IsIncluded )
					Config.DisplayedEvents.Remove( item.EventType );
				else
					Config.DisplayedEvents.Add( item.EventType );
			};

			Items.Add( item );
		}
	}
}

public class EventTypeIncludedItem : INotifyPropertyChanged
{
	#region Public properties

	public string Label { get; init; } = string.Empty;

	public EventType EventType { get; set; }

	public bool IsIncluded
	{
		get => _isIncluded;
		set => SetField( ref _isIncluded, value );
	}
	
	#endregion 
	
	#region Private fields

	private bool _isIncluded;
	
	#endregion 
	
	#region INotifyPropertyChanged interface implementation 
	
	public event PropertyChangedEventHandler? PropertyChanged;
	
	protected virtual void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
	{
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
	}
	
	protected bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
	{
		if( EqualityComparer<T>.Default.Equals( field, value ) )
		{
			return false;
		}

		field = value;
		
		OnPropertyChanged( propertyName );
		
		return true;
	}
	
	#endregion 
}
