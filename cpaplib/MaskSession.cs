namespace cpaplib;

public class MaskSession
{
	public DateTime StartTime { get; set; }
	public DateTime EndTime   { get; set; }
	
	#region Base class overrides

	public override string ToString()
	{
		return $"{StartTime.ToShortDateString()}    {StartTime.ToLongTimeString()} - {EndTime.ToLongTimeString()}";
	}

	#endregion 
}
