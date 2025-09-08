namespace Ecr.Host
{
    partial class frmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnToggleApi = new System.Windows.Forms.Button();
            this.btnMinimizeToTray = new System.Windows.Forms.Button();
            this.pnlNotifications = new System.Windows.Forms.Panel();
            this.btnClearNotifications = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlNotifications.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(500, 35);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(12, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(121, 19);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Infinia Yazarkasa";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.pnlMain.Controls.Add(this.lblStatus);
            this.pnlMain.Controls.Add(this.btnToggleApi);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMain.Location = new System.Drawing.Point(0, 35);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(20);
            this.pnlMain.Size = new System.Drawing.Size(500, 109);
            this.pnlMain.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.lblStatus.Location = new System.Drawing.Point(20, 20);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(139, 15);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "API Durumu: Durduruldu";
            // 
            // btnToggleApi
            // 
            this.btnToggleApi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnToggleApi.FlatAppearance.BorderSize = 0;
            this.btnToggleApi.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.btnToggleApi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleApi.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnToggleApi.ForeColor = System.Drawing.Color.White;
            this.btnToggleApi.Location = new System.Drawing.Point(20, 46);
            this.btnToggleApi.Name = "btnToggleApi";
            this.btnToggleApi.Size = new System.Drawing.Size(140, 40);
            this.btnToggleApi.TabIndex = 1;
            this.btnToggleApi.Text = "API\'yi Başlat";
            this.btnToggleApi.UseVisualStyleBackColor = false;
            this.btnToggleApi.Click += new System.EventHandler(this.btnToggleApi_Click);
            // 
            // btnMinimizeToTray
            // 
            this.btnMinimizeToTray.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.btnMinimizeToTray.FlatAppearance.BorderSize = 0;
            this.btnMinimizeToTray.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnMinimizeToTray.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimizeToTray.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMinimizeToTray.ForeColor = System.Drawing.Color.White;
            this.btnMinimizeToTray.Location = new System.Drawing.Point(337, 93);
            this.btnMinimizeToTray.Name = "btnMinimizeToTray";
            this.btnMinimizeToTray.Size = new System.Drawing.Size(140, 40);
            this.btnMinimizeToTray.TabIndex = 2;
            this.btnMinimizeToTray.Text = "Gizle";
            this.btnMinimizeToTray.UseVisualStyleBackColor = false;
            this.btnMinimizeToTray.Click += new System.EventHandler(this.button2_Click);
            // 
            // pnlNotifications
            // 
            this.pnlNotifications.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pnlNotifications.Controls.Add(this.btnClearNotifications);
            this.pnlNotifications.Controls.Add(this.btnMinimizeToTray);
            this.pnlNotifications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNotifications.Location = new System.Drawing.Point(0, 144);
            this.pnlNotifications.Name = "pnlNotifications";
            this.pnlNotifications.Padding = new System.Windows.Forms.Padding(20);
            this.pnlNotifications.Size = new System.Drawing.Size(500, 156);
            this.pnlNotifications.TabIndex = 2;
            // 
            // btnClearNotifications
            // 
            this.btnClearNotifications.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearNotifications.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(196)))), ((int)(((byte)(43)))), ((int)(((byte)(28)))));
            this.btnClearNotifications.FlatAppearance.BorderSize = 0;
            this.btnClearNotifications.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.btnClearNotifications.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearNotifications.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnClearNotifications.ForeColor = System.Drawing.Color.White;
            this.btnClearNotifications.Location = new System.Drawing.Point(20, 93);
            this.btnClearNotifications.Name = "btnClearNotifications";
            this.btnClearNotifications.Size = new System.Drawing.Size(140, 40);
            this.btnClearNotifications.TabIndex = 2;
            this.btnClearNotifications.Text = "Programı Kapat";
            this.btnClearNotifications.UseVisualStyleBackColor = false;
            this.btnClearNotifications.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(500, 300);
            this.Controls.Add(this.pnlNotifications);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Infinia ECR";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.pnlNotifications.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnToggleApi;
        private System.Windows.Forms.Button btnMinimizeToTray;
        private System.Windows.Forms.Panel pnlNotifications;
        private System.Windows.Forms.Button btnClearNotifications;
    }
}

