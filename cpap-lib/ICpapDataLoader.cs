using System;
using System.Collections.Generic;

namespace cpaplib
{
	public interface ICpapDataLoader
	{
		bool HasCorrectFolderStructure( string rootFolder );
		
		MachineIdentification LoadMachineIdentificationInfo( string rootFolder );

		List<DailyReport> LoadFromFolder(
			string    rootFolder,
			DateTime? minDate,
			DateTime? maxDate,
			CpapImportSettings importSettings 
			);
	}
}
