using System;

using cpap_app.Configuration;

namespace cpap_app.Controls;

public interface ISignalGraph
{
	SignalChartConfiguration? ChartConfiguration { get; set; }
	
	void SetDisplayedRange( DateTime startTime, DateTime endTime );
	
	void UpdateTimeMarker( DateTime  time );

	void RenderGraph( bool highQuality );
}
