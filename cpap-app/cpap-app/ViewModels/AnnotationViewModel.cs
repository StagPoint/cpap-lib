using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using cpaplib;

using Color = System.Drawing.Color;

namespace cpap_app.ViewModels;

public class AnnotationViewModel : INotifyPropertyChanged
{
	#region Public properties

	public int AnnotationID
	{
		get => _annotationID;
		set => SetField( ref _annotationID, value );
	}

	public string Signal
	{
		get => _signal;
		set => SetField( ref _signal, value );
	}

	public DateTime StartTime
	{
		get => _startTime;
		set => SetField( ref _startTime, value );
	}

	public DateTime EndTime
	{
		get => _endTime;
		set => SetField( ref _endTime, value );
	}

	public bool ShowMarker
	{
		get => _showMarker;
		set => SetField( ref _showMarker, value );
	}

	public string Notes
	{
		get => _notes;
		set => SetField( ref _notes, value );
	}

	public Color Color
	{
		get => _color;
		set => SetField( ref _color, value );
	}

	#endregion

	#region Private fields

	private string   _notes;
	private int      _annotationID;
	private string   _signal;
	private DateTime _startTime;
	private DateTime _endTime;
	private bool     _showMarker;
	private Color    _color;

	#endregion

	#region Constructor

	public AnnotationViewModel()
	{
		_signal = string.Empty;
		_notes  = string.Empty;
		_color  = Color.Yellow;
	}

	public AnnotationViewModel( Annotation value )
	{
		_annotationID = value.AnnotationID;
		_signal       = value.Signal;
		_startTime    = value.StartTime;
		_endTime      = value.EndTime;
		_showMarker   = value.ShowMarker;
		_notes        = value.Notes;
		_color        = value.Color ?? Color.Yellow;
	}

	#endregion

	#region Implicit type conversion

	public static implicit operator AnnotationViewModel( Annotation value )
	{
		return new AnnotationViewModel( value );
	}

	public static implicit operator Annotation( AnnotationViewModel viewModel )
	{
		return new Annotation
		{
			AnnotationID = viewModel._annotationID,
			Signal       = viewModel._signal,
			StartTime    = viewModel._startTime,
			EndTime      = viewModel._endTime,
			ShowMarker   = viewModel._showMarker,
			Notes        = viewModel._notes,
			Color        = viewModel._color,
		};
	}

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
