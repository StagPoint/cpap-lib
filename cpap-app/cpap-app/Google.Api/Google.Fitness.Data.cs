﻿using System.Collections.Generic;

using Newtonsoft.Json;

namespace Google.Fitness.Data;

public enum GoogleFitSleepStage
{
	INVALID  = 0,
	Awake    = 1,
	Asleep   = 2,
	OutOfBed = 3,
	Light    = 4,
	Deep     = 5,
	REM      = 6,
}

public sealed class AggregateBucket
{
	/// <summary>Available for Bucket.Type.ACTIVITY_TYPE, Bucket.Type.ACTIVITY_SEGMENT</summary>
	[JsonProperty( "activity" )]
	public int? Activity { get; set; }

	/// <summary>There will be one dataset per AggregateBy in the request.</summary>
	[JsonProperty( "dataset" )]
	public required List<Dataset> Datasets { get; set; }

	/// <summary>The end time for the aggregated data, in milliseconds since epoch, inclusive.</summary>
	[JsonProperty( "endTimeMillis", Required = Required.Always )]
	public long EndTimeMillis { get; set; }

	/// <summary>Available for Bucket.Type.SESSION</summary>
	[JsonProperty( "session" )]
	public ActivitySession? Session { get; set; }

	/// <summary>The start time for the aggregated data, in milliseconds since epoch, inclusive.</summary>
	[JsonProperty( "startTimeMillis", Required = Required.Always )]
	public long StartTimeMillis { get; set; }

	/// <summary>The type of a bucket signifies how the data aggregation is performed in the bucket.</summary>
	[JsonProperty( "type" )]
	public string Type { get; set; } = string.Empty;
}

public sealed class AggregateResponse
{
	/// <summary>A list of buckets containing the aggregated data.</summary>
	[JsonProperty( "bucket" )]
	public required List<AggregateBucket> Buckets { get; set; }
}

public sealed class Application
{
	/// <summary>An optional URI that can be used to link back to the application.</summary>
	[JsonProperty( "detailsUrl" )]
	public string DetailsUrl { get; set; } = string.Empty;

	/// <summary>
	/// The name of this application. This is required for REST clients, but we do not enforce uniqueness of this
	/// name. It is provided as a matter of convenience for other developers who would like to identify which REST
	/// created an Application or Data Source.
	/// </summary>
	[JsonProperty( "name" )]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Package name for this application. This is used as a unique identifier when created by Android applications,
	/// but cannot be specified by REST clients. REST clients will have their developer project number reflected
	/// into the Data Source data stream IDs, instead of the packageName.
	/// </summary>
	[JsonProperty( "packageName" )]
	public string PackageName { get; set; } = string.Empty;

	/// <summary>
	/// Version of the application. You should update this field whenever the application changes in a way that
	/// affects the computation of the data.
	/// </summary>
	[JsonProperty( "version" )]
	public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single data point, generated by a particular data source. A data point holds a value for each
/// field, an end timestamp and an optional start time. The exact semantics of each of these attributes are
/// specified in the documentation for the particular data type. A data point can represent an instantaneous
/// measurement, reading or input observation, as well as averages or aggregates over a time interval. Check the
/// data type documentation to determine which is the case for a particular data type. Data points always contain
/// one value for each field of the data type.
/// </summary>
public sealed class DataPoint
{
	/// <summary>The data type defining the format of the values in this data point.</summary>
	[JsonProperty( "dataTypeName" )]
	public string DataTypeName { get; set; } = string.Empty;

	/// <summary>
	/// The start time of the interval represented by this data point, in nanoseconds since epoch.
	/// </summary>
	[JsonProperty( "startTimeNanos", Required = Required.Always )]
	public long StartTimeNanos { get; set; }

	/// <summary>The end time of the interval represented by this data point, in nanoseconds since epoch.</summary>
	[JsonProperty( "endTimeNanos", Required = Required.Always )]
	public long EndTimeNanos { get; set; }

