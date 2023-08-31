using System.Diagnostics;

using StagPoint.EDF.Net;

namespace cpaplib;

public class MachineSettings
{
	#region Public properties

	public EPR_Settings     EPR     { get; set; } = new();
	public CPAP_Settings    CPAP    { get; set; } = new();
	public AutoSet_Settings AutoSet { get; set; } = new();

	public RampModeType       RampMode            { get; set; }
	public double             RampTime            { get; set; }
	public EssentialsMode     Essentials          { get; set; }
	public bool               AntibacterialFilter { get; set; }
	public MaskType           Mask                { get; set; }
	public double             Tube                { get; set; }
	public ClimateControlType ClimateControl      { get; set; }
	public bool               HumidityEnabled     { get; set; }
	public double             HumidityLevel       { get; set; }
	public bool               TemperatureEnabled  { get; set; }
	public double             TemperatureLevel    { get; set; }
	public bool               SmartStart          { get; set; }
	public double             PtAccess            { get; set; }
	public double             Temp                { get; set; }
	
	#endregion 
	
	#region Nested types

	public enum RampModeType
	{
		Off,
		On,
		Auto,
	}

	public enum MaskType
	{
		Pillows,
		FullFace,
		Nasal,
		Unknown
	}

	public enum EssentialsMode
	{
		Plus,
		On
	}

	public enum ClimateControlType
	{
		Manual, 
		Auto,
	}

	public enum EPR_Type : int
	{
		Off,
		RampOnly,
		FullTime,
	}
	
	public class CPAP_Settings
	{
		public double StartPressure { get; set; } = 0.0;
		public double Pressure      { get; set; } = 0.0;
	}

	public class EPR_Settings
	{
		public EPR_Type Mode             { get; set; }
		public bool     ClinicianEnabled { get; set; }
		public bool     EprEnabled       { get; set; }
		public int      Level            { get; set; }
	}

	public enum AutoSetResponseType
	{
		Standard = 0,
		Soft = 1
	}

	public class AutoSet_Settings
	{
		public AutoSetResponseType ResponseType;
		public double StartPressure;
		public double MaxPressure;
		public double MinPressure;
	}
	
	#endregion
	
	#region Public functions
	
	internal void ReadFrom( Dictionary<string, double> map )
	{
		CPAP.StartPressure = map[ "S.C.StartPress" ];
		CPAP.Pressure      = map[ "S.C.Press" ];
		
		EPR.ClinicianEnabled = map[ "S.EPR.ClinEnable" ] >= 0.5;
		EPR.EprEnabled       = map[ "S.EPR.EPREnable" ] >= 0.5;
		EPR.Level            = (int)map[ "S.EPR.Level" ];
		EPR.Mode             = (EPR_Type)(int)(map[ "S.EPR.EPRType" ] + 1);

		AutoSet.ResponseType       = (AutoSetResponseType)(int)map[ "S.AS.Comfort" ];
		AutoSet.StartPressure = map[ "S.AS.StartPress" ];
		AutoSet.MaxPressure   = map[ "S.AS.MaxPress" ];
		AutoSet.MinPressure   = map[ "S.AS.MinPress" ];
		
		RampMode = (RampModeType)(int)map[ "S.RampEnable" ];
		RampTime = map[ "S.RampTime" ];

		SmartStart          = map[ "S.SmartStart" ] > 0.5;
		AntibacterialFilter = map[ "S.ABFilter" ] >= 0.5;
		
		ClimateControl     = (ClimateControlType)(int)map[ "S.ClimateControl" ];
		Tube               = map[ "S.Tube" ];
		HumidityEnabled    = map[ "S.HumEnable" ] > 0.5;
		HumidityLevel      = map[ "S.HumLevel" ];
		TemperatureEnabled = map[ "S.TempEnable" ] > 0.5;
		Temp               = map[ "S.Temp" ];
		
		Mask = (MaskType)(int)map[ "S.Mask" ];

		Essentials = map[ "S.PtAccess" ] > 0.5 ? EssentialsMode.On : EssentialsMode.Plus;
		
		PtAccess = map[ "S.PtAccess" ];
	}
	
	#endregion 
}

public class DailyReport
{
	public DateTime       Date    { get; private set; }
	public List<DateTime> MaskOn  { get; private set; } = new();
	public List<DateTime> MaskOff { get; private set; } = new();

	public int MaskEvents { get; private set; }

	public FaultInfo Fault { get; set; } = new FaultInfo();

	public MachineSettings Settings { get; set; } = new MachineSettings();

	/// <summary>
	/// The amount of time the CPAP was used
	/// </summary>
	public TimeSpan Duration { get; private set; }

