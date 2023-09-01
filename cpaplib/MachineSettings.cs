namespace cpaplib;

public class MachineSettings
{
	#region Public properties

	/// <summary>
	/// The mode (CPAP, AutoSet, BiLevel, etc.) used on the reported day
	/// </summary>
	public CpapMode Mode { get; private set; }

	public bool HeatedTube { get; private set; }

	public bool Humidifier { get; private set; }

	public EprSettings     EPR        { get; set; } = new();
	public CpapSettings    CPAP       { get; set; } = new();
	public AutoSetSettings AutoSet    { get; set; } = new();

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
	public double             Temperature         { get; set; }
	public bool               SmartStart          { get; set; }
	public double             PtAccess            { get; set; }
	
	#endregion 
	
	#region Public functions
	
	internal void ReadFrom( Dictionary<string, double> map )
	{
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
			var mode = (int)map[ "Mode" ];
			if( mode >= (int)CpapMode.MAX_VALUE )
			{
				mode = -1;
			}

			Mode = (CpapMode)mode;
		}

		HeatedTube = map[ "HeatedTube" ] > 0.5;
		Humidifier = map[ "Humidifier" ] > 0.5;
		
		CPAP.StartPressure = map[ "S.C.StartPress" ];
		CPAP.Pressure      = map[ "S.C.Press" ];
		
		EPR.ClinicianEnabled = map[ "S.EPR.ClinEnable" ] >= 0.5;
		EPR.EprEnabled       = map[ "S.EPR.EPREnable" ] >= 0.5;
		EPR.Level            = (int)map[ "S.EPR.Level" ];
		EPR.Mode             = (EprType)(int)(map[ "S.EPR.EPRType" ] + 1);

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
		Temperature               = map[ "S.Temp" ];
		
		Mask = (MaskType)(int)map[ "S.Mask" ];

		Essentials = map[ "S.PtAccess" ] > 0.5 ? EssentialsMode.On : EssentialsMode.Plus;
		
		PtAccess = map[ "S.PtAccess" ];
	}
	
	#endregion 
}

