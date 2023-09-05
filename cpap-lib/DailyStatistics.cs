using System.Collections.Generic;

namespace cpaplib
{
	public class DailyStatistics
	{
		#region Public properties

		public SignalStatistics MaskPressure       { get; set; }
		public SignalStatistics TherapyPressure    { get; set; }
		public SignalStatistics ExpiratoryPressure { get; set; }
		public SignalStatistics Leak               { get; set; }
		public SignalStatistics RespirationRate    { get; set; }
		public SignalStatistics TidalVolume        { get; set; }
		public SignalStatistics MinuteVent         { get; set; }
		public SignalStatistics Snore              { get; set; }
		public SignalStatistics FlowLimit          { get; set; }
		public SignalStatistics Pulse              { get; set; }
		public SignalStatistics SpO2               { get; set; }

		#endregion
	}
}
