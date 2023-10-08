using System;

namespace cpaplib
{
	/// <summary>
	/// Calculated statics for a signal's values across an entire day of usage
	/// </summary>
	public class SignalStatistics : IComparable<SignalStatistics>
	{
		public string SignalName        { get; set; }
		public string UnitOfMeasurement { get; set; }
		public double Minimum           { get; set; }
		public double Median            { get; set; }
		public double Average           { get; set; }
		public double Percentile95      { get; set; }
		public double Percentile99      { get; set; }
		public double Maximum           { get; set; }

		public int CompareTo( SignalStatistics other )
		{
			return string.Compare( SignalName, other.SignalName, StringComparison.Ordinal );
		}
		
		public override string ToString()
		{
			return $"{SignalName, -25} Min: {Minimum:F2}, Med: {Median:F2}, Max: {Maximum:F2}";
		}
	}
}
