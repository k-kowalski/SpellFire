namespace SpellFire.Primer.Gui
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.panel1 = new System.Windows.Forms.Panel();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.buttonPreset = new System.Windows.Forms.Button();
			this.comboBoxPresets = new System.Windows.Forms.ComboBox();
			this.radarCanvas = new SpellFire.Primer.Gui.RadarCanvas();
			this.labelInfo = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.listBoxSolutions = new System.Windows.Forms.ListBox();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonToggle = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxProcesses = new System.Windows.Forms.ComboBox();
			this.panel1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panel1.Controls.Add(this.groupBox2);
			this.panel1.Controls.Add(this.radarCanvas);
			this.panel1.Controls.Add(this.labelInfo);
			this.panel1.Controls.Add(this.groupBox1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(938, 505);
			this.panel1.TabIndex = 0;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.buttonPreset);
			this.groupBox2.Controls.Add(this.comboBoxPresets);
			this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.groupBox2.Location = new System.Drawing.Point(12, 345);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(214, 148);
			this.groupBox2.TabIndex = 12;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Launch from preset";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 30);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(87, 16);
			this.label3.TabIndex = 11;
			this.label3.Text = "Select preset";
			// 
			// buttonPreset
			// 
			this.buttonPreset.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.buttonPreset.Location = new System.Drawing.Point(61, 107);
			this.buttonPreset.Name = "buttonPreset";
			this.buttonPreset.Size = new System.Drawing.Size(87, 35);
			this.buttonPreset.TabIndex = 13;
			this.buttonPreset.Text = "Launch";
			this.buttonPreset.UseVisualStyleBackColor = true;
			this.buttonPreset.Click += new System.EventHandler(this.buttonPreset_Click);
			// 
			// comboBoxPresets
			// 
			this.comboBoxPresets.FormattingEnabled = true;
			this.comboBoxPresets.Location = new System.Drawing.Point(15, 46);
			this.comboBoxPresets.Name = "comboBoxPresets";
			this.comboBoxPresets.Size = new System.Drawing.Size(186, 24);
			this.comboBoxPresets.TabIndex = 10;
			// 
			// radarCanvas
			// 
			this.radarCanvas.BackColor = System.Drawing.SystemColors.ControlLight;
			this.radarCanvas.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("radarCanvas.BackgroundImage")));
			this.radarCanvas.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.radarCanvas.Location = new System.Drawing.Point(247, 4);
			this.radarCanvas.Name = "radarCanvas";
			this.radarCanvas.Size = new System.Drawing.Size(679, 489);
			this.radarCanvas.TabIndex = 10;
			this.radarCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.radarCanvas_Paint);
			// 
			// labelInfo
			// 
			this.labelInfo.AutoSize = true;
			this.labelInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.labelInfo.ForeColor = System.Drawing.Color.Blue;
			this.labelInfo.Location = new System.Drawing.Point(9, 20);
			this.labelInfo.Name = "labelInfo";
			this.labelInfo.Size = new System.Drawing.Size(197, 16);
			this.labelInfo.TabIndex = 5;
			this.labelInfo.Text = "Attach to running WoW process.";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.listBoxSolutions);
			this.groupBox1.Controls.Add(this.buttonRefresh);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.buttonToggle);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.comboBoxProcesses);
			this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.groupBox1.Location = new System.Drawing.Point(12, 55);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(214, 283);
			this.groupBox1.TabIndex = 11;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Inject running process";
			// 
			// listBoxSolutions
			// 
			this.listBoxSolutions.FormattingEnabled = true;
			this.listBoxSolutions.ItemHeight = 16;
			this.listBoxSolutions.Location = new System.Drawing.Point(15, 117);
			this.listBoxSolutions.Name = "listBoxSolutions";
			this.listBoxSolutions.Size = new System.Drawing.Size(186, 116);
			this.listBoxSolutions.TabIndex = 6;
			// 
			// buttonRefresh
			// 
			this.buttonRefresh.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.buttonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("buttonRefresh.Image")));
			this.buttonRefresh.Location = new System.Drawing.Point(153, 50);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(48, 39);
			this.buttonRefresh.TabIndex = 9;
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 34);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(135, 16);
			this.label1.TabIndex = 7;
			this.label1.Text = "Select WoW process";
			// 
			// buttonToggle
			// 
			this.buttonToggle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.buttonToggle.Location = new System.Drawing.Point(61, 242);
			this.buttonToggle.Name = "buttonToggle";
			this.buttonToggle.Size = new System.Drawing.Size(87, 35);
			this.buttonToggle.TabIndex = 4;
			this.buttonToggle.Text = "Start";
			this.buttonToggle.UseVisualStyleBackColor = true;
			this.buttonToggle.Click += new System.EventHandler(this.buttonToggle_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 101);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(95, 16);
			this.label2.TabIndex = 8;
			this.label2.Text = "Select solution";
			// 
			// comboBoxProcesses
			// 
			this.comboBoxProcesses.FormattingEnabled = true;
			this.comboBoxProcesses.Location = new System.Drawing.Point(15, 50);
			this.comboBoxProcesses.Name = "comboBoxProcesses";
			this.comboBoxProcesses.Size = new System.Drawing.Size(121, 24);
			this.comboBoxProcesses.TabIndex = 0;
			this.comboBoxProcesses.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxProcesses_KeyPress);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(938, 505);
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.Text = "SpellFire";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ComboBox comboBoxProcesses;
		private System.Windows.Forms.ListBox listBoxSolutions;
		private System.Windows.Forms.Label labelInfo;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonRefresh;
		private RadarCanvas radarCanvas;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonPreset;
		private System.Windows.Forms.ComboBox comboBoxPresets;
		private System.Windows.Forms.Button buttonToggle;
	}
}

