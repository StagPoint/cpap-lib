namespace cpaplib
{
	public class StatisticsSummary
	{
		public double Leak95     { get; set; }
		public double LeakMedian { get; set; }
		
		public double RespirationRateMax    { get; set; }
		public double RespirationRate95     { get; set; }
		public double RespirationRateMedian { get; set; }
		
		public double MinuteVentilationMax    { get; set; }
		public double MinuteVentilation95     { get; set; }
		public double MinuteVentilationMedian { get; set; }
		
		public double TidalVolumeMax    { get; set; }
		public double TidalVolume95     { get; set; }
		public double TidalVolumeMedian { get; set; }
		
		public double PressureMax    { get; set; }
		public double Pressure95     { get; set; }
		public double PressureMedian { get; set; }
		
		public double TargetIpapMax    { get; set; }
		public double TargetIpap95     { get; set; }
		public double TargetIpapMedian { get; set; }
		
		public double TargetEpapMax    { get; set; }
		public double TargetEpap95     { get; set; }
		public double TargetEpapMedian { get; set; }
	}
}
