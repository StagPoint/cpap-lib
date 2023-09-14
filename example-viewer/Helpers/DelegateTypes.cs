using System;

namespace example_viewer;

public delegate void TimeSelectedEventHandler(object sender, DateTime time);

public delegate void TimeRangeSelectedEventHandler(object sender, DateTime startTime, DateTime endTime );
