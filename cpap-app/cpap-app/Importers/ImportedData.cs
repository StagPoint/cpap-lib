using System;
using System.Collections.Generic;

using cpap_app.Helpers;

using cpaplib;

namespace cpap_app.Importers;

public class ImportedData : IComparable<ImportedData>
{
	public DateTime StartTime { get; set;  }
	public DateTime EndTime   { get; set; }

	public List<Session>       Sessions { get; set; } = new List<Session>();
	public List<ReportedEvent> Events   { get; set; } = new List<ReportedEvent>();

	public void AddSession( Session session )
	{
		if( Sessions.Count == 0 )
		{
			StartTime = session.StartTime;
			EndTime   = session.EndTime;
		}
		else
		{
			StartTime = DateHelper.Min( StartTime, session.StartTime );
			EndTime   = DateHelper.Max( EndTime, session.EndTime );
		}

		Sessions.Add( session );
	}

	public int CompareTo( ImportedData? other )
	{
		return StartTime.CompareTo( other?.StartTime );
	}
	
	public override string ToString()
	{
		return $"Start: {StartTime}, End: {EndTime}, Sessions: {Sessions.Count}, Events: {Events.Count}";
	}
}

public class MetaSession : IComparable<MetaSession>
{
	public DateTime StartTime { get; set; }
	public DateTime EndTime   { get; set; }

	public List<ImportedData> Items { get; set; } = new List<ImportedData>();

	/// <summary>
	/// Used when the import process produces Session instances only. 
	/// </summary>
	public void Add( Session session )
	{
		var data = new ImportedData();
		data.AddSession( session );

		Add( data );
	}

	/// <summary>
	/// Used when the import process produces Session instances only. 
	/// </summary>
	public bool TryAdd( Session session )
	{
		var data = new ImportedData();
		data.AddSession( session );

		if( CanMerge( data ) )
		{
			Add( data );
			return true;
		}

		return false;
	}

	public void Add( ImportedData import )
	{
		if( Items.Count == 0 )
		{
			StartTime = import.StartTime;
			EndTime   = import.EndTime;
		}
		else
		{
			StartTime = DateHelper.Min( StartTime, import.StartTime );
			EndTime   = DateHelper.Max( EndTime, import.EndTime );
		}
		
		Items.Add( import );
	}

	public bool CanMergeWith( DailyReport day )
	{
		if( DateHelper.RangesOverlap( StartTime, EndTime, day.RecordingStartTime, day.RecordingEndTime ) )
		{
			return true;
		}

		if( day.RecordingStartTime >= EndTime && day.RecordingStartTime <= EndTime.AddHours( 1 ) )
		{
			return true;
		}

		return day.RecordingEndTime <= StartTime && day.RecordingEndTime >= StartTime.AddHours( -1 );
	}
	
	public bool CanMerge( ImportedData import )
	{
		if( Items.Count == 0 )
		{
			return true;
		}
		
		if( DateHelper.RangesOverlap( StartTime, EndTime, import.StartTime, import.EndTime ) )
		{
			return true;
		}

		if( import.StartTime >= EndTime && import.StartTime <= EndTime.AddHours( 1 ) )
		{
			return true;
		}

		return import.EndTime <= StartTime && import.EndTime >= StartTime.AddHours( -1 );
	}

	public int CompareTo( MetaSession? other )
	{
		return StartTime.CompareTo( other?.StartTime );
	}
	
	public override string ToString()
	{
		return $"Start: {StartTime},    End: {EndTime},    Items: {Items.Count}";
	}
}
