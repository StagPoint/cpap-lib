using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.Importers;

public static class OximetryEventGenerator
{
	public static List<ReportedEvent> GenerateEvents( Signal oxygen, Signal pulse )
	{
		var events = new List<ReportedEvent>();

		GenerateHypoxemiaEvents( oxygen, events );
		GenerateDesaturationEvents( oxygen, events );
		GeneratePulseRateEvents( pulse, events );
		GeneratePulseChangeEvents( pulse, events );

		return events;
	}

	private static void GeneratePulseChangeEvents( Signal signal, List<ReportedEvent> events )
	{
		const int    WINDOW_LENGTH = 60;
		const double THRESHOLD     = 4.5;

		int    windowSize   = (int)Math.Ceiling( WINDOW_LENGTH * signal.FrequencyInHz );
		double timeInterval = 1.0 / signal.FrequencyInHz;

		var    data                  = signal.Samples;
		double lastStandardDeviation = 0;

		var calculator        = new MovingAverageCalculator( windowSize );
		var standardDeviation = new double[ signal.Samples.Count ];
		var average           = new double[ signal.Samples.Count ];

		for( int i = 0; i < data.Count; i++ )
		{
			calculator.AddObservation( data[ i ] );
			
			average[ i ]           = calculator.Average;
			standardDeviation[ i ] = calculator.StandardDeviation;
		}

		for( int i = windowSize; i < data.Count; i++ )
		{
			if( Math.Abs( standardDeviation[ i ] - lastStandardDeviation ) >= THRESHOLD )
			{
				var annotation = new ReportedEvent
				{
					Type      = EventType.PulseRateChange,
					StartTime = signal.StartTime.AddSeconds( i * timeInterval - timeInterval ),
					Duration  = TimeSpan.Zero
				};

				if( !events.Any( x => x.Type is EventType.Tachycardia or EventType.Bradycardia && ReportedEvent.TimesOverlap( x, annotation ) ) )
				{
					events.Add( annotation );
				}

				lastStandardDeviation = standardDeviation[ i ];
			}
		}
	}

