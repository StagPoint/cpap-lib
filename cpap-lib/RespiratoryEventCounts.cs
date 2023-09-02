using System.Collections.Generic;

namespace cpaplib
{
	public class RespiratoryEventCounts
	{
		/// <summary>
		/// Combined Apnea/Hypopnea Index
		/// </summary>
		public double AHI { get; private set; }

		/// <summary>
		/// Hypopnea Index
		/// </summary>
		public double HI { get; private set; }

		/// <summary>
		/// Apnea Index
		/// </summary>
		public double AI { get; private set; }

		/// <summary>
		/// Obstructive Apnea Index
		/// </summary>
		public double OAI { get; private set; }

		/// <summary>
		/// Clear Airway Index
		/// </summary>
		public double CAI { get; private set; }

		/// <summary>
		/// Uncategorized Apnea Index 
		/// </summary>
		public double UAI { get; private set; }

		/// <summary>
		/// RERA?
		/// </summary>
		public double RIN { get; private set; }

		/// <summary>
		/// Cheyne-Stokes Respiration
		/// </summary>
		public double CSR { get; private set; }

		internal void ReadFrom( Dictionary<string, double> map )
		{
			AHI = map[ "AHI" ];
			HI  = map[ "HI" ];
			AI  = map[ "AI" ];
			OAI = map[ "OAI" ];
			CAI = map[ "CAI" ];
			UAI = map[ "UAI" ];
			RIN = map[ "RIN" ];
			CSR = map[ "CSR" ];
		}
	}
}
