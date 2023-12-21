namespace cpap_db.Converters;

public class SignalDataBlobConverter : IBlobTypeConverter
{
	#region Properties 
	
	/// <summary>
	/// Should be set to the minimum possible value stored by *any* Signal that will be stored in the database
	/// </summary>
	public double DataMinimum   { get; set; } = -512;
	
	/// <summary>
	/// Should be set to the maximum possible value stored by *any* Signal that will be stored in the database 
	/// </summary>
	public double DataMaximum   { get; set; } = 5000;
	
	public double StoredMinimum { get; set; } = 0;
	public double StoredMaximum { get; set; } = ushort.MaxValue;
	
	private double Gain { get => (DataMaximum - DataMinimum) / (StoredMaximum - StoredMinimum); }

	private double Offset { get => (DataMaximum / Gain) - StoredMaximum; }

	#endregion 
	
	#region IBlobTypeConverter interface implementation 

	public byte[] ConvertToBlob( object value )
	{
		if( value is not List<double> list )
		{
			throw new InvalidCastException( "Expected a value of type List<double>" );
		}

		var byteBuffer = new byte[ list.Count * sizeof( ushort ) ];

		using var stream = new MemoryStream( byteBuffer, true );
		using var writer = new BinaryWriter( stream );

		// Dereference to reduce extraneous computation
		var gain   = Gain;
		var offset = Offset;
		
		foreach( var element in list )
		{
			ushort outputValue = (ushort)(element / gain - offset);
			writer.Write( outputValue );
		}

		return byteBuffer;
	}
	
	public object ConvertFromBlob( byte[] data )
	{
		using var stream = new MemoryStream( data, false );
		using var reader = new BinaryReader( stream );

		var result = new List<double>( data.Length / sizeof( ushort ) );

		// Dereference to reduce extraneous computation
		var gain   = Gain;
		var offset = Offset;
		
		while( stream.Position < data.Length )
		{
			var sample      = reader.ReadUInt16();
			var scaledValue = gain * (sample + offset);

			result.Add( scaledValue );
		}

		return result;
	}
	
	#endregion 
}
