namespace cpap_app.Configuration;

public class ApplicationSettings
{
	public ApplicationThemeType Theme { get; set; } = ApplicationThemeType.System;
}

public class StoredNumericSetting
{
	public int    ID    { get; set; }
	public string Key   { get; set; } = string.Empty;
	public double Value { get; set; } = 0;
}

public class StoredStringSetting
{
	public int    ID    { get; set; }
	public string Key   { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
}
