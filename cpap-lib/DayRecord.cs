using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class DayRecord
	{
		/// <summary>
		/// The date on which this report was generated.
		/// </summary>
		public DateTime ReportDate { get; private set; }
		
		/// <summary>
		/// The specific time at which recording began
		/// </summary>
		public DateTime RecordingStartTime { get; internal set; }
		
		/// <summary>
		/// The specific time at which recording ended
		/// </summary>
		public DateTime RecordingEndTime { get => RecordingStartTime + Duration; }

		/// <summary>
		/// The list of sessions  for this day
		/// </summary>
		public List<Session> Sessions { get; } = new List<Session>();

		public List<ReportedEvent> Events { get; } = new List<ReportedEvent>();

		/// <summary>
		/// Returns the number of "Mask Times" for the day
		/// </summary>
		public int MaskEvents { get; private set; }

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
		public List<SignalStatistics> Statistics { get; private set; } = new List<SignalStatistics>();

		/// <summary>
		/// The number of events of each type (Obstructive Apnea, Clear Airway, RERA, etc.) that occurred on this day.
		/// </summary>
		public ReportedEventCounts EventSummary { get; private set; } = new ReportedEventCounts();

		/// <summary>
		/// The total amount of time the CPAP was used on the reported day (calculated)
		/// </summary>
		public TimeSpan Duration { get; internal set; }

		/// <summary>
		/// The total amount of time the CPAP was used on the recorded day, as reported by the CPAP machine 
		/// </summary>
		public TimeSpan OnDuration { get; private set; }

		public double PatientHours { get; private set; }

		/// <summary>
		/// Contains all of the raw data stored for each Day
		/// </summary>
		public Dictionary<string, double> RawData = new Dictionary<string, double>();

		#region Public functions

		/// <summary>
		/// Reads the statistics, settings, and other information from the stored data
		/// </summary>
		public static DayRecord Read( Dictionary<string, double> data )
		{
			var dialy = new DayRecord();
			dialy.ReadFrom( data );

			return dialy;
		}

		/// <summary>
		/// Reads the statistics, settings, and other information from the stored data
		/// </summary>
		public void ReadFrom( Dictionary<string, double> data )
		{
			// I've tried my best to decode what all of the data means, and convert it to meaningful typed
			// values exposed in a reasonable manner, but it's highly likely that there's something I didn't
			// understand correctly, not to mention fields that are different for different models, so the
			// raw data will be kept available for the consumer of this library to make use of if needs be.
			RawData = data;

			ReportDate = new DateTime( 1970, 1, 1 ).AddDays( data[ "Date" ] ).AddHours( 12 );

			Settings.ReadFrom( data );
			EventSummary.ReadFrom( data );

			MaskEvents = (int)(data[ "MaskEvents" ] / 2);
			Duration   = TimeSpan.FromMinutes( data[ "Duration" ] );
			OnDuration = TimeSpan.FromMinutes( data[ "OnDuration" ] );

			PatientHours = getValue( "PatientHours" );

			Fault.Device     = getValue( "Fault.Device" );
			Fault.Alarm      = getValue( "Fault.Alarm" );
			Fault.Humidifier = getValue( "Fault.Humidifier" );
			Fault.HeatedTube = getValue( "Fault.HeatedTube" );

			double getValue( params string[] keys )
			{
				foreach( var key in keys )
				{
					if( data.TryGetValue( key, out double value ) )
					{
						return value;
					}
				}

				return 0;
			}
		}

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
			Duration           = DateUtil.Max( RecordingEndTime, session.EndTime ) - RecordingStartTime;

			Sessions.Sort( ( lhs, rhs ) => lhs.StartTime.CompareTo( rhs.StartTime ) );
		}
		
		/// <summary>
		/// Merges Session data with existing Sessions when possible, or adds it if there are no coincident Sessions
		/// to merge with. Note that the Session being passed must still overlap the time period of this DayRecord,
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
			var endTime   = DateUtil.Max( RecordingEndTime, session.EndTime );

			Duration = endTime - RecordingStartTime;
		}

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			if( Sessions.Count > 0 )
			{
				return $"{ReportDate.ToLongDateString()}   {Sessions.First().StartTime.ToShortTimeString()} - {Sessions.Last().EndTime.ToShortTimeString()}    ({Duration})";
			}

			return $"{ReportDate.ToLongDateString()}   ({Duration})";
		}

		#endregion
	}
}
