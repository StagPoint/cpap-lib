using System.Collections.Generic;
using System.Drawing;
using System.Reactive;

using cpaplib;

namespace cpap_app.ViewModels;

public class MachineSettingsItemViewModel
{
	public string Name  { get; set; }
	public object Value { get; set; }
	public string Units { get; set; }

	public MachineSettingsItemViewModel( string name, object value, string units = "" )
	{
		Name  = name;
		Value = value;
		Units = units;
	}

	public override string ToString()
	{
		return $"{Name} = {Value} {Units}";
	}
}

public class MachineSettingsViewModel
{
	public List<MachineSettingsItemViewModel> Settings { get; init; } = new List<MachineSettingsItemViewModel>();
}
