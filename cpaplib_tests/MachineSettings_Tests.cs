using System.Diagnostics;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class MachineSettings_Tests
{
	[TestMethod]
	public void ReadMachineSettings()
	{
		string filename = Path.Combine( Environment.CurrentDirectory, "Files", "STR.edf" );
		Assert.IsTrue( File.Exists( filename ), "Test file does not exist" );

		var file = EdfFile.Open( filename );

		var days = new List<DailyReport>();

		// Copy all raw and single-value settings 
		for( int i = 0; i < file.Signals[ 0 ].Samples.Count; i++ )
		{
			// Gather a hash table of settings for a single day from across the signals 
			var lookup = new Dictionary<string, double>();
			for( int j = 0; j < file.Signals.Count; j++ )
			{
				lookup[ file.Signals[ j ].Label ] = file.Signals[ j ].Samples[ i ];
			}

			// Read in and process the settings for a single day
			var settings = DailyReport.Read( lookup );

			days.Add( settings );
		}

		// Add all intra-day sessions ("Mask On" to "Mask Off" periods)
		{
			// Mask On and Mask Off times are stored as the number of seconds since the day started.
			// Remember that according to ResMed, the day starts at 12pm (noon) instead of the more conventional 
			// and sane 12am (midnight).
			// There will be a maximum of ten MaskOn/MaskOff events per day (always true?)
			var maskOnSignal  = GetSignalByName( "MaskOn", "Mask On" );
			var maskOffSignal = GetSignalByName( "MaskOff", "Mask Off" );

			// There will be an even number of MaskOn/MaskOff times for each day
			var numberOfEntriesPerDay = maskOnSignal.Samples.Count / days.Count;
			Assert.IsTrue( maskOnSignal.Samples.Count % numberOfEntriesPerDay == 0, "Invalid calculation of Number of Sessions Per Day" );

			for( int dayIndex = 0; dayIndex < days.Count; dayIndex++ )
			{
				var day = days[ dayIndex ];

				if( day.Duration.TotalMinutes < 5 )
				{
					continue;
				}

				for( int i = 0; i < day.MaskEvents; i++ )
				{
					var sampleIndex = dayIndex * numberOfEntriesPerDay + i;

					// Stop processing MaskOn/MaskOff when we encounter a -1
					if( maskOnSignal.Samples[ sampleIndex ] < 0 )
					{
						break;
					}

					// Mask times are stored as the number of seconds since the "day" started. Remember that
					// the ResMed "day" starts at 12pm (noon).
					var maskOn  = day.Date.AddMinutes( maskOnSignal.Samples[ sampleIndex ] );
					var maskOff = day.Date.AddMinutes( maskOffSignal.Samples[ sampleIndex ] );

					day.MaskOn.Add( maskOn );
					day.MaskOff.Add( maskOff );
				}
			}
		}

		Debug.WriteLine( $"Number of days: {days.Count}" );

		foreach( var day in days )
		{
			if( day.Duration.TotalMinutes < 5 )
			{
				continue;
			}

			Debug.WriteLine( $"{day.Date.ToLongDateString()}   Events: {day.MaskEvents}, Duration: {day.Duration}, Mode: {day.Mode}" );

			// for( int i = 0; i < day.MaskOn.Count; i++ )
			// {
			// 	Debug.WriteLine( $"    {day.MaskOn[i].ToShortDateString()}    {day.MaskOn[i].ToShortTimeString()} - {day.MaskOff[i].ToShortTimeString()}" );
			// }
			
			Debug.WriteLine( $@"
	Mode					{day.Mode}
	Mask					{day.Settings.Mask}
	Antibacterial Filter	{(day.Settings.AntibacterialFilter ? "Yes" : "No")}

	Essentials				{day.Settings.Essentials}
	Response				{day.Settings.AutoSet.ResponseType}
	Smart Start				{day.Settings.SmartStart}

	Pressure				{day.Settings.CPAP.Pressure} cmH2O
	Pressure Min			{day.Settings.AutoSet.MinPressure} cmH2O
	Pressure Max			{day.Settings.AutoSet.MaxPressure} cmH2O

	EPR						{day.Settings.EPR.Mode}
	EPR Level				{day.Settings.EPR.Level} cmH2O

	Ramp					{day.Settings.RampMode}
	Ramp Pressure			{day.Settings.CPAP.StartPressure} cmH2O
	Ramp time				{day.Settings.RampTime} Minutes

	Climate Control			{day.Settings.ClimateControl}
	Humidity Enabled		{day.Settings.HumidityEnabled}
	Humidifier Status		{(day.Humidifier ? "On" : "Off")}
	Humidity Level			{day.Settings.HumidityLevel} 
	Temperature				{day.Settings.Temp} ºC
	Temperature				{day.Settings.Temp * 9.0/5.0 + 32.0:F0} ºF
	Temperature Enable		{day.Settings.TemperatureEnabled}
");
		}

		EdfStandardSignal GetSignalByName( params string[] labels )
		{
			// This isn't possible under normal usage, but...
			if( labels == null || labels.Length == 0 )
			{
				throw new ArgumentException( nameof( labels ) );
			}
			
			foreach( var label in labels )
			{
				var signal = file.GetSignalByName<EdfStandardSignal>( label );
				
				if( signal != null )
				{
					return signal;
				}
			}

			throw new KeyNotFoundException( $"Failed to find a signal named '{labels[0]}" );
		}
	}
}
