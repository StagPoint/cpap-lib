﻿using System.Collections.Generic;

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

		internal void ReadFrom( Dictionary<string, double> data )
		{
			AHI = data[ "AHI" ];
			HI  = data[ "HI" ];
			AI  = data[ "AI" ];
			OAI = data[ "OAI" ];
			CAI = data[ "CAI" ];
			UAI = data[ "UAI" ];
			RIN = data[ "RIN" ];
			CSR = data[ "CSR" ];
		}
	}
}