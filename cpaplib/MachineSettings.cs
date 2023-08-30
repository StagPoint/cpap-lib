using StagPoint.EDF.Net;

namespace cpaplib;

public class MachineSettings
{
	public DateTime Date                        { get; private set; }
	public double MaskOn                      { get; private set; }
	public double MaskOff                     { get; private set; }
	public double MaskEvents                  { get; private set; }
	public double Duration                    { get; private set; }
	public double OnDuration                  { get; private set; }
	public double PatientHours                { get; private set; }
	public double Mode                        { get; private set; }
	public double Settings_RampEnable         { get; private set; }
	public double Settings_RampTime           { get; private set; }
	public double Settings_CPAP_StartPress    { get; private set; }
	public double Settings_CPAP_Press         { get; private set; }
	public double Settings_EPR_ClinEnable     { get; private set; }
	public double Settings_EPR_EPREnable      { get; private set; }
	public double Settings_EPR_Level          { get; private set; }
	public double Settings_EPR_EPRType        { get; private set; }
	public double Settings_AutoSet_Comfort    { get; private set; }
	public double Settings_AutoSet_StartPress { get; private set; }
	public double Settings_AutoSet_MaxPress   { get; private set; }
	public double Settings_AutoSet_MinPress   { get; private set; }
	public double Settings_SmartStart         { get; private set; }
	public double Settings_PtAccess           { get; private set; }
	public double Settings_ABFilter           { get; private set; }
	public double Settings_Mask               { get; private set; }
	public double Settings_Tube               { get; private set; }
	public double Settings_ClimateControl     { get; private set; }
	public double Settings_HumEnable          { get; private set; }
	public double Settings_HumLevel           { get; private set; }
	public double Settings_TempEnable         { get; private set; }
	public double Settings_Temp               { get; private set; }
	public double HeatedTube                  { get; private set; }
	public double Humidifier                  { get; private set; }
	public double BlowPress_95                { get; private set; }
	public double BlowPress_5                 { get; private set; }
	public double Flow_95                     { get; private set; }
	public double Flow_5                      { get; private set; }
	public double BlowFlow_50                 { get; private set; }
	public double AmbHumidity_50              { get; private set; }
	public double HumTemp_50                  { get; private set; }
	public double HTubeTemp_50                { get; private set; }
	public double HTubePow_50                 { get; private set; }
	public double HumPow_50                   { get; private set; }
	public double SpO2_50                     { get; private set; }
	public double SpO2_95                     { get; private set; }
	public double SpO2_Max                    { get; private set; }
	public double SpO2Thresh                  { get; private set; }
	public double MaskPress_50                { get; private set; }
	public double MaskPress_95                { get; private set; }
	public double MaskPress_Max               { get; private set; }
	public double TgtIPAP_50                  { get; private set; }
	public double TgtIPAP_95                  { get; private set; }
	public double TgtIPAP_Max                 { get; private set; }
	public double TgtEPAP_50                  { get; private set; }
	public double TgtEPAP_95                  { get; private set; }
	public double TgtEPAP_Max                 { get; private set; }
	public double Leak_50                     { get; private set; }
	public double Leak_95                     { get; private set; }
	public double Leak_70                     { get; private set; }
	public double Leak_Max                    { get; private set; }
	public double MinVent_50                  { get; private set; }
	public double MinVent_95                  { get; private set; }
	public double MinVent_Max                 { get; private set; }
	public double RespRate_50                 { get; private set; }
	public double RespRate_95                 { get; private set; }
	public double RespRate_Max                { get; private set; }
	public double TidVol_50                   { get; private set; }
	public double TidVol_95                   { get; private set; }
	public double TidVol_Max                  { get; private set; }
	public double AHI                         { get; private set; }
	public double HI                          { get; private set; }
	public double AI                          { get; private set; }
	public double OAI                         { get; private set; }
	public double CAI                         { get; private set; }
	public double UAI                         { get; private set; }
	public double RIN                         { get; private set; }
	public double CSR                         { get; private set; }
	public double Fault_Device                { get; private set; }
	public double Fault_Alarm                 { get; private set; }
	public double Fault_Humidifier            { get; private set; }
	public double Fault_HeatedTube            { get; private set; }

