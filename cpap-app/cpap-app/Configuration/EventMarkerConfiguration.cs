using System.Drawing;

using cpaplib;

namespace cpap_app.Configuration;

public enum EventMarkerType
{
	Flag,
	TickTop,
	TickBottom,
	ArrowTop,
	ArrowBottom,
	Span,
	None,
}

public class EventMarkerConfiguration
{
	public EventType  EventType  { get; set; }
	public EventMarkerType EventMarkerType { get; set; }
	public string     Label      { get; set; } = "";
	public Color      Color      { get; set; }
}
