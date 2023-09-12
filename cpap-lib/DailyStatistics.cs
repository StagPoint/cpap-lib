using System.Collections.Generic;

namespace cpaplib
{
	public class DailyStatistics
	{
		#region Public properties

		public SignalStatistics MaskPressure       { get; set; } = new SignalStatistics();
		public SignalStatistics TherapyPressure    { get; set; } =  new SignalStatistics();
		public SignalStatistics ExpiratoryPressure { get; set; } =  new SignalStatistics();
		public SignalStatistics Leak               { get; set; } =  new SignalStatistics();
		public SignalStatistics RespirationRate    { get; set; } =  new SignalStatistics();
		public SignalStatistics TidalVolume        { get; set; } =  new SignalStatistics();
		public SignalStatistics MinuteVent         { get; set; } =  new SignalStatistics();
		public SignalStatistics Snore              { get; set; } =  new SignalStatistics();
		public SignalStatistics FlowLimit          { get; set; } =  new SignalStatistics();
		public SignalStatistics Pulse              { get; set; } =  new SignalStatistics();
		public SignalStatistics SpO2               { get; set; } =  new SignalStatistics();

		#endregion
	}
}
