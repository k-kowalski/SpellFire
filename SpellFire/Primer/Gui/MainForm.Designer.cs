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
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.labelInfo = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.listBoxSolutions = new System.Windows.Forms.ListBox();
			this.buttonToggle = new System.Windows.Forms.Button();
			this.comboBoxProcesses = new System.Windows.Forms.ComboBox();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panel1.Controls.Add(this.buttonRefresh);
			this.panel1.Controls.Add(this.labelInfo);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.listBoxSolutions);
			this.panel1.Controls.Add(this.buttonToggle);
			this.panel1.Controls.Add(this.comboBoxProcesses);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(229, 372);
			this.panel1.TabIndex = 0;
			// 
			// buttonRefresh
			// 
			this.buttonRefresh.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.buttonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("buttonRefresh.Image")));
			this.buttonRefresh.Location = new System.Drawing.Point(153, 136);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(48, 39);
			this.buttonRefresh.TabIndex = 9;
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			// 
			// labelInfo
			// 
			this.labelInfo.AutoSize = true;
			this.labelInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
			this.labelInfo.ForeColor = System.Drawing.Color.Blue;
			this.labelInfo.Location = new System.Drawing.Point(12, 9);
			this.labelInfo.Name = "labelInfo";
			this.labelInfo.Size = new System.Drawing.Size(179, 15);
			this.labelInfo.TabIndex = 5;
			this.labelInfo.Text = "Attach to running WoW process.";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 189);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(76, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Select solution";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 135);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(108, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Select WoW process";
			// 
			// listBoxSolutions
			// 
			this.listBoxSolutions.FormattingEnabled = true;
			this.listBoxSolutions.Location = new System.Drawing.Point(12, 210);
			this.listBoxSolutions.Name = "listBoxSolutions";
			this.listBoxSolutions.Size = new System.Drawing.Size(205, 95);
			this.listBoxSolutions.TabIndex = 6;
			// 
			// buttonToggle
			// 
			this.buttonToggle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.buttonToggle.Location = new System.Drawing.Point(73, 325);
			this.buttonToggle.Name = "buttonToggle";
			this.buttonToggle.Size = new System.Drawing.Size(87, 35);
			this.buttonToggle.TabIndex = 4;
			this.buttonToggle.Text = "Start";
			this.buttonToggle.UseVisualStyleBackColor = true;
			this.buttonToggle.Click += new System.EventHandler(this.buttonToggle_Click);
			// 
			// comboBoxProcesses
			// 
			this.comboBoxProcesses.FormattingEnabled = true;
			this.comboBoxProcesses.Location = new System.Drawing.Point(12, 154);
			this.comboBoxProcesses.Name = "comboBoxProcesses";
			this.comboBoxProcesses.Size = new System.Drawing.Size(121, 21);
			this.comboBoxProcesses.TabIndex = 0;
			this.comboBoxProcesses.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBoxProcesses_KeyPress);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(229, 372);
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.Text = "SpellFire";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ComboBox comboBoxProcesses;
		private System.Windows.Forms.Button buttonToggle;
		private System.Windows.Forms.ListBox listBoxSolutions;
		private System.Windows.Forms.Label labelInfo;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonRefresh;
	}
}

