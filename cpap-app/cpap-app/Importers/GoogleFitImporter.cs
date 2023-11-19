using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using cpap_app.Helpers;

using cpaplib;

using OAuth;
using Google.Fitness.Data;
using Newtonsoft.Json;

namespace cpap_app.Importers;

public class GoogleFitImporter
{
	#region Private fields

	private static readonly Dictionary<SleepStageValue, int> _sleepStageMap = new Dictionary<SleepStageValue, int>()
	{
		{ SleepStageValue.Awake,	1 },
		{ SleepStageValue.REM,		2 },
		{ SleepStageValue.Asleep,	3 },
		{ SleepStageValue.OutOfBed, 3 },
		{ SleepStageValue.Light,	3 },
		{ SleepStageValue.Deep,		4 },
	};

	#endregion 
	
	#region Public functions

	public static async Task<List<MetaSession>?> ImportAsync( DateTime startDate, DateTime endDate, string accessToken )
	{
		if( string.IsNullOrEmpty( accessToken ) )
		{
			throw new InvalidOperationException( "Invalid access token" );
		}

		var fitSessions = await ListSessionsAsync( accessToken, startDate, endDate );
		if( fitSessions == null || fitSessions.Count == 0 )
		{
			return null;
		}

		var sessions = await ImportSessions( accessToken, fitSessions );
		if( sessions.Count == 0 )
		{
			return null;
		}

		return null;
	}

	#endregion 
	
	#region Private functions 
	
	private static async Task<List<Session>> ImportSessions( string accessToken, List<ActivitySession> fitSessions )
	{
		List<Session> sessions = new();

		foreach( var fitSession in fitSessions )
		{
			var bucket = await GetSleepSessionDetailsAsync( accessToken, fitSession.StartTimeMillis, fitSession.EndTimeMillis );
			if( bucket == null || bucket.Datasets.Count == 0 )
			{
				continue;
			}

			var startTime = DateHelper.FromMillisecondsSinceEpoch( fitSession.StartTimeMillis );
			var endTime   = DateHelper.FromMillisecondsSinceEpoch( fitSession.EndTimeMillis );

			var session = ImportSessionFromDataset( bucket.Datasets[ 0 ].Points, startTime, endTime );
			sessions.Add( session );
		}

		return sessions;
	}
	
	private static Session ImportSessionFromDataset( List<DataPoint> Points, DateTime startTime, DateTime endTime )
	{
		var signal = new Signal
		{
			Name              = SignalNames.SleepStages,
			FrequencyInHz     = 0.5,
			MinValue          = 0,
			MaxValue          = 5,
			UnitOfMeasurement = null,
			StartTime         = startTime,
			EndTime           = endTime
		};

		var session = new Session
		{
			StartTime  = startTime,
			EndTime    = endTime,
			Source     = "Google Fit",
			SourceType = SourceType.HealthAPI,
			Signals    = { signal },
		};

		int numberOfSamples = (int)Math.Ceiling( (endTime - startTime).TotalSeconds ) / 30;
		int pointIndex      = 0;
		var timeStep        = TimeSpan.FromSeconds( 60 * signal.FrequencyInHz );
		var currentTime     = startTime;
		var samples         = signal.Samples;

		for( int i = 0; i < numberOfSamples; i++ )
		{
			var point  = Points[ pointIndex ];
			Debug.Assert( point.Value.Count > 0 );
			Debug.Assert( point.Value[ 0 ].IntVal != null );
			
			var value = (SleepStageValue)point.Value[ 0 ].IntVal!;
			if( !_sleepStageMap.TryGetValue( value, out int outputValue ) )
			{
				outputValue = 1;
			}

			samples.Add( outputValue );

			currentTime += timeStep;
			var pointEndTime = DateHelper.FromNanosecondsSinceEpoch( point.EndTimeNanos );

			if( currentTime >= pointEndTime )
			{
				pointIndex += 1;
			}
		}

		return session;
	}

	private static async Task<List<ActivitySession>?> ListSessionsAsync( string accessToken, DateTime startTime, DateTime endTime, int activityType = 72 )
	{
		string requestUri = $"https://www.googleapis.com/fitness/v1/users/me/sessions?activityType={activityType}&startTime={startTime:O}&endTime={endTime:O}";

		HttpWebRequest request = (HttpWebRequest)WebRequest.Create( requestUri );
		request.Method = "GET";
		request.Headers.Add( $"Authorization: Bearer {accessToken}" );
		request.ContentType = "application/x-www-form-urlencoded";
		request.Accept      = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

		WebResponse response = await request.GetResponseAsync();
		using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ?? throw new InvalidOperationException() ) )
		{
			var json = await responseReader.ReadToEndAsync();
			
			var sessions = JsonConvert.DeserializeObject<ListSessionsResponse>( json );

			return sessions?.Session;
		}
	}
	
	private static async Task<AggregateBucket?> GetSleepSessionDetailsAsync( string accessToken, long startTimeMilliseconds, long endTimeMilliseconds )
	{
		string requestUri = "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate";

		HttpWebRequest request = (HttpWebRequest)WebRequest.Create( requestUri );
		request.Method = "POST";
		request.Headers.Add( $"Authorization: Bearer {accessToken}" );
		request.ContentType = "application/json";

		string requestBody = $@"
{{
  ""aggregateBy"": [
    {{
      ""dataTypeName"": ""com.google.sleep.segment""
    }}
  ],
  ""endTimeMillis"": {endTimeMilliseconds},
  ""startTimeMillis"": {startTimeMilliseconds}
}}";

		byte[] requestBodyBytes = Encoding.ASCII.GetBytes( requestBody );
		request.ContentLength = requestBodyBytes.Length;

		using( Stream requestStream = request.GetRequestStream() )
		{
			await requestStream.WriteAsync( requestBodyBytes, 0, requestBodyBytes.Length );
		}

		WebResponse response = await request.GetResponseAsync();
		using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ?? throw new InvalidOperationException() ) )
		{
			string json = await responseReader.ReadToEndAsync();

			var result = JsonConvert.DeserializeObject<AggregateResponse>( json );

			return result?.Buckets[ 0 ];
		}
	}
	
	#endregion 
}
