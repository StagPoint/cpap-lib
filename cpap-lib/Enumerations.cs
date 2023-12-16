namespace cpaplib
{
	public enum MachineManufacturer
	{
		ResMed,
		PhilipsRespironics,
	}
	
	public enum OperatingMode
	{
		UNKNOWN = -1,
		/// <summary>
		/// Constant Positive Air Pressure
		/// </summary>
		Cpap,
		/// <summary>
		/// Auto-titrating Positive Air Pressure
		/// </summary>
		Apap,
		/// <summary>
		/// Fixed Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// </summary>
		BilevelFixed,
		/// <summary>
		/// Auto-titrated Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// with fixed Pressure Support
		/// </summary>
		BilevelAutoFixedPS,
		/// <summary>
		/// Auto-titrated Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// with variable Pressure Support
		/// </summary>
		BilevelAutoVariablePS,
		/// <summary>
		/// Adaptive Servo Ventilation
		/// </summary>
		Asv,
		/// <summary>
		/// Adaptive Servo Ventilation (ASV) with variable Expiratory Positive Airway Pressure (EPAP). Also known as ASV Auto.
		/// </summary>
		AsvVariableEpap,
		/// <summary>
		/// Average Volume-Assured Pressure Support
		/// </summary>
		Avaps,
		MAX_VALUE = Avaps + 1
	};

	public enum OnOffType
	{
		Off,
		On
	}

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

	/// <summary>
	/// On ResMed devices, describes what menu options are shown to the user on the menu screen.
	/// When Essentials Mode is set to Plus, there are more options available to the user.
	/// </summary>
	public enum EssentialsMode
	{
		Plus,
		On
	}

	/// <summary>
	/// On ResMed devices, describes how the humidifier and heated tubing (if present) are controlled.
	/// </summary>
	public enum ClimateControlType
	{
		/// <summary>
		/// The system controls the humidifier and the temperature of the heated air tubing 
		/// </summary>
		Auto,
		/// <summary>
		/// The user has set the humidity level and heated tube temperature manually
		/// </summary>
		Manual,
	}

	/// <summary>
	/// On ResMed AutoSense devices, Expiratory Pressure Relief (EPR) is described as a comfort feature that
	/// reduces the delivered mask pressure during exhalation.
	/// </summary>
	public enum EprType
	{
		/// <summary>
		/// EPR is not active
		/// </summary>
		Off,
		/// <summary>
		/// EPR is active only during the pressure ramp phase
		/// </summary>
		RampOnly,
		/// <summary>
		/// EPR is active at all times 
		/// </summary>
		FullTime,
	}

	/// <summary>
	/// On ResMed AutoSense devices, indicates the speed at which pressure increases during AutoSet mode operation 
	/// </summary>
	public enum AutoSetResponseType
	{
		/// <summary>
		/// Default pressure increase rate. 
		/// </summary>
		Standard = 0,
		/// <summary>
		/// Pressure rises slower and more gently than it does in Standard mode.
		/// </summary>
		Soft = 1
	}
	
	/// <summary>
	/// On Philips Respironics System One devices, specifies the Flex Mode in use for a given Session
	/// </summary>
	public enum FlexMode
	{
		Unknown = -1,
		None, 
		CFlex,
		CFlexPlus,
		AFlex,
		RiseTime, 
		BiFlex,
		PFlex, 
		Flex, 
	};

	/// <summary>
	/// On Philips Respironics System One devices, specifies the Humidifier Mode in use for a given Session
	/// </summary>
	public enum HumidifierMode
	{
		Fixed, 
		Adaptive, 
		HeatedTube, 
		Passover, 
		Error,
	}
}
