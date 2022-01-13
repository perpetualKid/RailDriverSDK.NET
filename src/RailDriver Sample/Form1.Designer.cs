namespace RailDriver.Sample
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.BtnEnumerate = new System.Windows.Forms.Button();
            this.CboDevices = new System.Windows.Forms.ComboBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.BtnCallback = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.BtnClear = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.BtnWriteDisplay = new System.Windows.Forms.Button();
            this.BtnSpeakerOn = new System.Windows.Forms.Button();
            this.BtnSpeakerOff = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // BtnEnumerate
            // 
            this.BtnEnumerate.Location = new System.Drawing.Point(12, 26);
            this.BtnEnumerate.Name = "BtnEnumerate";
            this.BtnEnumerate.Size = new System.Drawing.Size(131, 31);
            this.BtnEnumerate.TabIndex = 0;
            this.BtnEnumerate.Text = "Enumerate";
            this.BtnEnumerate.UseVisualStyleBackColor = true;
            this.BtnEnumerate.Click += new System.EventHandler(this.BtnEnumerate_Click);
            // 
            // CboDevices
            // 
            this.CboDevices.FormattingEnabled = true;
            this.CboDevices.Location = new System.Drawing.Point(179, 25);
            this.CboDevices.Name = "CboDevices";
            this.CboDevices.Size = new System.Drawing.Size(305, 28);
            this.CboDevices.TabIndex = 1;
            this.CboDevices.SelectedIndexChanged += new System.EventHandler(this.CboDevices_SelectedIndexChanged);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 20;
            this.listBox1.Location = new System.Drawing.Point(12, 160);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(655, 104);
            this.listBox1.TabIndex = 6;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 534);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.statusStrip1.Size = new System.Drawing.Size(695, 26);
            this.statusStrip1.TabIndex = 9;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(151, 20);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // BtnCallback
            // 
            this.BtnCallback.Location = new System.Drawing.Point(12, 98);
            this.BtnCallback.Name = "BtnCallback";
            this.BtnCallback.Size = new System.Drawing.Size(175, 31);
            this.BtnCallback.TabIndex = 10;
            this.BtnCallback.Text = "Setup for Callback";
            this.BtnCallback.UseVisualStyleBackColor = true;
            this.BtnCallback.Click += new System.EventHandler(this.BtnCallback_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 20);
            this.label1.TabIndex = 16;
            this.label1.Text = "1. Do this first";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(257, 20);
            this.label2.TabIndex = 17;
            this.label2.Text = "2. Set for data callback and read data";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 289);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 20);
            this.label3.TabIndex = 18;
            this.label3.Text = "3. Write to Display";
            // 
            // BtnClear
            // 
            this.BtnClear.Location = new System.Drawing.Point(552, 117);
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.Size = new System.Drawing.Size(115, 37);
            this.BtnClear.TabIndex = 20;
            this.BtnClear.Text = "Clear Listbox";
            this.BtnClear.UseVisualStyleBackColor = true;
            this.BtnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(149, 314);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(61, 27);
            this.textBox1.TabIndex = 21;
            this.textBox1.Text = "110";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(83, 314);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(61, 27);
            this.textBox2.TabIndex = 22;
            this.textBox2.Text = "121";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(16, 314);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(61, 27);
            this.textBox3.TabIndex = 23;
            this.textBox3.Text = "118";
            // 
            // BtnWriteDisplay
            // 
            this.BtnWriteDisplay.Location = new System.Drawing.Point(217, 312);
            this.BtnWriteDisplay.Name = "BtnWriteDisplay";
            this.BtnWriteDisplay.Size = new System.Drawing.Size(137, 31);
            this.BtnWriteDisplay.TabIndex = 24;
            this.BtnWriteDisplay.Text = "Write To Display";
            this.BtnWriteDisplay.UseVisualStyleBackColor = true;
            this.BtnWriteDisplay.Click += new System.EventHandler(this.BtnWriteDisplay_Click);
            // 
            // BtnSpeakerOn
            // 
            this.BtnSpeakerOn.Location = new System.Drawing.Point(16, 417);
            this.BtnSpeakerOn.Name = "BtnSpeakerOn";
            this.BtnSpeakerOn.Size = new System.Drawing.Size(137, 31);
            this.BtnSpeakerOn.TabIndex = 25;
            this.BtnSpeakerOn.Text = "Speaker On";
            this.BtnSpeakerOn.UseVisualStyleBackColor = true;
            this.BtnSpeakerOn.Click += new System.EventHandler(this.BtnSpeakerOn_Click);
            // 
            // BtnSpeakerOff
            // 
            this.BtnSpeakerOff.Location = new System.Drawing.Point(159, 417);
            this.BtnSpeakerOff.Name = "BtnSpeakerOff";
            this.BtnSpeakerOff.Size = new System.Drawing.Size(137, 31);
            this.BtnSpeakerOff.TabIndex = 26;
            this.BtnSpeakerOff.Text = "Speaker Off";
            this.BtnSpeakerOff.UseVisualStyleBackColor = true;
            this.BtnSpeakerOff.Click += new System.EventHandler(this.BtnSpeakerOff_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 392);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(176, 20);
            this.label4.TabIndex = 27;
            this.label4.Text = "4. Turn Speaker On or Off";
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(695, 560);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.BtnSpeakerOff);
            this.Controls.Add(this.BtnSpeakerOn);
            this.Controls.Add(this.BtnWriteDisplay);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.BtnClear);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BtnCallback);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.CboDevices);
            this.Controls.Add(this.BtnEnumerate);
            //this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "C# RailDriver Sample";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnEnumerate;
        private System.Windows.Forms.ComboBox CboDevices;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Button BtnCallback;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button BtnClear;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Button BtnWriteDisplay;
        private System.Windows.Forms.Button BtnSpeakerOn;
        private System.Windows.Forms.Button BtnSpeakerOff;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer timer1;
    }
}

