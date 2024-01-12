namespace cpap_app.Importers;

public class PulseOximetryImportOptions
{
	public int ID { get; set; }

	public string DeviceType { get; set; } = "UNKNOWN";

	public double TimeAdjust { get; set; } = 0;
	public double CalibrationAdjust { get; set; } = 0;
	public bool   GenerateEvents    { get; set; } = true;
}
