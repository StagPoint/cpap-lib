using System.Diagnostics;
using System.Globalization;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class MachineSettings_Tests
{
	[TestMethod]
	public void ReadMachineSettings()
	{
		string rootFolder = Path.Combine( Environment.CurrentDirectory, "Files" );
		Assert.IsTrue( Directory.Exists( rootFolder ), "Test file does not exist" );

		var loader = new CpapDataLoader();
		loader.LoadFromFolder( rootFolder, DateTime.Today.AddDays( -30 ) );

		var days = loader.Days;

		Debug.WriteLine( $"Number of days: {days.Count}" );

		foreach( var day in days )
		{
			Debug.WriteLine( $"{day.ReportDate.ToLongDateString()}   Events: {day.MaskEvents}, Duration: {day.Duration}, Mode: {day.Settings.Mode}" );

			// for( int i = 0; i < day.MaskOn.Count; i++ )
			// {
			// 	Debug.WriteLine( $"    {day.MaskOn[i].ToShortDateString()}    {day.MaskOn[i].ToShortTimeString()} - {day.MaskOff[i].ToShortTimeString()}" );
			// }

			Debug.WriteLine( $@"
	Mode					{day.Settings.Mode}
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
	Humidifier Status		{(day.Settings.Humidifier ? "On" : "Off")}
	Humidity Level			{day.Settings.HumidityLevel} 
	Temperature				{day.Settings.Temperature} ºC
	Temperature				{day.Settings.Temperature * 9.0 / 5.0 + 32.0:F0} ºF
	Temperature Enable		{day.Settings.TemperatureEnabled}
" );
		}
	}
}