	/// <summary>
	/// Indicates the last time this data point was modified. Useful only in contexts where we are listing the data
	/// changes, rather than representing the current state of the data.
	/// </summary>
	[JsonProperty( "modifiedTimeMillis" )]
	public long? ModifiedTimeMillis { get; set; }

	/// <summary>
	/// If the data point is contained in a dataset for a derived data source, this field will be populated with the
	/// data source stream ID that created the data point originally. WARNING: do not rely on this field for
	/// anything other than debugging. The value of this field, if it is set at all, is an implementation detail and
	/// is not guaranteed to remain consistent.
	/// </summary>
	[JsonProperty( "originDataSourceId" )]
	public string? OriginDataSourceId { get; set; }

	/// <summary>The raw timestamp from the original SensorEvent.</summary>
	[JsonProperty( "rawTimestampNanos" )]
	public long? RawTimestampNanos { get; set; }

	/// <summary>
	/// Values of each data type field for the data point. It is expected that each value corresponding to a data
	/// type field will occur in the same order that the field is listed with in the data type specified in a data
	/// source. Only one of integer and floating point fields will be populated, depending on the format enum value
	/// within data source's type field.
	/// </summary>
	[JsonProperty( "value", Required = Required.Always )]
	public required List<Value> Value { get; set; }
}

/// <summary>
/// A dataset represents a projection container for data points. They do not carry any info of their own. Datasets
/// represent a set of data points from a particular data source. A data point can be found in more than one
/// dataset.
/// </summary>
public sealed class Dataset
{
	/// <summary>The data stream ID of the data source that created the points in this dataset.</summary>
	[JsonProperty( "dataSourceId" )]
	public string DataSourceId { get; set; } = string.Empty;

	/// <summary>
	/// The largest end time of all data points in this possibly partial representation of the dataset. Time is in
	/// nanoseconds from epoch. This should also match the second part of the dataset identifier.
	/// </summary>
	[JsonProperty( "maxEndTimeNs" )]
	public long? MaxEndTimeNs { get; set; }

	/// <summary>
	/// The smallest start time of all data points in this possibly partial representation of the dataset. Time is
	/// in nanoseconds from epoch. This should also match the first part of the dataset identifier.
	/// </summary>
	[JsonProperty( "minStartTimeNs" )]
	public long? MinStartTimeNs { get; set; }

	/// <summary>
	/// This token will be set when a dataset is received in response to a GET request and the dataset is too large
	/// to be included in a single response. Provide this value in a subsequent GET request to return the next page
	/// of data points within this dataset.
	/// </summary>
	[JsonProperty( "nextPageToken" )]
	public string? NextPageToken { get; set; }

	/// <summary>
	/// A partial list of data points contained in the dataset, ordered by endTimeNanos. This list is considered
	/// complete when retrieving a small dataset and partial when patching a dataset or retrieving a dataset that is
	/// too large to include in a single response.
	/// </summary>
	[JsonProperty( "point" )]
	public required List<DataPoint> Points { get; set; }
}

/// <summary>
/// Representation of an integrated device (such as a phone or a wearable) that can hold sensors. Each sensor is
/// exposed as a data source. The main purpose of the device information contained in this class is to identify the
/// hardware of a particular data source. This can be useful in different ways, including: - Distinguishing two
/// similar sensors on different devices (the step counter on two nexus 5 phones, for instance) - Display the source
/// of data to the user (by using the device make / model) - Treat data differently depending on sensor type
/// (accelerometers on a watch may give different patterns than those on a phone) - Build different analysis models
/// for each device/version.
/// </summary>
public sealed class Device
{
	/// <summary>Manufacturer of the product/hardware.</summary>
	[JsonProperty( "manufacturer" )]
	public string? Manufacturer { get; set; }

	/// <summary>End-user visible model name for the device.</summary>
	[JsonProperty( "model" )]
	public string? Model { get; set; }

	/// <summary>A constant representing the type of the device.</summary>
	[JsonProperty( "type" )]
	public string? Type { get; set; }

