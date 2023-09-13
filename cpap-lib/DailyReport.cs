using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class DailyReport
	{
		/// <summary>
		/// The date on which this report was generated.
		/// Note that this is the "ResMed Date", which begins at noon and continues until noon the following day. 
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
		public List<MaskSession> Sessions { get; } = new List<MaskSession>();

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
		public DailyStatistics Statistics { get; private set; } = new DailyStatistics();

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
		public static DailyReport Read( Dictionary<string, double> data )
		{
			var dialy = new DailyReport();
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
