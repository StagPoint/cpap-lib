using System;

namespace cpap_app.Helpers;

public class WaveformFilter
{
	public enum PassType
	{
		Lowpass,
		Highpass,
	}

	private readonly double a1, a2, a3, b1, b2;

	private double[] inputHistory  = new double[ 2 ];
	private double[] outputHistory = new double[ 3 ];

	public WaveformFilter( double frequency, int sampleRate, PassType passType = PassType.Lowpass, double resonance = 1.414213562373095 )
	{
		double c;
		
		switch( passType )
		{
			case PassType.Lowpass:
				c  = 1.0f / Math.Tan( Math.PI * frequency / sampleRate );
				a1 = 1.0f / (1.0f + resonance * c + c * c);
				a2 = 2f * a1;
				a3 = a1;
				b1 = 2.0f * (1.0f - c * c) * a1;
				b2 = (1.0f - resonance * c + c * c) * a1;
				break;
			case PassType.Highpass:
				c  = Math.Tan( Math.PI * frequency / sampleRate );
				a1 = 1.0f / (1.0f + resonance * c + c * c);
				a2 = -2f * a1;
				a3 = a1;
				b1 = 2.0f * (c * c - 1.0f) * a1;
				b2 = (1.0f - resonance * c + c * c) * a1;
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( passType ), passType, null );
		}
	}

	public double Update( double newInput )
	{
		double newOutput = a1 * newInput + a2 * inputHistory[ 0 ] + a3 * inputHistory[ 1 ] - b1 * outputHistory[ 0 ] - b2 * outputHistory[ 1 ];

		inputHistory[ 1 ] = inputHistory[ 0 ];
		inputHistory[ 0 ] = newInput;

		outputHistory[ 2 ] = outputHistory[ 1 ];
		outputHistory[ 1 ] = outputHistory[ 0 ];
		outputHistory[ 0 ] = newOutput;
		
		return outputHistory[ 0 ];
	}
}
