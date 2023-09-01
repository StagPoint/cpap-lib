namespace cpaplib;

public class RespiratoryEvents
{
	public double AHI { get; private set; }
	public double HI  { get; private set; }
	public double AI  { get; private set; }
	public double OAI { get; private set; }
	public double CAI { get; private set; }
	public double UAI { get; private set; }
	public double RIN { get; private set; }
	public double CSR { get; private set; }

	internal void ReadFrom( Dictionary<string, double> map )
	{
		AHI = getValue( "AHI" );
		HI  = getValue( "HI" );
		AI  = getValue( "AI" );
		OAI = getValue( "OAI" );
		CAI = getValue( "CAI" );
		UAI = getValue( "UAI" );
		RIN = getValue( "RIN" );
		CSR = getValue( "CSR" );
		
		double getValue( params string[] keys )
		{
			foreach( var key in keys )
			{
				if( map.TryGetValue( key, out double value ) )
				{
					return value;
				}
			}

			return 0;
		}
	}
}

