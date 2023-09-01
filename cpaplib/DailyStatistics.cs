using System.Collections.Generic;

namespace cpaplib
{
	public class DailyStatistics
	{
		#region Public properties

		public double BlowPress_95   { get; private set; }
		public double BlowPress_5    { get; private set; }
		public double Flow_95        { get; private set; }
		public double Flow_5         { get; private set; }
		public double BlowFlow_50    { get; private set; }
		public double AmbHumidity_50 { get; private set; }
		public double HumTemp_50     { get; private set; }
		public double HTubeTemp_50   { get; private set; }
		public double HTubePow_50    { get; private set; }
		public double HumPow_50      { get; private set; }
		public double SpO2_50        { get; private set; }
		public double SpO2_95        { get; private set; }
		public double SpO2_Max       { get; private set; }
		public double SpO2Thresh     { get; private set; }
		public double MaskPress_50   { get; private set; }
		public double MaskPress_95   { get; private set; }
		public double MaskPress_Max  { get; private set; }
		public double TgtIPAP_50     { get; private set; }
		public double TgtIPAP_95     { get; private set; }
		public double TgtIPAP_Max    { get; private set; }
		public double TgtEPAP_50     { get; private set; }
		public double TgtEPAP_95     { get; private set; }
		public double TgtEPAP_Max    { get; private set; }
		public double Leak_50        { get; private set; }
		public double Leak_95        { get; private set; }
		public double Leak_70        { get; private set; }
		public double Leak_Max       { get; private set; }
		public double MinVent_50     { get; private set; }
		public double MinVent_95     { get; private set; }
		public double MinVent_Max    { get; private set; }
		public double RespRate_50    { get; private set; }
		public double RespRate_95    { get; private set; }
		public double RespRate_Max   { get; private set; }
		public double TidVol_50      { get; private set; }
		public double TidVol_95      { get; private set; }
		public double TidVol_Max     { get; private set; }

		#endregion

		#region Public functions

		internal void ReadFrom( Dictionary<string, double> map )
		{
			BlowPress_95   = getValue( "BlowPress.95" );
			BlowPress_5    = getValue( "BlowPress.5" );
			Flow_95        = getValue( "Flow.95" );
			Flow_5         = getValue( "Flow.5" );
			BlowFlow_50    = getValue( "BlowFlow.50" );
			AmbHumidity_50 = getValue( "AmbHumidity.50" );
			HumTemp_50     = getValue( "HumTemp.50" );
			HTubeTemp_50   = getValue( "HTubeTemp.50" );
			HTubePow_50    = getValue( "HTubePow.50" );
			HumPow_50      = getValue( "HumPow.50" );
			SpO2_50        = getValue( "SpO2.50" );
			SpO2_95        = getValue( "SpO2.95" );
			SpO2_Max       = getValue( "SpO2.Max" );
			SpO2Thresh     = getValue( "SpO2Thresh" );
			MaskPress_50   = getValue( "MaskPress.50" );
			MaskPress_95   = getValue( "MaskPress.95" );
			MaskPress_Max  = getValue( "MaskPress.Max" );
			TgtIPAP_50     = getValue( "TgtIPAP.50" );
			TgtIPAP_95     = getValue( "TgtIPAP.95" );
			TgtIPAP_Max    = getValue( "TgtIPAP.Max" );
			TgtEPAP_50     = getValue( "TgtEPAP.50" );
			TgtEPAP_95     = getValue( "TgtEPAP.95" );
			TgtEPAP_Max    = getValue( "TgtEPAP.Max" );
			Leak_50        = getValue( "Leak.50" );
			Leak_95        = getValue( "Leak.95" );
			Leak_70        = getValue( "Leak.70" );
			Leak_Max       = getValue( "Leak.Max" );
			MinVent_50     = getValue( "MinVent.50" );
			MinVent_95     = getValue( "MinVent.95" );
			MinVent_Max    = getValue( "MinVent.Max" );
			RespRate_50    = getValue( "RespRate.50" );
			RespRate_95    = getValue( "RespRate.95" );
			RespRate_Max   = getValue( "RespRate.Max" );
			TidVol_50      = getValue( "TidVol.50" );
			TidVol_95      = getValue( "TidVol.95" );
			TidVol_Max     = getValue( "TidVol.Max" );

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

		#endregion
	}
}
