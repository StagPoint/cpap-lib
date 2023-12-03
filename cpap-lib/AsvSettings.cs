namespace cpaplib
{
	public class AsvSettings
	{
		public double StartPressure      { get; set; }
		public double MinPressureSupport { get; set; }
		public double MaxPressureSupport { get; set; }

		public double EPAP    { get; set; }
		public double EpapMin { get; set; }
		public double EpapMax { get; set; }
		
		public double IPAP    { get; set; }
		public double IpapMin { get; set; }
		public double IpapMax { get; set; }
	}
}
