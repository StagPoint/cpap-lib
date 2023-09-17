namespace cpaplib
{
	/// <summary>
	/// Settings which are used when Mode = OperatingMode.APAP 
	/// </summary>
	public class AutoSetSettings
	{
		#region Public properties 
		
		/// <summary>
		/// Indicates the speed at which pressure increases during AutoSet mode operation 
		/// </summary>
		public AutoSetResponseType ResponseType { get; set; }

		/// <summary>
		/// When Ramp is enabled, starting ramp pressure 
		/// </summary>
		public double StartPressure { get; set; }

		/// <summary>
		/// Maximum pressure
		/// </summary>
		public double MaxPressure { get; set; }

		/// <summary>
		/// Minimum pressure 
		/// </summary>
		public double MinPressure { get; set; }
		
		#endregion

		#region Base class overrides

		public override string ToString()
		{
			return $"Min: {MinPressure:F1}, Max: {MaxPressure:F1}, Start: {StartPressure:F1}, Response: {ResponseType}";
		}

		#endregion
	}
}