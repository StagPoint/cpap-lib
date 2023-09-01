namespace cpaplib
{
	/// <summary>
	/// Settings which are used when Mode = OperatingMode.APAP 
	/// </summary>
	public class CpapSettings
	{
		/// <summary>
		/// When Ramp is enabled, this is the starting ramp pressureS
		/// </summary>
		public double StartPressure { get; set; } = 0.0;

		/// <summary>
		/// The fixed pressure that will be delivered (except during any ramp period)
		/// </summary>
		public double Pressure { get; set; } = 0.0;

	#region Base class overrides

		public override string ToString()
		{
			return $"Pressure: {Pressure:F1} cmH20";
		}

	#endregion
	}
}
