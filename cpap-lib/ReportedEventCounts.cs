using System;
using System.Collections.Generic;

namespace cpaplib
{
	public class ReportedEventCounts
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
		public double CSR { get; internal set; }

		/// <summary>
		/// The total number of Obstructive Apnea events that occurred
		/// </summary>
		public int ObstructiveApneaCount { get; internal set; }

		/// <summary>
		/// The total number of Hypopnea events that occurred
		/// </summary>
		public int HypopneaCount { get; internal set; }

		/// <summary>
		/// The total number of Open Airway events that occurred
		/// </summary>
		public int ClearAirwayCount { get; internal set; }

		/// <summary>
		/// The total number of Unclassified Apnea events that occurred
		/// </summary>
		public int UnclassifiedApneaCount { get; internal set; }

		/// <summary>
		/// The total number of RERA events that occurred
		/// </summary>
		public int RespiratoryEffortCount { get; internal set; }
		
		/// <summary>
		/// The number of Flow Limit events that occur per hour
		/// </summary>
		public double FlowLimitIndex { get; internal set; }
		
		/// <summary>
		/// The total number of Flow Limit events that occurred
		/// </summary>
		public int FlowLimitCount { get; internal set; }
		
		/// <summary>
		/// The total time spent that day in apnea events 
		/// </summary>
		public TimeSpan TotalTimeInApnea { get; internal set; }
		
		/// <summary>
		/// The total time when large leaks (above the specified limit) occured
		/// </summary>
		public TimeSpan TotalTimeOfLargeLeaks { get; internal set; }

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
