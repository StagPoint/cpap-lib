using System;
using System.Drawing;

using cpap_app.Helpers;

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

	public void ResetToDefaults()
	{
		const float DEFAULT_SPAN_OPACITY = 0.65f;
		
		var eventTypeLabel      = EventType.ToName();
		var eventMarkerType     = EventMarkerType.Flag;
		var eventMarkerPosition = EventMarkerPosition.AtEnd;
		var eventColor          = DataColors.GetMarkerColor( (int)EventType ).ToDrawingColor();

		switch( EventType )
		{
			case EventType.ObstructiveApnea:
			case EventType.Hypopnea:
			case EventType.ClearAirway:
			case EventType.RERA:
			case EventType.UnclassifiedApnea:
			case EventType.FlowReduction:
				eventMarkerType     = EventMarkerType.Flag;
				eventMarkerPosition = EventMarkerPosition.AtEnd;
				break;
			case EventType.Arousal:
				eventMarkerType     = EventMarkerType.TickBottom;
				eventMarkerPosition = EventMarkerPosition.AtEnd;
				break;
			case EventType.CSR:
			case EventType.FlowLimitation:
			case EventType.LargeLeak:
			case EventType.PeriodicBreathing:
			case EventType.VariableBreathing:
			case EventType.BreathingNotDetected:
				eventColor          = eventColor.MultiplyAlpha( DEFAULT_SPAN_OPACITY );
				eventMarkerType     = EventMarkerType.Span;
				eventMarkerPosition = EventMarkerPosition.AtEnd;
				break;
			case EventType.VibratorySnore:
				eventMarkerType     = EventMarkerType.TickTop;
				eventMarkerPosition = EventMarkerPosition.AtBeginning;
				break;
			case EventType.Desaturation:
				eventMarkerType     = EventMarkerType.ArrowBottom;
				eventColor          = Color.OrangeRed;
				eventMarkerPosition = EventMarkerPosition.InCenter;
				break;
			case EventType.PulseRateChange:
				eventMarkerType     = EventMarkerType.TickBottom;
				eventColor          = Color.Red;
				eventMarkerPosition = EventMarkerPosition.AtBeginning;
				break;
			case EventType.Hypoxemia:
			case EventType.Tachycardia:
			case EventType.Bradycardia:
				eventColor          = eventColor.MultiplyAlpha( DEFAULT_SPAN_OPACITY );
				eventMarkerType     = EventMarkerType.Span;
				eventMarkerPosition = EventMarkerPosition.AtBeginning;
				break;
			case EventType.PulseOximetryFault:
				eventColor          = Color.DimGray.MultiplyAlpha( DEFAULT_SPAN_OPACITY );
				eventMarkerType     = EventMarkerType.Span;
				eventMarkerPosition = EventMarkerPosition.AtBeginning;
				break;
			case EventType.RecordingStarts:
			case EventType.RecordingEnds:
				eventMarkerType = EventMarkerType.None;
				break;
			case >= cpaplib.EventType.FalsePositive:
				eventColor = Color.Black;
				break;
			default:
				throw new Exception( $"{nameof(EventType)} value not handled: {EventType}" );
		}

		Label           = eventTypeLabel;
		EventMarkerType = eventMarkerType;
		MarkerPosition  = eventMarkerPosition;
		Initials        = EventType.ToInitials();
		Color           = eventColor;
	}
	
	public override string ToString()
	{
		return $"{EventType} ({Label}) - {Color}";
	}
}
