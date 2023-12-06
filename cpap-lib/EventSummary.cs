namespace cpaplib
{
	/// <summary>
	/// The summary of events provided by the machine. This summary is not updated once
	/// the data has been initially imported, and is only referred to when there is
	/// no detailed information available (such as when the user did not have an SD Card
	/// in the machine, etc.)
	/// </summary>
	public class EventSummary
	{
		public double AHI                      { get; set; }
		public double ApneaIndex               { get; set; }
		public double HypopneaIndex            { get; set; }
		public double ObstructiveApneaIndex    { get; set; }
		public double CentralApneaIndex        { get; set; }
		public double UnclassifiedApneaIndex   { get; set; }
		public double RespiratoryArousalIndex  { get; set; }
		public double CheynesStokesRespiration { get; set; }
	}
}
