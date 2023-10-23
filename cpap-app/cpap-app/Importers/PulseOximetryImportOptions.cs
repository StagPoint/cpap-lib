namespace cpap_app.Importers;

public class PulseOximetryDevice
{
	public int ID { get; set; }
	
	public string Name { get; set; }
}

public class PulseOximetryImportOptions
{
	public int DeviceID { get; set; }

	public double CalibrationAdjust { get; set; } = 0;
	public double TimeAdjust        { get; set; } = 0;
	public bool   GenerateEvents { get; set; } = true;
}
