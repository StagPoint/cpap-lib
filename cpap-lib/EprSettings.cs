namespace cpaplib
{
	public class EprSettings
	{
		#region Public properties 
		
		public EprType Mode             { get; set; }
		public bool    EprEnabled       { get; set; }
		public int     Level            { get; set; }
		
		#endregion 

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
