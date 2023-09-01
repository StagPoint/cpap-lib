using System.Diagnostics;
using System.Numerics;

using StagPoint.EDF.Net;

namespace cpaplib;

public class DailyReport
{
	/// <summary>
	/// The date on which this report was generated.
	/// Note that this is the "ResMed Date", which begins at noon and continues until noon the following day. 
	/// </summary>
	public DateTime Date { get; private set; }

	/// <summary>
	/// The list of sessions  for this day
	/// </summary>
	public List<MaskSession> Sessions { get; private set; } = new();

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
	public RespiratoryEvents EventSummary { get; private set; } = new RespiratoryEvents();

	/// <summary>
	/// The total amount of time the CPAP was used on the reported day 
	/// </summary>
	public TimeSpan Duration { get; private set; }

	public TimeSpan OnDuration     { get; private set; }
	public double   PatientHours   { get; private set; }
	
	private Dictionary<string, double> _map = null;
	
	#region Public functions 
	
	public static DailyReport Read( Dictionary<string, double> map )
	{
		var dialy = new DailyReport();
		dialy.ReadFrom( map );

		return dialy;
	}

	public void ReadFrom( Dictionary<string,double> map )
	{
		_map = map;
		
		Date = new DateTime( 1970, 1, 1 ).AddDays( map[ "Date" ] ).AddHours( 12 );

		Settings.ReadFrom( map );
		Statistics.ReadFrom( map );
		EventSummary.ReadFrom( map );

		MaskEvents = (int)(map[ "MaskEvents" ] / 2);
		Duration   = TimeSpan.FromMinutes( map[ "Duration" ] );
		OnDuration = TimeSpan.FromMinutes( map[ "OnDuration" ] );

		PatientHours = getValue( "PatientHours" );

		Fault.Device     = getValue( "Fault.Device" );
		Fault.Alarm      = getValue( "Fault.Alarm" );
		Fault.Humidifier = getValue( "Fault.Humidifier" );
		Fault.HeatedTube = getValue( "Fault.HeatedTube" );

		double getValue( params string[] keys )
		{
			foreach( var key in keys )
			{
				if( map.TryGetValue( key, out double value ) )
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
			return $"{Date.ToLongDateString()}   {Sessions.First().StartTime.ToShortTimeString()} - {Sessions.Last().EndTime.ToShortTimeString()}    ({Duration})";
		}
		
		return $"{Date.ToLongDateString()}   ({Duration})";
	}

	#endregion
}
