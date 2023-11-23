namespace cpap_app.Importers;

public class PulseOximetryImportOptions
{
	public int DeviceID { get; set; }

	public double CalibrationAdjust { get; set; } = 0;
	public double TimeAdjust        { get; set; } = 0;
	public bool   GenerateEvents { get; set; } = true;
}
