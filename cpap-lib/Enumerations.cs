namespace cpaplib
{
	public enum OperatingMode
	{
		UNKNOWN = -1,
		/// <summary>
		/// Constant Positive Air Pressure
		/// </summary>
		CPAP,
		/// <summary>
		/// Auto-titrating Positive Air Pressure
		/// </summary>
		APAP,
		/// <summary>
		/// Fixed Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// </summary>
		BILEVEL_FIXED,
		/// <summary>
		/// Auto-titrated Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// with fixed Pressure Support
		/// </summary>
		BILEVEL_AUTO_FIXED_PS,
		/// <summary>
		/// Auto-titrated Inspiratory Positive Airway Pressure (IPAP) and Expiratory Positive Airway Pressure (EPAP)
		/// with variable Pressure Support
		/// </summary>
		BILEVEL_AUTO_VARIABLE_PS,
		/// <summary>
		/// Adaptive Servo Ventilation
		/// </summary>
		ASV,
		/// <summary>
		/// Adaptive Servo Ventilation (ASV) with variable Expiratory Positive Airway Pressure (EPAP)
		/// </summary>
		ASV_VARIABLE_EPAP,
		/// <summary>
		/// Average Volume-Assured Pressure Support
		/// </summary>
		AVAPS,
		MAX_VALUE = AVAPS + 1
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

	public enum EssentialsMode
	{
		Plus,
		On
	}

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
	/// Expiratory Pressure Relief (EPR) reduces the delivered mask pressure during exhalation.
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
	/// Indicates the speed at which pressure increases during AutoSet mode operation 
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
}
