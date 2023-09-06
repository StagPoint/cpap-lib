namespace cpaplib
{
	public class AvapsSettings
	{
		public double StartPressure { get; set; }
		public double MinPressure   { get; set; }
		public double MaxPressure   { get; set; }
		
		public bool   EpapAuto { get; set; }
		public double EPAP     { get; set; }
		public double EpapMin  { get; set; }
		public double EpapMax  { get; set; }
		
		public double IPAP    { get; set; }
		public double IpapMin { get; set; }
		public double IpapMax { get; set; }
	}
}
