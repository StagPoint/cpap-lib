using cpap_app.Importers;

using cpaplib;

namespace cpap_app.ViewModels;

public class ImportOptionsViewModel
{
	public int                        UserProfileID         { get; set; }
	public CpapImportSettings         CpapSettings          { get; set; } = new();
	public PulseOximetryImportOptions PulseOximetrySettings { get; set; } = new();

	public ImportOptionsViewModel() : this( UserProfileStore.GetActiveUserProfile().UserProfileID )
	{
		
	}
	
	public ImportOptionsViewModel( int userProfileID )
	{
		UserProfileID         = userProfileID;
		CpapSettings          = ImportOptionsStore.GetCpapImportSettings( userProfileID );
		PulseOximetrySettings = ImportOptionsStore.GetPulseOximetryImportOptions( userProfileID );
	}

	public void SaveChanges()
	{
		ImportOptionsStore.UpdateCpapImportSettings( UserProfileID, CpapSettings );
		ImportOptionsStore.UpdatePulseOximetryImportOptions( UserProfileID, PulseOximetrySettings );
	}
}
