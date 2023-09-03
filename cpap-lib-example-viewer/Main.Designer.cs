namespace cpap_lib_example_viewer;

partial class Main
{
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	///  Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
		mnuMain = new MenuStrip();
		fileToolStripMenuItem = new ToolStripMenuItem();
		importToolStripMenuItem = new ToolStripMenuItem();
		exitToolStripMenuItem = new ToolStripMenuItem();
		lblWelcomeHeader = new Label();
		pnlWelcome = new Panel();
		lblWelcomeText = new Label();
		pnlSplitContainer = new SplitContainer();
		pnlSummary = new Panel();
		pnlInfo = new Panel();
		lblAHI = new Label();
		lblMachineIdentifier = new Label();
		lblSerialNumber = new Label();
		lblMode = new Label();
		lblPressure = new Label();
		lvwStatistics = new ListView();
		columnHeader1 = new ColumnHeader();
		columnHeader2 = new ColumnHeader();
		columnHeader3 = new ColumnHeader();
		columnHeader4 = new ColumnHeader();
		lblSelectedDate = new Label();
		calMain = new MonthCalendar();
		pnlGraphs = new Panel();
		mnuMain.SuspendLayout();
		pnlWelcome.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)pnlSplitContainer).BeginInit();
		pnlSplitContainer.Panel1.SuspendLayout();
		pnlSplitContainer.Panel2.SuspendLayout();
		pnlSplitContainer.SuspendLayout();
		pnlSummary.SuspendLayout();
		pnlInfo.SuspendLayout();
		SuspendLayout();
		// 
		// mnuMain
		// 
		mnuMain.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
		mnuMain.Location = new Point(0, 0);
		mnuMain.Name = "mnuMain";
		mnuMain.Size = new Size(1008, 25);
		mnuMain.TabIndex = 0;
		mnuMain.Text = "menuStrip1";
		// 
		// fileToolStripMenuItem
		// 
		fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importToolStripMenuItem, exitToolStripMenuItem });
		fileToolStripMenuItem.Name = "fileToolStripMenuItem";
		fileToolStripMenuItem.Size = new Size(39, 21);
		fileToolStripMenuItem.Text = "&File";
		// 
		// importToolStripMenuItem
		// 
		importToolStripMenuItem.Name = "importToolStripMenuItem";
		importToolStripMenuItem.Size = new Size(115, 22);
		importToolStripMenuItem.Text = "&Import";
		importToolStripMenuItem.Click += importToolStripMenuItem_Click;
		// 
		// exitToolStripMenuItem
		// 
		exitToolStripMenuItem.Name = "exitToolStripMenuItem";
		exitToolStripMenuItem.Size = new Size(115, 22);
		exitToolStripMenuItem.Text = "E&xit";
		exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
		// 
		// lblWelcomeHeader
		// 
		lblWelcomeHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblWelcomeHeader.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
		lblWelcomeHeader.Location = new Point(0, 0);
		lblWelcomeHeader.Margin = new Padding(0);
		lblWelcomeHeader.Name = "lblWelcomeHeader";
		lblWelcomeHeader.Size = new Size(535, 23);
		lblWelcomeHeader.TabIndex = 0;
		lblWelcomeHeader.Text = "Welcome to the CPAP Explorer sample application";
		lblWelcomeHeader.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// pnlWelcome
		// 
		pnlWelcome.Controls.Add(lblWelcomeText);
		pnlWelcome.Controls.Add(lblWelcomeHeader);
		pnlWelcome.Location = new Point(5000, 161);
		pnlWelcome.Name = "pnlWelcome";
		pnlWelcome.Size = new Size(537, 263);
		pnlWelcome.TabIndex = 4;
		// 
		// lblWelcomeText
		// 
		lblWelcomeText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		lblWelcomeText.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
		lblWelcomeText.Location = new Point(0, 46);
		lblWelcomeText.Name = "lblWelcomeText";
		lblWelcomeText.Size = new Size(537, 217);
		lblWelcomeText.TabIndex = 4;
		lblWelcomeText.Text = resources.GetString("lblWelcomeText.Text");
		// 
		// pnlSplitContainer
		// 
		pnlSplitContainer.Dock = DockStyle.Fill;
		pnlSplitContainer.Location = new Point(0, 25);
		pnlSplitContainer.Name = "pnlSplitContainer";
		// 
		// pnlSplitContainer.Panel1
		// 
		pnlSplitContainer.Panel1.BackColor = SystemColors.ActiveBorder;
		pnlSplitContainer.Panel1.Controls.Add(pnlSummary);
		// 
		// pnlSplitContainer.Panel2
		// 
		pnlSplitContainer.Panel2.Controls.Add(pnlGraphs);
		pnlSplitContainer.Size = new Size(1008, 736);
		pnlSplitContainer.SplitterDistance = 295;
		pnlSplitContainer.SplitterWidth = 5;
		pnlSplitContainer.TabIndex = 5;
		pnlSplitContainer.Visible = false;
		// 
		// pnlSummary
		// 
		pnlSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		pnlSummary.BackColor = SystemColors.Control;
		pnlSummary.BorderStyle = BorderStyle.FixedSingle;
		pnlSummary.Controls.Add(pnlInfo);
		pnlSummary.Controls.Add(lblSelectedDate);
		pnlSummary.Controls.Add(calMain);
		pnlSummary.Location = new Point(0, 0);
		pnlSummary.Name = "pnlSummary";
		pnlSummary.Size = new Size(295, 736);
		pnlSummary.TabIndex = 2;
		pnlSummary.Visible = false;
		// 
		// pnlInfo
		// 
		pnlInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		pnlInfo.AutoScroll = true;
		pnlInfo.BackColor = SystemColors.Control;
		pnlInfo.Controls.Add(lblAHI);
		pnlInfo.Controls.Add(lblMachineIdentifier);
		pnlInfo.Controls.Add(lblSerialNumber);
		pnlInfo.Controls.Add(lblMode);
		pnlInfo.Controls.Add(lblPressure);
		pnlInfo.Controls.Add(lvwStatistics);
		pnlInfo.Location = new Point(0, 230);
		pnlInfo.Name = "pnlInfo";
		pnlInfo.Size = new Size(294, 504);
		pnlInfo.TabIndex = 4;
		pnlInfo.Visible = false;
		// 
		// lblAHI
		// 
		lblAHI.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblAHI.BackColor = SystemColors.InfoText;
		lblAHI.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
		lblAHI.ForeColor = SystemColors.ControlLightLight;
		lblAHI.Location = new Point(3, 0);
		lblAHI.Name = "lblAHI";
		lblAHI.Size = new Size(292, 25);
		lblAHI.TabIndex = 2;
		lblAHI.Text = "AHI";
		lblAHI.TextAlign = ContentAlignment.TopCenter;
		// 
		// lblMachineIdentifier
		// 
		lblMachineIdentifier.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblMachineIdentifier.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
		lblMachineIdentifier.Location = new Point(3, 26);
		lblMachineIdentifier.Name = "lblMachineIdentifier";
		lblMachineIdentifier.Size = new Size(290, 23);
		lblMachineIdentifier.TabIndex = 3;
		lblMachineIdentifier.Text = "ResMed Machine Name";
		lblMachineIdentifier.TextAlign = ContentAlignment.TopCenter;
		// 
		// lblSerialNumber
		// 
		lblSerialNumber.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblSerialNumber.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
		lblSerialNumber.Location = new Point(3, 46);
		lblSerialNumber.Name = "lblSerialNumber";
		lblSerialNumber.Size = new Size(290, 23);
		lblSerialNumber.TabIndex = 4;
		lblSerialNumber.Text = "Serial Number";
		lblSerialNumber.TextAlign = ContentAlignment.TopCenter;
		// 
		// lblMode
		// 
		lblMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblMode.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
		lblMode.Location = new Point(3, 69);
		lblMode.Name = "lblMode";
		lblMode.Size = new Size(285, 23);
		lblMode.TabIndex = 5;
		lblMode.Text = "Mode";
		lblMode.TextAlign = ContentAlignment.TopCenter;
		// 
		// lblPressure
		// 
		lblPressure.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblPressure.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
		lblPressure.Location = new Point(3, 92);
		lblPressure.Name = "lblPressure";
		lblPressure.Size = new Size(285, 23);
		lblPressure.TabIndex = 6;
		lblPressure.Text = "Mode";
		lblPressure.TextAlign = ContentAlignment.TopCenter;
		// 
		// lvwStatistics
		// 
		lvwStatistics.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lvwStatistics.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
		lvwStatistics.HeaderStyle = ColumnHeaderStyle.Nonclickable;
		lvwStatistics.Location = new Point(3, 121);
		lvwStatistics.Name = "lvwStatistics";
		lvwStatistics.Scrollable = false;
		lvwStatistics.Size = new Size(285, 381);
		lvwStatistics.TabIndex = 7;
		lvwStatistics.UseCompatibleStateImageBehavior = false;
		lvwStatistics.View = View.Details;
		// 
		// columnHeader1
		// 
		columnHeader1.Text = "Signal";
		columnHeader1.Width = 100;
		// 
		// columnHeader2
		// 
		columnHeader2.Text = "Min";
		// 
		// columnHeader3
		// 
		columnHeader3.Text = "Med";
		// 
		// columnHeader4
		// 
		columnHeader4.Text = "95%";
		// 
		// lblSelectedDate
		// 
		lblSelectedDate.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
		lblSelectedDate.Location = new Point(0, 0);
		lblSelectedDate.Name = "lblSelectedDate";
		lblSelectedDate.Size = new Size(299, 37);
		lblSelectedDate.TabIndex = 2;
		lblSelectedDate.Text = "Selected Date";
		lblSelectedDate.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// calMain
		// 
		calMain.Location = new Point(34, 40);
		calMain.Name = "calMain";
		calMain.TabIndex = 0;
		// 
		// pnlGraphs
		// 
		pnlGraphs.BorderStyle = BorderStyle.FixedSingle;
		pnlGraphs.Dock = DockStyle.Fill;
		pnlGraphs.Location = new Point(0, 0);
		pnlGraphs.Name = "pnlGraphs";
		pnlGraphs.Size = new Size(708, 736);
		pnlGraphs.TabIndex = 3;
		pnlGraphs.Visible = false;
		// 
		// Main
		// 
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(1008, 761);
		Controls.Add(pnlSplitContainer);
		Controls.Add(pnlWelcome);
		Controls.Add(mnuMain);
		MainMenuStrip = mnuMain;
		Name = "Main";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Example CPAP Data Viewer";
		Load += Main_Load;
		mnuMain.ResumeLayout(false);
		mnuMain.PerformLayout();
		pnlWelcome.ResumeLayout(false);
		pnlSplitContainer.Panel1.ResumeLayout(false);
		pnlSplitContainer.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)pnlSplitContainer).EndInit();
		pnlSplitContainer.ResumeLayout(false);
		pnlSummary.ResumeLayout(false);
		pnlInfo.ResumeLayout(false);
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private MenuStrip mnuMain;
	private ToolStripMenuItem fileToolStripMenuItem;
	private ToolStripMenuItem importToolStripMenuItem;
	private ToolStripMenuItem exitToolStripMenuItem;
	private Label lblWelcomeHeader;
	private Panel pnlWelcome;
	private Label lblWelcomeText;
	private SplitContainer pnlSplitContainer;
	private Panel pnlSummary;
	private Label lblSelectedDate;
	private MonthCalendar calMain;
	private Panel pnlGraphs;
	private Panel pnlInfo;
	private Label lblAHI;
	private Label lblMachineIdentifier;
	private Label lblSerialNumber;
	private Label lblMode;
	private Label lblPressure;
	private ListView lvwStatistics;
	private ColumnHeader columnHeader1;
	private ColumnHeader columnHeader2;
	private ColumnHeader columnHeader3;
	private ColumnHeader columnHeader4;
}
