using System;

using cpaplib;

namespace cpapviewer;

public delegate void DailyReportModifiedHandler( object sender, DailyReport day );

public delegate void TimeSelectedEventHandler(object sender, DateTime time);

public delegate void TimeRangeSelectedEventHandler(object sender, DateTime startTime, DateTime endTime );
