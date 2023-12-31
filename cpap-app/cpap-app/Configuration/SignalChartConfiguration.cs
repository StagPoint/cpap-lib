﻿using System;
using System.Collections.Generic;

using cpaplib;

namespace cpap_app.Configuration;

using Color = System.Drawing.Color;

public enum AxisScalingMode
{
	/// <summary>
	/// The scaling mode is controlled by the Signal's defined <see cref="Signal.MinValue"/> and <see cref="Signal.MaxValue"/> values 
	/// </summary>
	Defaults,
	/// <summary>
	/// The Y axis will be scaled to show only the range of the available data
	/// </summary>
	AutoFit,
	/// <summary>
	/// The Y axis will be scaled according to user-defined values <see cref="SignalChartConfiguration.AxisMinValue"/> and <see cref="SignalChartConfiguration.AxisMaxValue"/> 
	/// </summary>
	Override
}

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
	/// Whether the Signal's chart is displayed in the History/Trends view
	/// </summary>
	public bool ShowInTrends { get; set; } = true;

	/// <summary>
	/// If set, indicates the position on the Y axis to show a dotted red line indicating the top of a normal range 
	/// </summary>
	public double? BaselineHigh { get; set; }

	/// <summary>
	/// If set, indicates the position on the Y axis to show a dotted red line indicating the bottom of a normal range 
	/// </summary>
	public double? BaselineLow { get; set; }

	/// <summary>
	/// Controls how the Signal's Y axis will be scaled
	/// </summary>
	public AxisScalingMode ScalingMode { get; set; } = AxisScalingMode.Defaults;

	/// <summary>
	/// If set to TRUE, the Y axis will be inverted
	/// </summary>
	public bool InvertAxisY { get; set; } = false;

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
	public bool? FillBelow { get; set; } = false;

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
		return other == null ? 0 : DisplayOrder.CompareTo( other.DisplayOrder );
	}

	#endregion

	#region Base class overrides

	public override string ToString()
	{
		return SignalName;
	}

	#endregion
}