	/// <summary>
	/// The serial number or other unique ID for the hardware. This field is obfuscated when read by any REST or
	/// Android client that did not create the data source. Only the data source creator will see the uid field in
	/// clear and normal form. The obfuscation preserves equality; that is, given two IDs, if id1 == id2,
	/// obfuscated(id1) == obfuscated(id2).
	/// </summary>
	[JsonProperty( "uid" )]
	public string? Uid { get; set; }

	/// <summary>Version string for the device hardware/software.</summary>
	[JsonProperty( "version" )]
	public string? Version { get; set; }
}

public sealed class ListSessionsResponse
{
	/// <summary>
	/// If includeDeleted is set to true in the request, and startTime and endTime are omitted, this will include
	/// sessions which were deleted since the last sync.
	/// </summary>
	[JsonProperty( "deletedSession" )]
	public List<ActivitySession>? DeletedSession { get; set; }

	/// <summary>
	/// The sync token which is used to sync further changes. This will only be provided if both startTime and
	/// endTime are omitted from the request.
	/// </summary>
	[JsonProperty( "nextPageToken" )]
	public string? NextPageToken { get; set; }

	/// <summary>Sessions with an end time that is between startTime and endTime of the request.</summary>
	[JsonProperty( "session", Required = Required.Always )]
	public required List<ActivitySession> Session { get; set; }
}

/// <summary>Sessions contain metadata, such as a user-friendly name and time interval information.</summary>
public sealed class ActivitySession
{
	/// <summary>
	/// Session active time. While start_time_millis and end_time_millis define the full session time, the active
	/// time can be shorter and specified by active_time_millis. If the inactive time during the session is known,
	/// it should also be inserted via a com.google.activity.segment data point with a STILL activity value
	/// </summary>
	[JsonProperty( "activeTimeMillis" )]
	public long? ActiveTimeMillis { get; set; }

	/// <summary>The type of activity this session represents.</summary>
	[JsonProperty( "activityType" )]
	public int? ActivityType { get; set; }

	/// <summary>The application that created the session.</summary>
	[JsonProperty( "application" )]
	public required Application Application { get; set; }

	/// <summary>A description for this session.</summary>
	[JsonProperty( "description" )]
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// A client-generated identifier that is unique across all sessions owned by this particular user.
	/// </summary>
	[JsonProperty( "id" )]
	public string Id { get; set; } = string.Empty;

	/// <summary>A timestamp that indicates when the session was last modified.</summary>
	[JsonProperty( "modifiedTimeMillis" )]
	public long? ModifiedTimeMillis { get; set; }

	/// <summary>A human readable name of the session.</summary>
	[JsonProperty( "name" )]
	public string Name { get; set; } = string.Empty;

	/// <summary>A start time, in milliseconds since epoch, inclusive.</summary>
	[JsonProperty( "startTimeMillis", Required = Required.Always )]
	public long StartTimeMillis { get; set; }

	/// <summary>An end time, in milliseconds since epoch, inclusive.</summary>
	[JsonProperty( "endTimeMillis", Required = Required.Always )]
	public long EndTimeMillis { get; set; }
}

/// <summary>
/// Holder object for the value of a single field in a data point. A field value has a particular format and is only
/// ever set to one of an integer or a floating point value.
/// </summary>
public sealed class Value
{
	/// <summary>Floating point value. When this is set, other values must not be set.</summary>
	[JsonProperty( "fpVal" )]
	public double? FpVal { get; set; }

	/// <summary>Integer value. When this is set, other values must not be set.</summary>
	[JsonProperty( "intVal" )]
	public int? IntVal { get; set; }

	/// <summary>
	/// String value. When this is set, other values must not be set. Strings should be kept small whenever
	/// possible. Data streams with large string values and high data frequency may be down sampled.
	/// </summary>
	[JsonProperty( "stringVal" )]
	public string? StringVal { get; set; }
}