	public TimeSpan OnDuration     { get; private set; }
	public double   PatientHours   { get; private set; }
	public CpapMode Mode           { get; private set; }
	public double   HeatedTube     { get; private set; }
	public bool     Humidifier     { get; private set; }
	public double   BlowPress_95   { get; private set; }
	public double   BlowPress_5    { get; private set; }
	public double   Flow_95        { get; private set; }
	public double   Flow_5         { get; private set; }
	public double   BlowFlow_50    { get; private set; }
	public double   AmbHumidity_50 { get; private set; }
	public double   HumTemp_50     { get; private set; }
	public double   HTubeTemp_50   { get; private set; }
	public double   HTubePow_50    { get; private set; }
	public double   HumPow_50      { get; private set; }
	public double   SpO2_50        { get; private set; }
	public double   SpO2_95        { get; private set; }
	public double   SpO2_Max       { get; private set; }
	public double   SpO2Thresh     { get; private set; }
	public double   MaskPress_50   { get; private set; }
	public double   MaskPress_95   { get; private set; }
	public double   MaskPress_Max  { get; private set; }
	public double   TgtIPAP_50     { get; private set; }
	public double   TgtIPAP_95     { get; private set; }
	public double   TgtIPAP_Max    { get; private set; }
	public double   TgtEPAP_50     { get; private set; }
	public double   TgtEPAP_95     { get; private set; }
	public double   TgtEPAP_Max    { get; private set; }
	public double   Leak_50        { get; private set; }
	public double   Leak_95        { get; private set; }
	public double   Leak_70        { get; private set; }
	public double   Leak_Max       { get; private set; }
	public double   MinVent_50     { get; private set; }
	public double   MinVent_95     { get; private set; }
	public double   MinVent_Max    { get; private set; }
	public double   RespRate_50    { get; private set; }
	public double   RespRate_95    { get; private set; }
	public double   RespRate_Max   { get; private set; }
	public double   TidVol_50      { get; private set; }
	public double   TidVol_95      { get; private set; }
	public double   TidVol_Max     { get; private set; }
	public double   AHI            { get; private set; }
	public double   HI             { get; private set; }
	public double   AI             { get; private set; }
	public double   OAI            { get; private set; }
	public double   CAI            { get; private set; }
	public double   UAI            { get; private set; }
	public double   RIN            { get; private set; }
	public double   CSR            { get; private set; }
	
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

		if( map.TryGetValue( "CPAP_MODE", out double S9_mode ) )
		{
			switch( (int)S9_mode )
			{
				case 1:
				case 2:
					Mode = CpapMode.APAP;
					break;
				case 3:
					Mode = CpapMode.CPAP;
					break;
				default:
					Mode = CpapMode.UNKNOWN;
					break;
			}
		}
		else
		{
			var mode = (int)getValue( "Mode" );
			if( mode >= (int)CpapMode.MAX_VALUE )
			{
				mode = -1;
			}

			Mode = (CpapMode)mode;
		}

		Settings.ReadFrom( map );

		MaskEvents = (int)(map[ "MaskEvents" ] / 2);
		Duration   = TimeSpan.FromMinutes( map[ "Duration" ] );
		OnDuration = TimeSpan.FromMinutes( map[ "OnDuration" ] );

		PatientHours   = getValue( "PatientHours" );
		HeatedTube     = getValue( "HeatedTube" );
		Humidifier     = getValue( "Humidifier" ) > 0.5;
		BlowPress_95   = getValue( "BlowPress.95" );
		BlowPress_5    = getValue( "BlowPress.5" );
		Flow_95        = getValue( "Flow.95" );
		Flow_5         = getValue( "Flow.5" );
		BlowFlow_50    = getValue( "BlowFlow.50" );
		AmbHumidity_50 = getValue( "AmbHumidity.50" );
		HumTemp_50     = getValue( "HumTemp.50" );
		HTubeTemp_50   = getValue( "HTubeTemp.50" );
		HTubePow_50    = getValue( "HTubePow.50" );
		HumPow_50      = getValue( "HumPow.50" );
		SpO2_50        = getValue( "SpO2.50" );
		SpO2_95        = getValue( "SpO2.95" );
		SpO2_Max       = getValue( "SpO2.Max" );
		SpO2Thresh     = getValue( "SpO2Thresh" );
		MaskPress_50   = getValue( "MaskPress.50" );
		MaskPress_95   = getValue( "MaskPress.95" );
		MaskPress_Max  = getValue( "MaskPress.Max" );
		TgtIPAP_50     = getValue( "TgtIPAP.50" );
		TgtIPAP_95     = getValue( "TgtIPAP.95" );
		TgtIPAP_Max    = getValue( "TgtIPAP.Max" );
		TgtEPAP_50     = getValue( "TgtEPAP.50" );
		TgtEPAP_95     = getValue( "TgtEPAP.95" );
		TgtEPAP_Max    = getValue( "TgtEPAP.Max" );
		Leak_50        = getValue( "Leak.50" );
		Leak_95        = getValue( "Leak.95" );
		Leak_70        = getValue( "Leak.70" );
		Leak_Max       = getValue( "Leak.Max" );
		MinVent_50     = getValue( "MinVent.50" );
		MinVent_95     = getValue( "MinVent.95" );
		MinVent_Max    = getValue( "MinVent.Max" );
		RespRate_50    = getValue( "RespRate.50" );
		RespRate_95    = getValue( "RespRate.95" );
		RespRate_Max   = getValue( "RespRate.Max" );
		TidVol_50      = getValue( "TidVol.50" );
		TidVol_95      = getValue( "TidVol.95" );
		TidVol_Max     = getValue( "TidVol.Max" );
		AHI            = getValue( "AHI" );
		HI             = getValue( "HI" );
		AI             = getValue( "AI" );
		OAI            = getValue( "OAI" );
		CAI            = getValue( "CAI" );
		UAI            = getValue( "UAI" );
		RIN            = getValue( "RIN" );
		CSR            = getValue( "CSR" );
		
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
	
	#region Nested types 
	
	public class FaultInfo
	{
		public double Device     { get; set; }
		public double Alarm      { get; set; }
		public double Humidifier { get; set; }
		public double HeatedTube { get; set; }
	}
	
	public enum CpapMode : int 
	{ 
		UNKNOWN = -1,
		CPAP, 
		APAP, 
		BILEVEL_FIXED, 
		BILEVEL_AUTO_FIXED_PS, 
		BILEVEL_AUTO_VARIABLE_PS, 
		ASV, 
		ASV_VARIABLE_EPAP, 
		AVAPS, 
		MAX_VALUE = AVAPS + 1
	};

	#endregion 
}
