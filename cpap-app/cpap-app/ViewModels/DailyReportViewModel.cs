using System;
using System.Reflection;

using cpap_app.Events;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyReportViewModel : DailyReport
{
	#region Events

	public event EventHandler<AnnotationListEventArgs>? AnnotationsChanged;
	
	#endregion 
	
	#region Constructor 
	
	public DailyReportViewModel( DailyReport source )
	{
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
	
	#region Public functions 
	
	public void AddAnnotation( Annotation annotation )
	{
		using var db = StorageService.Connect();
		db.Insert( annotation, foreignKeyValue: ID );
		
		Annotations.Add( annotation );
		
		AnnotationsChanged?.Invoke( this, new AnnotationListEventArgs()
		{
			Change     = AnnotationListEventType.Added,
			Annotation = annotation,
		} );
	}

	public void DeleteAnnotation( Annotation annotation )
	{
		using var db = StorageService.Connect();
		if( !db.Delete( annotation, ID ) )
		{
			throw new InvalidOperationException( $"Failed to delete {nameof( Annotation )}: {annotation}" );
		}
		
		Annotations.Remove( annotation );
		
		AnnotationsChanged?.Invoke( this, new AnnotationListEventArgs()
		{
			Change     = AnnotationListEventType.Removed,
			Annotation = annotation,
		} );
	}
	
	#endregion 
}
