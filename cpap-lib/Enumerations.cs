namespace cpaplib
{
	public enum OperatingMode : int
	{
		UNKNOWN = -1,
		/// <summary>
		/// Constant pressure delivered at a fixed set point
		/// </summary>
		CPAP,
		/// <summary>
		/// Pressure will fluctuate between a set minimum and maximum value
		/// </summary>
		APAP,
		/// <summary>
		/// Delivers a constant fixed pressure for both inhalation (IPAP) and exhalation (EPAP) separately.
		/// The device senses when the patient is inhaling and exhaling and supplies the appropriate pressures accordingly.
		/// </summary>
		BILEVEL_FIXED,
		BILEVEL_AUTO_FIXED_PS,
		BILEVEL_AUTO_VARIABLE_PS,
		/// <summary>
		/// Adaptive Servo Ventilation mode
		/// </summary>
		ASV,
		ASV_VARIABLE_EPAP,
		AVAPS,
		MAX_VALUE = AVAPS + 1
	};

	public enum OnOffType : int
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
		/// The user has set the humidity level and heated tube temperature manually
		/// </summary>
		Manual,
		/// <summary>
		/// The system controls the humidifier and the temperature of the heated air tubing 
		/// </summary>
		Auto,
	}

	/// <summary>
	/// Expiratory Pressure Relief (EPR) reduces the delivered mask pressure during exhalation.
	/// </summary>
	public enum EprType : int
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
