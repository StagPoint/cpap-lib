using System;

namespace cpap_app.Helpers;

public class DateRange
{
	public DateTime Start { get; set; }
	public DateTime End   { get; set; }

	public bool Overlaps( DateRange other )
	{
		return DateHelper.RangesOverlap( Start, End, other.Start, other.End );
	}
}
