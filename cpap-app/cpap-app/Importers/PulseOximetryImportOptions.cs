namespace cpap_app.Importers;

public class PulseOximetryImportOptions
{
	public int ID { get; set; }
	
	public double TimeAdjust        { get; set; } = 0;
	public double CalibrationAdjust { get; set; } = 0;
	public bool   GenerateEvents    { get; set; } = true;

	public double EventScanDelay { get; set; } = 300;

	public double HypoxemiaThreshold       { get; set; } = 88;
	public double HypoxemiaMinimumDuration { get; set; } = 8;

	public double DesaturationThreshold       { get; set; } = 3;
	public double DesaturationWindowLength    { get; set; } = 600;
	public double DesaturationMinimumDuration { get; set; } = 1;
	public double DesaturationMaximumDuration { get; set; } = 120;

	public double TachycardiaThreshold     { get; set; } = 100;
	public double BradycardiaThreshold     { get; set; } = 50;
	public double PulseRateMinimumDuration { get; set; } = 10;

	public double PulseChangeThreshold    { get; set; } = 10;
	public double PulseChangeWindowLength { get; set; } = 120;
}
