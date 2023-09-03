namespace cpap_lib_example_viewer;

using cpaplib;

using System.Diagnostics;
using System.Windows.Forms;

public partial class Main : Form
{
	private CpapDataLoader _cpapData;

	public Main()
	{
		InitializeComponent();
	}

	private void Main_Load(object sender, EventArgs e)
	{
		this.Resize += Main_Resize;
		calMain.DateSelected += CalMain_DateSelected;

		pnlInfo.VerticalScroll.Enabled = true;
		pnlInfo.VerticalScroll.Visible = true;

		pnlSummary.Resize += Resize_CenterChildren;

		Main_Resize(null, null);
	}

	private void Resize_CenterChildren(object? sender, EventArgs e)
	{
		var parent = (Control)sender;

		foreach (var child in parent.Controls)
		{
			var control = child as Control;
			if (control != null)
			{
				var x = (parent.Width - control.Width) / 2;
				control.Location = new Point(x, control.Location.Y);
			}
		}
	}

	private void Main_Resize(object? sender, EventArgs e)
	{
		pnlWelcome.Location = new Point((this.Width - pnlWelcome.Width) / 2, pnlWelcome.Location.Y);
	}

	private void exitToolStripMenuItem_Click(object sender, EventArgs e)
	{
		this.Close();
	}

	private void importToolStripMenuItem_Click(object sender, EventArgs e)
	{
		var drives = DriveInfo.GetDrives();
		foreach (var drive in drives)
		{
			if (!drive.IsReady)
			{
				continue;
			}

			if (CpapDataLoader.HasCorrectFolderStructure(drive.RootDirectory.FullName))
			{
				Debug.WriteLine($"Found CPAP data at: {drive}");

				var result = MessageBox.Show($"Found CPAP data on drive {drive}. Do you want to import from this location?", "Found CPAP data", MessageBoxButtons.YesNo);
				if (result == DialogResult.OK)
				{
					loadCpapDataFrom(drive.RootDirectory.FullName);
					return;
				}
			}
		}

		var fbd = new FolderBrowserDialog();
		if (fbd.ShowDialog(this) == DialogResult.OK)
		{
			if (CpapDataLoader.HasCorrectFolderStructure(fbd.SelectedPath))
			{
				loadCpapDataFrom(fbd.SelectedPath);
			}
		}
	}

	private void CalMain_DateSelected(object? sender, DateRangeEventArgs e)
	{
		lblSelectedDate.Text = $"{e.Start.ToLongDateString()}";

		var dayReport = _cpapData.Days.FirstOrDefault(x => x.ReportDate.Date == e.Start);
		if (dayReport != null)
		{
			pnlSplitContainer.Visible = true;
			pnlInfo.Visible = true;
			pnlGraphs.Visible = true;

			LoadReport(dayReport);

			return;
		}

		pnlInfo.Visible = false;
		pnlGraphs.Visible = false;
	}

	private void loadCpapDataFrom(string path)
	{
		_cpapData = new CpapDataLoader();
		_cpapData.LoadFromFolder(path);

		if (_cpapData.Days.Count == 0)
		{
			return;
		}

		foreach (var day in _cpapData.Days)
		{
			calMain.AddBoldedDate(day.ReportDate);
		}

		pnlWelcome.Hide();
		pnlSummary.Show();

		var lastDay = _cpapData.Days.Last();

		calMain.SelectionStart = calMain.SelectionEnd = lastDay.ReportDate.Date;
		CalMain_DateSelected(null, new DateRangeEventArgs(calMain.SelectionStart, calMain.SelectionEnd));
	}

	private void LoadReport(DailyReport day)
	{
		Debug.WriteLine($"Load {day.ReportDate}");

		lblAHI.Text = $"AHI {day.EventCountSummary.AHI:F2}";
		lblMachineIdentifier.Text = _cpapData.MachineID.ProductName;
		lblMode.Text = $"Mode: {day.Settings.Mode}";

		switch (day.Settings.Mode)
		{
			case OperatingMode.APAP:
				lblPressure.Text = $"Pressure Range: {day.Settings.AutoSet.MinPressure:F2} cmH2O to {day.Settings.AutoSet.MaxPressure:F2} cmH02";
				break;
			default:
				lblPressure.Text = $"Pressure: {day.Settings.CPAP.Pressure:F2} cmH2O";
				break;
		}

		LoadDailyStatistics(day);
	}

	private void LoadDailyStatistics(DailyReport day)
	{
		var stats = day.Statistics;

		lvwStatistics.Items.Clear();

		addStatistic("Pressure", stats.MaskPress_50, stats.MaskPress_95, stats.MaskPress_Max);
		addStatistic("Minute Vent", stats.MinVent_50, stats.MinVent_95, stats.MinVent_Max);
		addStatistic("Resp. Rate", stats.RespRate_50, stats.RespRate_95, stats.RespRate_Max);
		addStatistic("Flow Limit", stats.Flow_5, stats.Flow_95, stats.Flow_95);
		addStatistic("Leak Rate", stats.Leak_50, stats.Leak_70, stats.Leak_Max);
		addStatistic("Tidal Volume", stats.TidVol_50, stats.TidVol_95, stats.TidVol_Max);
	}

	private void addStatistic(string text, double minValue, double medValue, double maxValue)
	{
		var item = new ListViewItem(text);
		item.SubItems.Add($"{minValue:F1}");
		item.SubItems.Add($"{medValue:F1}");
		item.SubItems.Add($"{maxValue:F1}");

		lvwStatistics.Items.Add(item);
	}

	private void scrollInfo_Scroll(object sender, ScrollEventArgs e)
	{
		Debug.WriteLine($"Scroll position: {e.NewValue}");
		pnlInfo.AutoScrollPosition = new Point(pnlInfo.AutoScrollPosition.X, e.NewValue);
	}
}