	private static void GeneratePulseRateEvents( Signal signal, List<ReportedEvent> events )
	{
		const double TACHYCARDIA_THRESHOLD = 100;
		const double BRADYCARDIA_THRESHOLD = 50;

		if( signal.Samples.Count == 0 )
		{
			return;
		}

		int    state        = 0;
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var sourceData = signal.Samples;

		for( int i = 1; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			switch( state )
			{
				case 0:
					switch( sample )
					{
						case <= BRADYCARDIA_THRESHOLD:
						{
							// Find the specific time when the sample crossed the threshold, even if it 
							// doesn't align directly on a sample's interval
							var lastSample = sourceData[ i - 1 ];
							var t          = MathUtil.InverseLerp( lastSample, sample, BRADYCARDIA_THRESHOLD );
							eventStart = time - (1.0 - t) * timeInterval;

							state         = 1;
							eventDuration = 0;
							break;
						}
						case >= TACHYCARDIA_THRESHOLD:
						{
							// Find the specific time when the sample crossed the threshold, even if it 
							// doesn't align directly on a sample's interval
							var lastSample = sourceData[ i - 1 ];
							var t          = MathUtil.InverseLerp( lastSample, sample, TACHYCARDIA_THRESHOLD );
							eventStart = time - (1.0 - t) * timeInterval;

							state         = 2;
							eventDuration = 0;
							break;
						}
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( sample > BRADYCARDIA_THRESHOLD )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, BRADYCARDIA_THRESHOLD );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration > 0 )
						{
							var annotation = new ReportedEvent
							{
								Type      = EventType.Bradycardia,
								StartTime = signal.StartTime.AddSeconds( eventStart ),
								Duration  = TimeSpan.FromSeconds( eventDuration )
							};

							events.Add( annotation );
						}

						state = 0;
					}
					break;

				case 2:
					eventDuration = (time - eventStart);
					if( sample < TACHYCARDIA_THRESHOLD )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, TACHYCARDIA_THRESHOLD );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration > 0 )
						{
							var annotation = new ReportedEvent
							{
								Type      = EventType.Tachycardia,
								StartTime = signal.StartTime.AddSeconds( eventStart ),
								Duration  = TimeSpan.FromSeconds( eventDuration )
							};

							events.Add( annotation );
						}

						state = 0;
					}
					break;
			}
		}

		if( state != 0 && eventDuration > 0 )
		{
			var annotation = new ReportedEvent
			{
				Type      = (state == 1) ? EventType.Bradycardia : EventType.Tachycardia,
				StartTime = signal.StartTime.AddSeconds( eventStart ),
				Duration  = TimeSpan.FromSeconds( eventDuration )
			};

			events.Add( annotation );
		}
	}

	private static void GenerateDesaturationEvents( Signal signal, List<ReportedEvent> events )
	{
		// TODO: Make window size configurable 
		const int    WINDOW_SIZE            = 300;
		const double MAX_EVENT_DURATION     = 120;
		const double MIN_EVENT_DURATION     = 1;
		const double DESATURATION_THRESHOLD = 3;

		if( signal.Samples.Count < WINDOW_SIZE * signal.FrequencyInHz )
		{
			return;
		}

		int    state        = 0;
		int    windowSize   = (int)Math.Ceiling( WINDOW_SIZE * signal.FrequencyInHz );
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var    sourceData  = signal.Samples;
		double baseLine    = sourceData[ 0 ];
		double baselineSum = 0.0;

		for( int i = 0; i < sourceData.Count; i++ )
		{
			var sample    = sourceData[ i ];
			var time      = (i * timeInterval);
			var threshold = baseLine - DESATURATION_THRESHOLD;

			// Update the sliding window average used as a baseline 
			{
				if( i >= windowSize )
				{
					baselineSum -= sourceData[ i - windowSize ];
				}

				baselineSum += sample;

				var count = Math.Min( i + 1, windowSize );
				baseLine = baselineSum / count;
			}

			switch( state )
			{
				case 0:
					if( sample <= threshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, threshold );
						eventStart = time - (1.0 - t) * timeInterval;

						state         = 1;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( eventDuration >= MAX_EVENT_DURATION || sample > threshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, threshold );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= MIN_EVENT_DURATION )
						{
							var annotation = new ReportedEvent
							{
								Type      = EventType.Desaturation,
								StartTime = signal.StartTime.AddSeconds( eventStart ),
								Duration  = TimeSpan.FromSeconds( eventDuration )
							};

							if( !events.Any( x => x.Type == EventType.Hypoxemia && ReportedEvent.TimesOverlap( x, annotation ) ) )
							{
								events.Add( annotation );
							}
						}

						state = 0;
					}
					break;
			}
		}

		if( state == 1 && eventDuration >= MIN_EVENT_DURATION && eventDuration <= MAX_EVENT_DURATION )
		{
			var annotation = new ReportedEvent
			{
				Type      = EventType.Desaturation,
				StartTime = signal.StartTime.AddSeconds( eventStart ),
				Duration  = TimeSpan.FromSeconds( eventDuration )
			};

			if( !events.Any( x => ReportedEvent.TimesOverlap( x, annotation ) ) )
			{
				events.Add( annotation );
			}
		}
	}

	private static void GenerateHypoxemiaEvents( Signal signal, List<ReportedEvent> events )
	{
		const double HYPOXEMIA_THRESHOLD = 88;
		const double MIN_EVENT_DURATION  = 1;

		if( signal.Samples.Count == 0 )
		{
			return;
		}

		int    state        = 0;
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var sourceData = signal.Samples;

		for( int i = 1; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			switch( state )
			{
				case 0:
					if( sample <= HYPOXEMIA_THRESHOLD )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, HYPOXEMIA_THRESHOLD );
						eventStart = time - (1.0 - t) * timeInterval;

						state         = 1;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( sample > HYPOXEMIA_THRESHOLD )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, HYPOXEMIA_THRESHOLD );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= MIN_EVENT_DURATION )
						{
							var annotation = new ReportedEvent
							{
								Type      = EventType.Hypoxemia,
								StartTime = signal.StartTime.AddSeconds( eventStart ),
								Duration  = TimeSpan.FromSeconds( eventDuration )
							};

							events.Add( annotation );
						}

						state = 0;
					}
					break;
			}
		}

		if( state == 1 && eventDuration >= MIN_EVENT_DURATION )
		{
			var annotation = new ReportedEvent
			{
				Type      = EventType.Hypoxemia,
				StartTime = signal.StartTime.AddSeconds( eventStart ),
				Duration  = TimeSpan.FromSeconds( eventDuration )
			};

			events.Add( annotation );
		}
	}
}
