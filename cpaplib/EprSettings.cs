﻿namespace cpaplib
{
	public class EprSettings
	{
		public EprType Mode             { get; set; }
		public bool    ClinicianEnabled { get; set; }
		public bool    EprEnabled       { get; set; }
		public int     Level            { get; set; }

	#region Base class overrides

		public override string ToString()
		{
			if( EprEnabled )
				return $"Enabled: {Mode} (Level {Level})";

			return $"{Mode}";
		}

	#endregion
	}
}
