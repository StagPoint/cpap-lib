using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class DailyReport
	{
		#region Public properties 
		
		/// <summary>
		/// Identifies the machine that was used to record this report 
		/// </summary>
		public MachineIdentification MachineInfo { get; set; }
		
		/// <summary>
		/// The date on which this report was generated.
		/// </summary>
		public DateTime ReportDate { get; set; }
		
		/// <summary>
		/// The specific time at which recording began
		/// </summary>
		public DateTime RecordingStartTime { get; set; }
		
		/// <summary>
		/// The specific time at which recording ended
		/// </summary>
		public DateTime RecordingEndTime { get; set; }
		
		/// <summary>
		/// Returns the total time between the start of the first session and the end of the last session
		/// </summary>
		public TimeSpan TotalTimeSpan { get => RecordingEndTime - RecordingStartTime; }

		/// <summary>
		/// The list of sessions  for this day
		/// </summary>
		public List<Session> Sessions { get; } = new List<Session>();

		public List<ReportedEvent> Events { get; } = new List<ReportedEvent>();

		/// <summary>
		/// Returns the number of "Mask Times" for the day
		/// </summary>
		public int MaskEvents { get; set; }

		/// <summary>
		/// Fault information reported by the CPAP machine
		/// </summary>
		public FaultInfo Fault { get; set; } = new FaultInfo();

		/// <summary>
		/// The settings (pressure, EPR, response type, etc.) used on this day
		/// </summary>
		public MachineSettings Settings { get; set; } = new MachineSettings();

		/// <summary>
		/// Usage and performance statistics for this day (average pressure, leak rate, etc.)
		/// </summary>
		public List<SignalStatistics> Statistics { get; set; } = new List<SignalStatistics>();

		/// <summary>
		/// The number of events of each type (Obstructive Apnea, Clear Airway, RERA, etc.) that occurred on this day.
		/// </summary>
		public ReportedEventCounts EventSummary { get; set; } = new ReportedEventCounts();

		/// <summary>
		/// The total amount of time the CPAP was used on the reported day (calculated)
		/// </summary>
		public TimeSpan UsageTime { get; set; }

		/// <summary>
		/// Returns the total number of hours the patient has used the CPAP machine since the last factory reset.
		/// Supported on ResMed AirSense machines, not sure about others. 
		/// </summary>
		public double PatientHours { get; set; }

		#endregion 

		#region Public functions

		/// <summary>
		/// Recalculates the statistics for the named Signal. Designed to be called after a data import to
		/// update the statistics to account for the newly imported data. 
		/// </summary>
		public void UpdateSignalStatistics( string signalName )
		{
			var calculator = new StatCalculator( short.MaxValue );
			var stats      = calculator.CalculateStats( signalName, Sessions );

			Statistics.RemoveAll( x => x.SignalName.Equals( signalName ) );
			Statistics.Add( stats );
		}

		/// <summary>
		/// Adds a new Session to the Sessions list and updates the RecordingStartTime and RecordingEndTime
		/// properties if necessary.
		/// </summary>
		/// <param name="session"></param>
		public void AddSession( Session session )
		{
			Sessions.Add( session );
			
			RecordingStartTime = DateUtil.Min( RecordingStartTime, session.StartTime );
			RecordingEndTime   = DateUtil.Max( RecordingEndTime, session.EndTime );
			UsageTime           = DateUtil.Max( RecordingEndTime, session.EndTime ) - RecordingStartTime;

			Sessions.Sort( ( lhs, rhs ) => lhs.StartTime.CompareTo( rhs.StartTime ) );
		}
		
		/// <summary>
		/// Merges Session data with existing Sessions when possible, or adds it if there are no coincident Sessions
		/// to merge with. Note that the Session being passed must still overlap the time period of this DailyReport,
		/// and an exception will be thrown if that is not the case.  
		/// </summary>
		public void MergeSession( Session session )
		{
			if( RecordingStartTime > session.EndTime || RecordingEndTime < session.StartTime )
			{
				throw new Exception( $"Session from {session.StartTime} to {session.EndTime} does not overlap reporting period for {ReportDate.Date} and cannot be merged." );
			}
			
			// There are two obvious options here: Merge the new session's signals into an existing session, or 
			// simply add the new session to the list of the day's sessions. 
			//
			// The first option will potentially involve fixing up start and end times, and might have more 
			// non-obvious edge cases to worry about, but the second option creates a situation where all 
			// sessions only contain a subset of the available data for a given period of time, which sort of
			// breaks the original design of what a session entails.
			//
			// When merging into an existing session, there is the question of whether to extend the session
			// start and end times if the new data exceeds them, or whether to trim the new data to match 
			// those times if needed. Consider adding pulse oximetry data to a session containing CPAP flow
			// pressure data (among others) when the pulse oximetry data starts a few seconds before the CPAP
			// data starts and ends a minute after the CPAP session ends; The user probably cares more about
			// the CPAP data and wants to see what their blood oxygen levels were during the CPAP therapy,
			// and may not necessarily care about values that lie outside of the "mask on" period. 
			//

			foreach( var existingSession in Sessions )
			{
				bool disjoint = (existingSession.StartTime > session.EndTime || existingSession.EndTime < session.StartTime);
				if( !disjoint )
				{
					// When merging with an existing Session, all of the merged Signals will be trimmed to fit the 
					// destination Session's time period. 
					existingSession.Merge( session );
					
					return;
				}
			}

			Sessions.Add( session );
			Sessions.Sort( ( lhs, rhs ) => lhs.StartTime.CompareTo( rhs.StartTime ) );
			
			RecordingStartTime = DateUtil.Min( RecordingStartTime, session.StartTime );
			RecordingEndTime   = DateUtil.Max( RecordingEndTime, session.EndTime );

			UsageTime = RecordingEndTime - RecordingStartTime;
		}

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			if( Sessions.Count > 0 )
			{
				return $"{ReportDate.ToLongDateString()}   {Sessions.First().StartTime.ToShortTimeString()} - {Sessions.Last().EndTime.ToShortTimeString()}    ({UsageTime})";
			}

			return $"{ReportDate.ToLongDateString()}   ({UsageTime})";
		}

		#endregion
	}
}
