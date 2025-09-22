namespace XisCoreSensors
{
    partial class FrmMainMDI
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMainMDI));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.model1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLayoutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.loadLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuChkEditMode = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pLCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tagMapperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeEditModePasswordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblEditModeStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblPlcStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.notificationBar1 = new XisCoreSensors.Controls.NotificationBar();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(991, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "Main Menu";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.model1ToolStripMenuItem,
            this.loadImageToolStripMenuItem,
            this.saveLayoutToolStripMenuItem,
            this.saveLayoutToolStripMenuItem1,
            this.loadLayoutToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // model1ToolStripMenuItem
            // 
            this.model1ToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.play_icon;
            this.model1ToolStripMenuItem.Name = "model1ToolStripMenuItem";
            this.model1ToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.model1ToolStripMenuItem.Text = "View";
            this.model1ToolStripMenuItem.Click += new System.EventHandler(this.model1ToolStripMenuItem_Click);
            // 
            // loadImageToolStripMenuItem
            // 
            this.loadImageToolStripMenuItem.Enabled = false;
            this.loadImageToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.Images_icon;
            this.loadImageToolStripMenuItem.Name = "loadImageToolStripMenuItem";
            this.loadImageToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.loadImageToolStripMenuItem.Text = "Change image...";
            this.loadImageToolStripMenuItem.Click += new System.EventHandler(this.loadImageToolStripMenuItem_Click);
            // 
            // saveLayoutToolStripMenuItem
            // 
            this.saveLayoutToolStripMenuItem.Enabled = false;
            this.saveLayoutToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.saves;
            this.saveLayoutToolStripMenuItem.Name = "saveLayoutToolStripMenuItem";
            this.saveLayoutToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.saveLayoutToolStripMenuItem.Text = "Save Layout As...";
            this.saveLayoutToolStripMenuItem.Click += new System.EventHandler(this.saveLayoutToolStripMenuItem_Click);
            // 
            // saveLayoutToolStripMenuItem1
            // 
            this.saveLayoutToolStripMenuItem1.Enabled = false;
            this.saveLayoutToolStripMenuItem1.Image = global::XisCoreSensors.Properties.Resources.save_icon;
            this.saveLayoutToolStripMenuItem1.Name = "saveLayoutToolStripMenuItem1";
            this.saveLayoutToolStripMenuItem1.Size = new System.Drawing.Size(162, 22);
            this.saveLayoutToolStripMenuItem1.Text = "Save Layout";
            this.saveLayoutToolStripMenuItem1.Click += new System.EventHandler(this.saveLayoutToolStripMenuItem1_Click);
            // 
            // loadLayoutToolStripMenuItem
            // 
            this.loadLayoutToolStripMenuItem.Enabled = false;
            this.loadLayoutToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.Folder_Open_icon;
            this.loadLayoutToolStripMenuItem.Name = "loadLayoutToolStripMenuItem";
            this.loadLayoutToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.loadLayoutToolStripMenuItem.Text = "Load Layout...";
            this.loadLayoutToolStripMenuItem.Click += new System.EventHandler(this.loadLayoutToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuChkEditMode});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // mnuChkEditMode
            // 
            this.mnuChkEditMode.Name = "mnuChkEditMode";
            this.mnuChkEditMode.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.mnuChkEditMode.Size = new System.Drawing.Size(147, 22);
            this.mnuChkEditMode.Text = "Edit mode";
            this.mnuChkEditMode.Click += new System.EventHandler(this.mnuChkEditMode_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pLCToolStripMenuItem,
            this.tagMapperToolStripMenuItem,
            this.changeEditModePasswordToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // pLCToolStripMenuItem
            // 
            this.pLCToolStripMenuItem.Enabled = false;
            this.pLCToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.plc;
            this.pLCToolStripMenuItem.Name = "pLCToolStripMenuItem";
            this.pLCToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
            this.pLCToolStripMenuItem.Text = "PLC...";
            this.pLCToolStripMenuItem.Click += new System.EventHandler(this.pLCToolStripMenuItem_Click);
            // 
            // tagMapperToolStripMenuItem
            // 
            this.tagMapperToolStripMenuItem.Enabled = false;
            this.tagMapperToolStripMenuItem.Image = global::XisCoreSensors.Properties.Resources.tags_icon;
            this.tagMapperToolStripMenuItem.Name = "tagMapperToolStripMenuItem";
            this.tagMapperToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
            this.tagMapperToolStripMenuItem.Text = "Tag Mapper...";
            this.tagMapperToolStripMenuItem.Click += new System.EventHandler(this.tagMapperToolStripMenuItem_Click);
            // 
            // changeEditModePasswordToolStripMenuItem
            // 
            this.changeEditModePasswordToolStripMenuItem.Name = "changeEditModePasswordToolStripMenuItem";
            this.changeEditModePasswordToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
            this.changeEditModePasswordToolStripMenuItem.Text = "Change edit mode password...";
            this.changeEditModePasswordToolStripMenuItem.Click += new System.EventHandler(this.changeEditModePasswordToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblEditModeStatus,
            this.lblPlcStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 583);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(991, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblEditModeStatus
            // 
            this.lblEditModeStatus.BackColor = System.Drawing.Color.Lime;
            this.lblEditModeStatus.Name = "lblEditModeStatus";
            this.lblEditModeStatus.Size = new System.Drawing.Size(91, 17);
            this.lblEditModeStatus.Text = "MODE: LOCKED";
            // 
            // lblPlcStatus
            // 
            this.lblPlcStatus.Name = "lblPlcStatus";
            this.lblPlcStatus.Size = new System.Drawing.Size(118, 17);
            this.lblPlcStatus.Text = "toolStripStatusLabel1";
            // 
            // notificationBar1
            // 
            this.notificationBar1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.notificationBar1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.notificationBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.notificationBar1.ForeColor = System.Drawing.Color.Yellow;
            this.notificationBar1.Location = new System.Drawing.Point(0, 24);
            this.notificationBar1.Name = "notificationBar1";
            this.notificationBar1.Size = new System.Drawing.Size(991, 100);
            this.notificationBar1.TabIndex = 5;
            this.notificationBar1.Visible = false;
            // 
            // FrmMainMDI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(991, 605);
            this.Controls.Add(this.notificationBar1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmMainMDI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Xis Sensors 0.1.7";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMainMDI_FormClosing);
            this.Load += new System.EventHandler(this.FrmMainMDI_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmMainMDI_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem model1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuChkEditMode;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblEditModeStatus;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private Controls.NotificationBar notificationBar1;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pLCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tagMapperToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel lblPlcStatus;
        private System.Windows.Forms.ToolStripMenuItem changeEditModePasswordToolStripMenuItem;
    }
}

