namespace BucketDesktop
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtDestination = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.chkAutoEject = new System.Windows.Forms.CheckBox();
            this.btnEject = new System.Windows.Forms.Button();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.grpProgress = new System.Windows.Forms.GroupBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnCopy = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.bkgndWorker = new System.ComponentModel.BackgroundWorker();
            this.ejectorThread = new System.ComponentModel.BackgroundWorker();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.grpProgress.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtDestination);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(166, 48);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Destination";
            // 
            // txtDestination
            // 
            this.txtDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestination.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.txtDestination.Location = new System.Drawing.Point(6, 19);
            this.txtDestination.Name = "txtDestination";
            this.txtDestination.ReadOnly = true;
            this.txtDestination.Size = new System.Drawing.Size(154, 20);
            this.txtDestination.TabIndex = 0;
            this.txtDestination.DoubleClick += new System.EventHandler(this.txtDestination_DoubleClick);
            this.txtDestination.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtDestination_MouseDoubleClick);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtSource);
            this.groupBox2.Controls.Add(this.chkAutoEject);
            this.groupBox2.Controls.Add(this.btnEject);
            this.groupBox2.Location = new System.Drawing.Point(184, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(321, 48);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Source";
            // 
            // txtSource
            // 
            this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSource.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.txtSource.Location = new System.Drawing.Point(6, 19);
            this.txtSource.Name = "txtSource";
            this.txtSource.ReadOnly = true;
            this.txtSource.Size = new System.Drawing.Size(154, 20);
            this.txtSource.TabIndex = 1;
            // 
            // chkAutoEject
            // 
            this.chkAutoEject.AutoSize = true;
            this.chkAutoEject.Checked = true;
            this.chkAutoEject.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoEject.Location = new System.Drawing.Point(247, 21);
            this.chkAutoEject.Name = "chkAutoEject";
            this.chkAutoEject.Size = new System.Drawing.Size(73, 17);
            this.chkAutoEject.TabIndex = 5;
            this.chkAutoEject.Text = "auto-eject";
            this.chkAutoEject.UseVisualStyleBackColor = true;
            // 
            // btnEject
            // 
            this.btnEject.Location = new System.Drawing.Point(166, 17);
            this.btnEject.Name = "btnEject";
            this.btnEject.Size = new System.Drawing.Size(75, 23);
            this.btnEject.TabIndex = 4;
            this.btnEject.Text = "Eject";
            this.btnEject.UseVisualStyleBackColor = true;
            this.btnEject.Click += new System.EventHandler(this.btnEject_Click);
            // 
            // notifyIcon
            // 
            this.notifyIcon.Text = "notifyIcon1";
            // 
            // grpProgress
            // 
            this.grpProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpProgress.Controls.Add(this.progressBar);
            this.grpProgress.Location = new System.Drawing.Point(12, 66);
            this.grpProgress.Name = "grpProgress";
            this.grpProgress.Size = new System.Drawing.Size(580, 43);
            this.grpProgress.TabIndex = 5;
            this.grpProgress.TabStop = false;
            this.grpProgress.Text = "Progress";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(6, 19);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(568, 18);
            this.progressBar.TabIndex = 0;
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(511, 12);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(81, 48);
            this.btnCopy.TabIndex = 6;
            this.btnCopy.Text = "Copy";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 200;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // bkgndWorker
            // 
            this.bkgndWorker.WorkerReportsProgress = true;
            this.bkgndWorker.WorkerSupportsCancellation = true;
            this.bkgndWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bkgndWorker_DoWork);
            this.bkgndWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.bkgndWorker_ProgressChanged);
            this.bkgndWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bkgndWorker_RunWorkerCompleted);
            // 
            // ejectorThread
            // 
            this.ejectorThread.WorkerReportsProgress = true;
            this.ejectorThread.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ejectorThread_DoWork);
            this.ejectorThread.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.ejectorThread_ProgressChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(604, 121);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.grpProgress);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.grpProgress.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkAutoEject;
        private System.Windows.Forms.Button btnEject;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.GroupBox grpProgress;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.TextBox txtSource;
        private System.ComponentModel.BackgroundWorker bkgndWorker;
        private System.ComponentModel.BackgroundWorker ejectorThread;
    }
}

