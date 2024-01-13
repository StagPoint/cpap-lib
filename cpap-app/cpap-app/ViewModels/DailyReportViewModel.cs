using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using cpap_app.Events;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyReportViewModel : DailyReport, INotifyPropertyChanged
{
	#region Events

	public event PropertyChangedEventHandler? PropertyChanged;

	public event EventHandler<AnnotationListEventArgs>? AnnotationsChanged;

	public event EventHandler? ReloadRequired;
	
	#endregion 
	
	#region Public properties
	
	public UserProfile UserProfile { get; set; }

	public Action<string, DateTime, DateTime> CreateNewAnnotation { get; set; } = CreateNewAnnotation_DEFAULT;

	public Action<Annotation> EditAnnotation { get; set; } = EditAnnotation_DEFAULT;
	
	#endregion 
	
	#region Constructor 
	
	public DailyReportViewModel( UserProfile user, DailyReport source )
	{
		UserProfile = user ?? throw new ArgumentNullException( nameof( user ) );
		Copy( source ?? throw new ArgumentNullException( nameof( source ) ) );
	}

	#endregion
	
	#region Public functions 
	
	public void AddAnnotation( Annotation annotation )
	{
		using var db = StorageService.Connect();
		db.Insert( annotation, foreignKeyValue: ID );
		
		Annotations.Add( annotation );
		Annotations.Sort();

		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Annotations ) ) );
		
		AnnotationsChanged?.Invoke( this, new AnnotationListEventArgs()
		{
			Change     = AnnotationListEventType.Added,
			Annotation = annotation,
		} );
	}

	public void UpdateAnnotation( Annotation annotation )
	{
		var replaced = Annotations.FirstOrDefault( x => x.AnnotationID == annotation.AnnotationID );
		if( replaced == null )
		{
			throw new InvalidOperationException( $"Could not find and existing {nameof( Annotation )} matching the argument" );
		}

		using var db = StorageService.Connect();

		if( !db.Update( annotation ) )
		{
			throw new Exception( $"Failed to update {annotation}" );
		}
		
		// Replace the annotation with the edited version 
		if( !ReferenceEquals( replaced, annotation ) )
		{
			Annotations.Remove( replaced );
			Annotations.Add( annotation );
			Annotations.Sort();
		}

		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Annotations ) ) );
		
		AnnotationsChanged?.Invoke( this, new AnnotationListEventArgs()
		{
			Change     = AnnotationListEventType.Changed,
			Annotation = annotation,
		} );
	}

	public void DeleteAnnotation( Annotation annotation )
	{
		using var db = StorageService.Connect();
		if( !db.Delete( annotation ) )
		{
			throw new InvalidOperationException( $"Failed to delete {nameof( Annotation )}: {annotation}" );
		}
		
		Annotations.Remove( annotation );
		Annotations.Sort();
		
		AnnotationsChanged?.Invoke( this, new AnnotationListEventArgs()
		{
			Change     = AnnotationListEventType.Removed,
			Annotation = annotation,
		} );
	}
	
	public void DeleteSession( Session session )
	{
		if( !RemoveSession( session ) )
		{
			return;
		}

		// Ensure that a notation is made about this change 
		Notes = Notes.TrimEnd() + $"\nDeleted '{session.Source}' session starting at {session.StartTime:g} and ending at {session.EndTime}\n";
		
		using var db = StorageService.Connect();
		db.SaveDailyReport( UserProfile.UserProfileID, this );

		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Notes ) ) );
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Sessions ) ) );

		Reload();
	}
	
	public void SaveNotes( string notesText )
	{
		this.Notes = notesText;
		
		using var db = StorageService.Connect();
		db.Update( (DailyReport)this );
		
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Notes ) ) );
	}
	
	public void Reload()
	{
		using var db = StorageService.Connect();

		var newDay = db.LoadDailyReport( UserProfile.UserProfileID, ReportDate );
		Copy( newDay );
		
		ReloadRequired?.Invoke( this, EventArgs.Empty );
	}

	#endregion
	
	#region Private functions

	private static void EditAnnotation_DEFAULT( Annotation annotation )
	{
		throw new NullReferenceException( $"Caller attempted to invoke {nameof( EditAnnotation )}, but no delegate has been assigned" );
	}
	
	private static void CreateNewAnnotation_DEFAULT( string arg1, DateTime arg2, DateTime arg3 )
	{
		throw new NullReferenceException( $"Caller attempted to invoke {nameof( CreateNewAnnotation )}, but no delegate has been assigned" );
	}

	private void Copy( DailyReport source )
	{
		if( source == null )
		{
			throw new ArgumentNullException( $"{nameof( source )} cannot be NULL" );
		}
		
		// Copy all of the source's property values to this instance
		var properties = typeof( DailyReport ).GetTypeInfo().GetProperties( BindingFlags.Instance | BindingFlags.Public );
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
