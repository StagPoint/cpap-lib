using System;
using System.Collections.Generic;
using System.IO;

using cpaplib;

namespace cpapviewer.Loaders;

public interface ISessionDataLoader
{
	public string FriendlyName         { get; }
	public string Source               { get; }
	public string FileExtension        { get; }
	public string FilenameFilter       { get; }
	public string FilenameMatchPattern { get; }

	public (List<Session>, List<ReportedEvent>) Load( Stream stream );
	public (List<Session>, List<ReportedEvent>) Load( Stream stream, TimeSpan? timeAdjust );
}
