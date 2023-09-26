using Avalonia.Media;

namespace cpap_app.Configuration;

public class SignalChartConfiguration
{
	public int ID { get; set; }
	
	public string  Title               { get; set; }
	public string  SignalName          { get; set; } = "";
	public string  SecondarySignalName { get; set; } = "";
	public int     DisplayOrder        { get; set; }
	public bool    IsPinned            { get; set; }
	public bool    IsVisible           { get; set; } = true;
	public double? RedlinePosition     { get; set; }
	public double? MinSignalValue      { get; set; }
	public double? MaxSignalValue      { get; set; }
	public bool?   FillBelow           { get; set; }
	public Color   PlotColor           { get; set; } = Colors.DodgerBlue;
	
	public override string ToString()
	{
		return SignalName;
	}
}