	public void ReadFrom( Dictionary<string,double> map )
	{
		Date                        = new DateTime( 1970, 1, 1 ).AddDays( map[ "Date" ] );
		MaskOn                      = map[ "MaskOn" ];
		MaskOff                     = map[ "MaskOff" ];
		MaskEvents                  = map[ "MaskEvents" ];
		Duration                    = map[ "Duration" ];
		OnDuration                  = map[ "OnDuration" ];
		PatientHours                = map[ "PatientHours" ];
		Mode                        = map[ "Mode" ];
		Settings_RampEnable         = map[ "S.RampEnable" ];
		Settings_RampTime           = map[ "S.RampTime" ];
		Settings_CPAP_StartPress    = map[ "S.C.StartPress" ];
		Settings_CPAP_Press         = map[ "S.C.Press" ];
		Settings_EPR_ClinEnable     = map[ "S.EPR.ClinEnable" ];
		Settings_EPR_EPREnable      = map[ "S.EPR.EPREnable" ];
		Settings_EPR_Level          = map[ "S.EPR.Level" ];
		Settings_EPR_EPRType        = map[ "S.EPR.EPRType" ];
		Settings_AutoSet_Comfort    = map[ "S.AS.Comfort" ];
		Settings_AutoSet_StartPress = map[ "S.AS.StartPress" ];
		Settings_AutoSet_MaxPress   = map[ "S.AS.MaxPress" ];
		Settings_AutoSet_MinPress   = map[ "S.AS.MinPress" ];
		Settings_SmartStart         = map[ "S.SmartStart" ];
		Settings_PtAccess           = map[ "S.PtAccess" ];
		Settings_ABFilter           = map[ "S.ABFilter" ];
		Settings_Mask               = map[ "S.Mask" ];
		Settings_Tube               = map[ "S.Tube" ];
		Settings_ClimateControl     = map[ "S.ClimateControl" ];
		Settings_HumEnable          = map[ "S.HumEnable" ];
		Settings_HumLevel           = map[ "S.HumLevel" ];
		Settings_TempEnable         = map[ "S.TempEnable" ];
		Settings_Temp               = map[ "S.Temp" ];
		HeatedTube                  = map[ "HeatedTube" ];
		Humidifier                  = map[ "Humidifier" ];
		BlowPress_95                = map[ "BlowPress.95" ];
		BlowPress_5                 = map[ "BlowPress.5" ];
		Flow_95                     = map[ "Flow.95" ];
		Flow_5                      = map[ "Flow.5" ];
		BlowFlow_50                 = map[ "BlowFlow.50" ];
		AmbHumidity_50              = map[ "AmbHumidity.50" ];
		HumTemp_50                  = map[ "HumTemp.50" ];
		HTubeTemp_50                = map[ "HTubeTemp.50" ];
		HTubePow_50                 = map[ "HTubePow.50" ];
		HumPow_50                   = map[ "HumPow.50" ];
		SpO2_50                     = map[ "SpO2.50" ];
		SpO2_95                     = map[ "SpO2.95" ];
		SpO2_Max                    = map[ "SpO2.Max" ];
		SpO2Thresh                  = map[ "SpO2Thresh" ];
		MaskPress_50                = map[ "MaskPress.50" ];
		MaskPress_95                = map[ "MaskPress.95" ];
		MaskPress_Max               = map[ "MaskPress.Max" ];
		TgtIPAP_50                  = map[ "TgtIPAP.50" ];
		TgtIPAP_95                  = map[ "TgtIPAP.95" ];
		TgtIPAP_Max                 = map[ "TgtIPAP.Max" ];
		TgtEPAP_50                  = map[ "TgtEPAP.50" ];
		TgtEPAP_95                  = map[ "TgtEPAP.95" ];
		TgtEPAP_Max                 = map[ "TgtEPAP.Max" ];
		Leak_50                     = map[ "Leak.50" ];
		Leak_95                     = map[ "Leak.95" ];
		Leak_70                     = map[ "Leak.70" ];
		Leak_Max                    = map[ "Leak.Max" ];
		MinVent_50                  = map[ "MinVent.50" ];
		MinVent_95                  = map[ "MinVent.95" ];
		MinVent_Max                 = map[ "MinVent.Max" ];
		RespRate_50                 = map[ "RespRate.50" ];
		RespRate_95                 = map[ "RespRate.95" ];
		RespRate_Max                = map[ "RespRate.Max" ];
		TidVol_50                   = map[ "TidVol.50" ];
		TidVol_95                   = map[ "TidVol.95" ];
		TidVol_Max                  = map[ "TidVol.Max" ];
		AHI                         = map[ "AHI" ];
		HI                          = map[ "HI" ];
		AI                          = map[ "AI" ];
		OAI                         = map[ "OAI" ];
		CAI                         = map[ "CAI" ];
		UAI                         = map[ "UAI" ];
		RIN                         = map[ "RIN" ];
		CSR                         = map[ "CSR" ];
		Fault_Device                = map[ "Fault.Device" ];
		Fault_Alarm                 = map[ "Fault.Alarm" ];
		Fault_Humidifier            = map[ "Fault.Humidifier" ];
		Fault_HeatedTube            = map[ "Fault.HeatedTube" ];
	}
}
