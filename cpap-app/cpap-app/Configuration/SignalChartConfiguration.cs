using System;
using System.Collections.Generic;

using cpaplib;

namespace cpap_app.Configuration;

using Color = System.Drawing.Color;

public class SignalChartConfiguration : IComparable<SignalChartConfiguration>
{
	#region Public properties

	public int ID { get; set; }

	/// <summary>
	/// The name that will be displayed for this Signal
	/// </summary>
	public string Title { get; set; } = "";

	/// <summary>
	/// The name of the Signal
	/// </summary>
	public string SignalName { get; set; } = "";

	/// <summary>
	/// Contains the name of a second signal that should also be displayed in the same chart, if any
	/// </summary>
	public string SecondarySignalName { get; set; } = "";

	/// <summary>
	/// The order in which the signal will be displayed (with higher values appearing later)
	/// </summary>
	public int DisplayOrder { get; set; }

	/// <summary>
	/// Whether the Signal's chart is pinned (always at the top and doesn't scroll)
	/// </summary>
	public bool IsPinned { get; set; }

	/// <summary>
	/// Whether the Signal's chart is displayed 
	/// </summary>
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// If set, indicates the position on the Y axis to show a dotted red line indicating the top of a normal range 
	/// </summary>
	public double? BaselineHigh { get; set; }

	/// <summary>
	/// If set, indicates the position on the Y axis to show a dotted red line indicating the bottom of a normal range 
	/// </summary>
	public double? BaselineLow { get; set; }

	/// <summary>
	/// If set to TRUE, the chart's Y axis will be automatically scaled to fit the data, rather than
	/// having a pre-set scale.
	/// </summary>
	public bool AutoScaleY { get; set; } = false;

	/// <summary>
	/// If set, controls the minimum value that will be displayed on the Y axis in a Signal's chart
	/// </summary>
	public double? AxisMinValue { get; set; }

	/// <summary>
	/// If set, controls the maximum value that will be displayed on the Y axis in a Signal's chart
	/// </summary>
	public double? AxisMaxValue { get; set; }

	/// <summary>
	/// If set to TRUE, the chart will paint the area under the Signal's waveform with a gradient 
	/// </summary>
	public bool? FillBelow { get; set; }

	/// <summary>
	/// The color to use when drawing the Signal's waveform 
	/// </summary>
	public Color PlotColor { get; set; } = System.Drawing.Color.DodgerBlue;

	/// <summary>
	/// If set to TRUE, the Signal will be displayed as a square waveform rather than a typical line graph. 
	/// </summary>
	public bool ShowStepped { get; set; }

	/// <summary>
	/// Contains the list of types of event markers that will be overlaid on this Signal's chart
	/// </summary>
	public List<EventType> DisplayedEvents { get; set; } = new();

	#endregion

	#region IComparable<SignalChartConfiguration> interface implementation

	public int CompareTo( SignalChartConfiguration? other )
	{
		if( other == null )
		{
			return 0;
		}

		if( IsVisible != other.IsVisible )
		{
			return IsVisible ? -1 : 1;
		}

		return IsPinned == other.IsPinned ? DisplayOrder.CompareTo( other.DisplayOrder ) : (IsPinned ? -1 : 1);
	}

	#endregion

	#region Base class overrides

	public override string ToString()
	{
		return SignalName;
	}

	#endregion
}
