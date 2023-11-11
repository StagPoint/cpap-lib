using System;

using cpaplib;

namespace cpap_app.Events;

public enum AnnotationListEventType
{
	Added,
	Removed,
	Changed,
}

public class AnnotationListEventArgs : EventArgs
{
	public          AnnotationListEventType Change     { get; set; }
	public required Annotation              Annotation { get; set; }
}
