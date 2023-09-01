namespace cpaplib;

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

public enum EprType : int
{
	Off,
	RampOnly,
	FullTime,
}

public enum AutoSetResponseType
{
	Standard = 0,
	Soft     = 1
}

