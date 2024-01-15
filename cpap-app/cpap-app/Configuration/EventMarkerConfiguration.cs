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

public enum EventMarkerPosition
{
	AtEnd,
	AtBeginning,
	InCenter,
}

public class EventMarkerConfiguration
{
	public EventType           EventType       { get; set; }
	public EventMarkerType     EventMarkerType { get; set; } = EventMarkerType.Flag;
	public EventMarkerPosition MarkerPosition  { get; set; } = EventMarkerPosition.AtEnd;
	public string              Label           { get; set; } = string.Empty;
	public string              Initials        { get; set; } = string.Empty;
	public Color               Color           { get; set; }

	public override string ToString()
	{
		return $"{EventType} ({Label}) - {Color}";
	}
}
