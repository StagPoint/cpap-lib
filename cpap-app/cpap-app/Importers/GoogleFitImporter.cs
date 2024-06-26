﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using cpap_app.Helpers;

using cpaplib;

using Google.Fitness.Data;

using Newtonsoft.Json;

namespace cpap_app.Importers;

public class GoogleFitImporter
{
	#region Private fields

    private static readonly Dictionary<GoogleFitSleepStage, int> _sleepStageMap = new Dictionary<GoogleFitSleepStage, int>()
    {
        { GoogleFitSleepStage.Awake,	1 },
        { GoogleFitSleepStage.REM,		2 },
        { GoogleFitSleepStage.Asleep,	3 },
        { GoogleFitSleepStage.OutOfBed, 3 },
        { GoogleFitSleepStage.Light,	3 },
        { GoogleFitSleepStage.Deep,		4 },
    };

	#endregion

	#region Public functions

    public static async Task<List<MetaSession>?> ImportAsync( DateTime startDate, DateTime endDate, string accessToken, Action<string> progressNotify )
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

        var sessions = await ImportSessions( accessToken, fitSessions, progressNotify );
        if( sessions.Count == 0 )
        {
            return null;
        }

        List<MetaSession> metaSessions       = new();
        MetaSession?      currentMetaSession = null;

        foreach( var session in sessions )
        {
            if( currentMetaSession == null || !currentMetaSession.TryAdd( session ) )
            {
                currentMetaSession = new MetaSession();
                currentMetaSession.Add( session );

                metaSessions.Add( currentMetaSession );
            }
        }

        return metaSessions;
    }

	#endregion

	#region Private functions

    private static async Task<List<Session>> ImportSessions( string accessToken, List<ActivitySession> fitSessions, Action<string> progressNotify )
    {
        List<Session> sessions = new();

        foreach( var fitSession in fitSessions )
        {
            var startTime = DateHelper.FromMillisecondsSinceEpoch( fitSession.StartTimeMillis ).AdjustForDaylightSavings();
            var endTime   = DateHelper.FromMillisecondsSinceEpoch( fitSession.EndTimeMillis ).AdjustForDaylightSavings();

            progressNotify( $"Retrieving session starting on {startTime:d} at {startTime:h:mm:ss tt}" );

            var bucket = await GetSleepSessionDetailsAsync( accessToken, fitSession.StartTimeMillis, fitSession.EndTimeMillis );
            if( bucket == null || bucket.Datasets.Count == 0 )
            {
                continue;
            }

            var sessionSource = fitSession.Application.Name;
            if( string.IsNullOrEmpty( sessionSource ) )
            {
                sessionSource = fitSession.Application.PackageName;
                if( string.IsNullOrEmpty( sessionSource ) )
                {
                    sessionSource = "Google Fit API";
                }
            }

            var session = ImportSessionFromDataset( bucket.Datasets[ 0 ].Points, startTime, endTime );
            session.Source = sessionSource;
            sessions.Add( session );
        }

        return sessions;
    }

    private static Session ImportSessionFromDataset( List<DataPoint> Points, DateTime startTime, DateTime endTime )
    {
        var signal = new Signal
        {
            Name              = SignalNames.SleepStages,
            FrequencyInHz     = 1.0 / 30,
            MinValue          = 0,
            MaxValue          = 5,
            UnitOfMeasurement = string.Empty,
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

        var timeStep    = TimeSpan.FromSeconds( 30 );
        var currentTime = startTime;
        var samples     = signal.Samples;

        foreach( var point in Points )
        {
            Debug.Assert( point.Value.Count > 0 );
            Debug.Assert( point.Value[ 0 ].IntVal != null );

            var value = (GoogleFitSleepStage)point.Value[ 0 ].IntVal!;
            if( !_sleepStageMap.TryGetValue( value, out int outputValue ) )
            {
                outputValue = 1;
            }

            var sampleCount = (int)Math.Max( 1, TimeSpan.FromMilliseconds( (point.EndTimeNanos - point.StartTimeNanos) * 1E-6 ).TotalSeconds / 30 );
            for( int j = 0; j < sampleCount; j++ )
            {
                samples.Add( outputValue );
                currentTime += timeStep;
            }
        }

        session.EndTime = signal.EndTime = currentTime;

        return session;
    }

    private static async Task<List<ActivitySession>?> ListSessionsAsync( string accessToken, DateTime startTime, DateTime endTime, int activityType = 72 )
    {
        Debug.Assert( startTime.Kind != DateTimeKind.Unspecified, $"{nameof( startTime )} is missing the {nameof( DateTime.Kind )} value" );
        Debug.Assert( endTime.Kind != DateTimeKind.Unspecified,   $"{nameof( endTime )} is missing the {nameof( DateTime.Kind )} value" );

        string requestUri = $"https://www.googleapis.com/fitness/v1/users/me/sessions?activityType={activityType}&startTime={startTime:O}&endTime={endTime:O}";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create( requestUri );
        request.Method = "GET";
        request.Headers.Add( $"Authorization: Bearer {accessToken}" );
        request.ContentType = "application/x-www-form-urlencoded";
        request.Accept      = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

        try
        {
            var       response       = await request.GetResponseAsync();
            using var responseReader = new StreamReader( response.GetResponseStream() ?? throw new InvalidOperationException() );
            var       json           = await responseReader.ReadToEndAsync();

            var sessions = JsonConvert.DeserializeObject<ListSessionsResponse>( json );

            return sessions?.Session;
        }
        catch( Exception err )
        {
            Debug.WriteLine( err.ToString() );
            throw;
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