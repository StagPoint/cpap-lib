using System;

namespace cpaplib
{
	/// <summary>
	/// Calculated statics for a signal's values across an entire day of usage
	/// </summary>
	public class SignalStatistics : IComparable<SignalStatistics>
	{
		#region Public properties 
		
		/// <summary>
		/// The name of the Signal for which these statistics have been computed
		/// </summary>
		public string SignalName { get; set; }

		/// <summary>
		/// The unit of measure (also sometimes called Physical Units) of the Signal's samples
		/// </summary>
		public string UnitOfMeasurement { get; set; }

		/// <summary>
		/// The minimum value (above zero) of the Signal's samples
		/// </summary>
		public double Minimum { get; set; }

		/// <summary>
		/// The median value of the Signal's samples 
		/// </summary>
		public double Median { get; set; }

		/// <summary>
		/// The average value of the Signal's samples 
		/// </summary>
		public double Average { get; set; }

		/// <summary>
		/// The 95th percentile value of the Signal's samples 
		/// </summary>
		public double Percentile95 { get; set; }

		/// <summary>
		/// The 99.5th percentile value of the Signal's samples 
		/// </summary>
		public double Percentile995 { get; set; }

		/// <summary>
		/// The maximum value of the Signal's samples 
		/// </summary>
		public double Maximum { get; set; }
		
		/// <summary>
		/// The average Standard Deviation (over a one minute period) from the mean of the Signal's samples 
		/// </summary>
		public double MeanDeviation { get; set; }

		#endregion 
		
		#region IComparable<SignalStatistics> implementation 

		public int CompareTo( SignalStatistics other )
		{
			return string.Compare( SignalName, other.SignalName, StringComparison.Ordinal );
		}
		
		#endregion 
		
		#region Base class overrides 
		
		public override string ToString()
		{
			return $"{SignalName, -25} Min: {Minimum:F2}, Med: {Median:F2}, Max: {Maximum:F2}";
		}
		
		#endregion 
	}
}
