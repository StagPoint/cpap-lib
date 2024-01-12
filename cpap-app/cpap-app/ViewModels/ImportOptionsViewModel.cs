using cpap_app.Importers;

using cpaplib;

namespace cpap_app.ViewModels;

public class ImportOptionsViewModel
{
	public CpapImportSettings           CpapSettings          { get; set; } = new();
	public PulseOximetryImportOptions   PulseOximetrySettings { get; set; } = new();
	public OximetryEventGeneratorConfig OximetryEventSettings { get; set; } = new();
}
