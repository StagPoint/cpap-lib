using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

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
	
	#region Private fields 
	
	private static List<string> _signalNames = typeof( SignalNames ).GetAllPublicConstantValues<string>();

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
	
	public void ResetToDefaults()
	{
		var colorIndex = _signalNames.IndexOf( SignalName );
		if( colorIndex < 0 )
		{
			colorIndex = DisplayOrder;
		}
		
		var plotColor  = DataColors.GetDataColor( colorIndex );

		Title     = SignalName;
		IsPinned  = false;
		IsVisible = (SignalName != SignalNames.EPAP && SignalName != SignalNames.MaskPressureLow);
		FillBelow = true;
		PlotColor = plotColor.ToDrawingColor();

		switch( SignalName )
		{
			case SignalNames.MaskPressureLow:
				// Do not create a configuration for this Signal
				break;
			
			case SignalNames.FlowRate:
				BaselineHigh = 0;
				ShowInTrends = false;
				
				DisplayedEvents = new List<EventType>( EventTypes.Apneas );
				DisplayedEvents.Add( EventType.RERA );
				DisplayedEvents.Add( EventType.FlowReduction );
				break;
			
			case SignalNames.Pressure:
				SecondarySignalName = SignalNames.EPAP;
				AxisMinValue        = 5;
				AxisMaxValue        = 25;
				ScalingMode         = AxisScalingMode.Override;
				break;
			
			case SignalNames.MaskPressure:
				AxisMinValue = 0;
				AxisMaxValue = 20;
				ScalingMode  = AxisScalingMode.Override;
				ShowInTrends = false;
				break;
			
			case SignalNames.LeakRate:
				SecondarySignalName = SignalNames.TotalLeak;
				ShowStepped         = true;
				BaselineHigh        = 24;
				BaselineLow         = 8;
				AxisMinValue        = 0;
				AxisMaxValue        = 40;
				ScalingMode         = AxisScalingMode.Override;
				DisplayedEvents = new List<EventType>()
				{
					EventType.LargeLeak
				};
				break;
			
			case SignalNames.TotalLeak:
				ScalingMode  = AxisScalingMode.Override;
				BaselineHigh = 24;
				BaselineLow  = 8;
				AxisMinValue = 0;
				AxisMaxValue = 40;
				IsVisible    = false;
				ShowStepped  = true;
				break;
			
			case SignalNames.FlowLimit:
				ShowStepped  = true;
				BaselineLow  = 0.25;
				BaselineHigh = 0.5;
				DisplayedEvents = new List<EventType>()
				{
					EventType.FlowLimitation
				};
				break;
			
			case SignalNames.TidalVolume:
				ScalingMode  = AxisScalingMode.Override;
				BaselineHigh = 500;
				AxisMinValue = 0;
				AxisMaxValue = 2000;
				break;
			
			case SignalNames.MinuteVent:
				SecondarySignalName = SignalNames.TargetVent;
				BaselineHigh        = 12;
				BaselineLow         = 4;
				break;
			
			case SignalNames.RespirationRate:
				ScalingMode  = AxisScalingMode.Override;
				Title        = "Resp. Rate";
				BaselineHigh = 24;
				BaselineLow  = 10;
				AxisMinValue = 0;
				AxisMaxValue = 40;
				break;
			
			case SignalNames.SpO2:
				ScalingMode     = AxisScalingMode.Override;
				DisplayedEvents = EventTypes.OxygenSaturation.ToList();
				BaselineLow     = 88;
				AxisMinValue    = 80;
				AxisMaxValue    = 100;
				break;
			
			case SignalNames.Pulse:
				ScalingMode     = AxisScalingMode.Override;
				DisplayedEvents = EventTypes.Pulse.ToList();
				BaselineHigh    = 100;
				BaselineLow     = 50;
				AxisMinValue    = 40;
				AxisMaxValue    = 120;
				break;
			
			case SignalNames.Snore:
			case SignalNames.Movement:
				ScalingMode  = AxisScalingMode.AutoFit;
				IsVisible    = false;
				ShowStepped  = true;
				ShowInTrends = false;
				break;
			
			case SignalNames.AHI:
				Title           = "AHI";
				ShowStepped     = true;
				ScalingMode     = AxisScalingMode.AutoFit;
				ShowInTrends    = false;
				IsVisible       = false;
				DisplayedEvents = new List<EventType>( EventTypes.Apneas );
				break;
			
			case SignalNames.InspirationTime:
				Title        = "Insp. Time";
				ScalingMode  = AxisScalingMode.Override;
				AxisMinValue = 0;
				AxisMaxValue = 12;
				ShowInTrends = false;
				IsVisible    = false;
				break;
			
			case SignalNames.ExpirationTime:
				Title        = "Exp. Time";
				ScalingMode  = AxisScalingMode.Override;
				AxisMinValue = 0;
				AxisMaxValue = 10;
				ShowInTrends = false;
				IsVisible    = false;
				break;
			
			case SignalNames.InspToExpRatio:
				Title        = "I:E Ratio";
				AxisMinValue = 0;
				AxisMaxValue = 4;
				ScalingMode  = AxisScalingMode.Override;
				ShowInTrends = false;
				IsVisible    = false;
				break;
			
			case SignalNames.SleepStages:
				Title        = "Sleep Stage";
				AxisMaxValue = 0;
				AxisMaxValue = 5;
				InvertAxisY  = true;
				IsVisible    = false;
				ShowStepped  = true;
				ShowInTrends = false;
				break;
			
			case SignalNames.TargetVent:
				ShowInTrends = false;
				IsVisible    = false;
				break;
		}
	}
}
