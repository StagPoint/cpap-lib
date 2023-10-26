using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace cpap_app.ViewModels;

public class CheckmarkMenuItemViewModel : INotifyPropertyChanged
{
	#region Public properties

	public string Label
	{
		get => _label;
		set => SetField( ref _label, value );
	}

	public object? Tag 
	{
		get => _tag;
		set => SetField( ref _tag, value );
	}

	public bool IsChecked
	{
		get => _isChecked;
		set => SetField( ref _isChecked, value );
	}
	
	#endregion 
	
	#region Private fields

	private bool    _isChecked;
	private object? _tag;
	private string  _label = string.Empty;
	
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
