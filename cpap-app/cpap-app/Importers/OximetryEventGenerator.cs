using System;
using System.Collections.Generic;
using System.Linq;

using cpap_app.Helpers;

using cpaplib;

using MathUtil = cpap_app.Helpers.MathUtil;

namespace cpap_app.Importers;

public class OximetryEventGeneratorConfig
{
	public double EventScanDelay { get; set; } = 300;

	public double HypoxemiaThreshold       { get; set; } = 88;
	public double HypoxemiaMinimumDuration { get; set; } = 8;

	public double DesaturationThreshold       { get; set; } = 3;
	public double DesaturationWindowLength    { get; set; } = 600;
	public double DesaturationMinimumDuration { get; set; } = 5;
	public double DesaturationMaximumDuration { get; set; } = 120;

	public double TachycardiaThreshold     { get; set; } = 100;
	public double BradycardiaThreshold     { get; set; } = 50;
	public double PulseRateMinimumDuration { get; set; } = 10;

	public double PulseChangeThreshold    { get; set; } = 10;
	public double PulseChangeWindowLength { get; set; } = 120;
}

public static class OximetryEventGenerator
{
	public static List<ReportedEvent> GenerateEvents( OximetryEventGeneratorConfig config, Signal oxygen, Signal pulse )
	{
		var events = new List<ReportedEvent>();

		GenerateHypoxemiaEvents( config, oxygen, events );
		GenerateDesaturationEvents( config, oxygen, events );
		GeneratePulseRateEvents( config, pulse, events );
		GeneratePulseChangeEvents( config, pulse, events );
		AssignEventSourceType( events );

		return events;
	}

	private static void AssignEventSourceType( List<ReportedEvent> events )
	{
		foreach( var evt in events )
		{
			evt.SourceType = SourceType.PulseOximetry;
		}
	}

	private static void GeneratePulseChangeEvents( OximetryEventGeneratorConfig config, Signal signal, List<ReportedEvent> events )
	{
		const double THRESHOLD     = 0.1;
		const double PERSISTENCE   = 0.1;

		int    windowSize   = (int)Math.Ceiling( config.EventScanDelay * signal.FrequencyInHz );
		double timeInterval = 1.0 / signal.FrequencyInHz;

		var data   = signal.Samples;
		
		// Looks for variations greater than one standard deviation from the running 30 second average,
		// with a minimum difference of 10 beats per minute. 
		var peakSignals = PeakFinder.GenerateSignals( data, windowSize, THRESHOLD, PERSISTENCE, config.PulseChangeThreshold );

		var startIndex = (int)(config.EventScanDelay / timeInterval);
		for( int i = startIndex; i < data.Count; i++ )
		{
			var peakSignal = peakSignals[ i ];
			if( peakSignal == 0 )
			{
				continue;
			}
			
			var annotation = new ReportedEvent
			{
				Type      = EventType.PulseRateChange,
				StartTime = signal.StartTime.AddSeconds( i * timeInterval ),
				Duration  = TimeSpan.Zero
			};

			// Don't add any PulseRateChange events when there is already a Tachycardia or Bradycardia event at 
			// that time period. You could add it, but then the UI becomes a mess for the user to understand and 
			// the other event types are probably more important. 
			if( !events.Any( x => x.Type is EventType.Tachycardia or EventType.Bradycardia && ReportedEvent.TimesOverlap( x, annotation ) ) )
			{
				events.Add( annotation );
			}

			// The peak finder (may) generates a signal per sample for the entire duration of the peak. We only 
			// need to know when the peak started, so skip ahead a bit. 
			int eventStart = i;
			while( i < data.Count - 1 && peakSignals[ i + 1 ] == peakSignal && i - eventStart + 1 < windowSize )
			{
				i++;
			}
		}
	}

	private static void GeneratePulseRateEvents( OximetryEventGeneratorConfig config, Signal signal, List<ReportedEvent> events )
	{
		if( signal.Samples.Count == 0 )
		{
			return;
		}

		int    state        = 0;
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var sourceData = signal.Samples;

		var startIndex = (int)(config.EventScanDelay / timeInterval);
		for( int i = startIndex; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			switch( state )
			{
				case 0:
					if( sample <= config.BradycardiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.BradycardiaThreshold );
						eventStart = time - (1.0 - t) * timeInterval;

						state         = 1;
						eventDuration = 0;
					}
					else if( sample >= config.TachycardiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.TachycardiaThreshold );

						eventStart    = time - (1.0 - t) * timeInterval;
						state         = 2;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( sample > config.BradycardiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.BradycardiaThreshold );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= config.PulseRateMinimumDuration )
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
					if( sample < config.TachycardiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.TachycardiaThreshold );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= config.PulseRateMinimumDuration )
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

		if( state != 0 && eventDuration >= config.PulseRateMinimumDuration )
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

	private static void GenerateDesaturationEvents( OximetryEventGeneratorConfig config, Signal signal, List<ReportedEvent> events )
	{
		int    state        = 0;
		int    windowSize   = (int)Math.Ceiling( config.DesaturationWindowLength * signal.FrequencyInHz );
		double timeInterval = 1.0 / signal.FrequencyInHz;

		if( signal.Samples.Count <= windowSize )
		{
			return;
		}

		double eventStart    = -1;

		var sourceData = signal.Samples;
		var calc       = new MovingAverageCalculator( windowSize );

		for( int i = 0; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			calc.AddObservation( sample );

			if( !calc.HasFullPeriod )
			{
				continue;
			}
			
			var time      = (i * timeInterval);
			var baseline  = calc.Average;
			var threshold = baseline - config.DesaturationThreshold;

			if( time < config.EventScanDelay )
			{
				continue;
			}

			var eventDuration = 0.0;
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
					if( eventDuration >= config.DesaturationMaximumDuration || sample > threshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, threshold );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= config.DesaturationMinimumDuration )
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
	}

	private static void GenerateHypoxemiaEvents( OximetryEventGeneratorConfig config, Signal signal, List<ReportedEvent> events )
	{
		if( signal.Samples.Count == 0 )
		{
			return;
		}

		int    state        = 0;
		double timeInterval = 1.0 / signal.FrequencyInHz;

		double eventStart    = -1;
		double eventDuration = 0.0;

		var sourceData = signal.Samples;

		var startIndex = (int)(config.EventScanDelay / timeInterval);
		for( int i = startIndex; i < sourceData.Count; i++ )
		{
			var sample = sourceData[ i ];
			var time   = (i * timeInterval);

			switch( state )
			{
				case 0:
					if( sample <= config.HypoxemiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.HypoxemiaThreshold );
						eventStart = time - (1.0 - t) * timeInterval;

						state         = 1;
						eventDuration = 0;
					}
					break;

				case 1:
					eventDuration = (time - eventStart);
					if( sample > config.HypoxemiaThreshold )
					{
						// Find the specific time when the sample crossed the threshold, even if it 
						// doesn't align directly on a sample's interval
						var lastSample = sourceData[ i - 1 ];
						var t          = MathUtil.InverseLerp( lastSample, sample, config.HypoxemiaThreshold );
						var eventEnd   = time - (1.0 - t) * timeInterval;

						eventDuration = eventEnd - eventStart;

						if( eventDuration >= config.HypoxemiaMinimumDuration )
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

		if( state == 1 && eventDuration >= config.HypoxemiaMinimumDuration )
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
