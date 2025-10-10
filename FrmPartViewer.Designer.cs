namespace XisCoreSensors
{
    partial class FrmPartViewer
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
            this.pnlViewport = new System.Windows.Forms.Panel();
            this.picCanvas = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.agregarSensorToolTipMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.normalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.laserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.contextMenuSensor = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.renombrarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.eliminarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toNormalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toLaserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlViewport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.contextMenuSensor.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlViewport
            // 
            this.pnlViewport.AutoScroll = true;
            this.pnlViewport.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlViewport.Controls.Add(this.picCanvas);
            this.pnlViewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlViewport.Location = new System.Drawing.Point(0, 0);
            this.pnlViewport.Name = "pnlViewport";
            this.pnlViewport.Size = new System.Drawing.Size(1824, 1002);
            this.pnlViewport.TabIndex = 0;
            // 
            // picCanvas
            // 
            this.picCanvas.ContextMenuStrip = this.contextMenu;
            this.picCanvas.Location = new System.Drawing.Point(0, 0);
            this.picCanvas.Name = "picCanvas";
            this.picCanvas.Size = new System.Drawing.Size(165, 105);
            this.picCanvas.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picCanvas.TabIndex = 0;
            this.picCanvas.TabStop = false;
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.agregarSensorToolTipMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(135, 26);
            // 
            // agregarSensorToolTipMenuItem
            // 
            this.agregarSensorToolTipMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.normalToolStripMenuItem,
            this.laserToolStripMenuItem});
            this.agregarSensorToolTipMenuItem.Name = "agregarSensorToolTipMenuItem";
            this.agregarSensorToolTipMenuItem.Size = new System.Drawing.Size(134, 22);
            this.agregarSensorToolTipMenuItem.Text = "Add Sensor";
            this.agregarSensorToolTipMenuItem.Click += new System.EventHandler(this.agregarSensorToolTipMenuItem_Click);
            // 
            // normalToolStripMenuItem
            // 
            this.normalToolStripMenuItem.Name = "normalToolStripMenuItem";
            this.normalToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.normalToolStripMenuItem.Text = "Normal";
            this.normalToolStripMenuItem.Click += new System.EventHandler(this.normalToolStripMenuItem_Click);
            // 
            // laserToolStripMenuItem
            // 
            this.laserToolStripMenuItem.Name = "laserToolStripMenuItem";
            this.laserToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.laserToolStripMenuItem.Text = "Laser";
            this.laserToolStripMenuItem.Click += new System.EventHandler(this.laserToolStripMenuItem_Click);
            // 
            // contextMenuSensor
            // 
            this.contextMenuSensor.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.renombrarSensorToolStripMenuItem,
            this.changeTypeToolStripMenuItem,
            this.toolStripMenuItem1,
            this.eliminarSensorToolStripMenuItem});
            this.contextMenuSensor.Name = "contextMenuSensor";
            this.contextMenuSensor.Size = new System.Drawing.Size(181, 98);
            this.contextMenuSensor.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuSensor_Opening);
            // 
            // renombrarSensorToolStripMenuItem
            // 
            this.renombrarSensorToolStripMenuItem.Name = "renombrarSensorToolStripMenuItem";
            this.renombrarSensorToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.renombrarSensorToolStripMenuItem.Text = "Edit ID...";
            this.renombrarSensorToolStripMenuItem.Click += new System.EventHandler(this.renombrarSensorToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(142, 6);
            // 
            // eliminarSensorToolStripMenuItem
            // 
            this.eliminarSensorToolStripMenuItem.Name = "eliminarSensorToolStripMenuItem";
            this.eliminarSensorToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.eliminarSensorToolStripMenuItem.Text = "Delete Sensor";
            this.eliminarSensorToolStripMenuItem.Click += new System.EventHandler(this.eliminarSensorToolStripMenuItem_Click);
            // 
            // changeTypeToolStripMenuItem
            // 
            this.changeTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toNormalToolStripMenuItem,
            this.toLaserToolStripMenuItem});
            this.changeTypeToolStripMenuItem.Name = "changeTypeToolStripMenuItem";
            this.changeTypeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.changeTypeToolStripMenuItem.Text = "Change type";
            // 
            // toNormalToolStripMenuItem
            // 
            this.toNormalToolStripMenuItem.Enabled = false;
            this.toNormalToolStripMenuItem.Name = "toNormalToolStripMenuItem";
            this.toNormalToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.toNormalToolStripMenuItem.Text = "To Normal";
            this.toNormalToolStripMenuItem.Click += new System.EventHandler(this.toNormalToolStripMenuItem_Click);
            // 
            // toLaserToolStripMenuItem
            // 
            this.toLaserToolStripMenuItem.Enabled = false;
            this.toLaserToolStripMenuItem.Name = "toLaserToolStripMenuItem";
            this.toLaserToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.toLaserToolStripMenuItem.Text = "To Laser";
            this.toLaserToolStripMenuItem.Click += new System.EventHandler(this.toLaserToolStripMenuItem_Click);
            // 
            // FrmPartViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1824, 1002);
            this.Controls.Add(this.pnlViewport);
            this.KeyPreview = true;
            this.Name = "FrmPartViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Model";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmPartViewer_FormClosing);
            this.Load += new System.EventHandler(this.FrmPartViewer_Load);
            this.Shown += new System.EventHandler(this.FrmPartViewer_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmPartViewer_KeyDown);
            this.pnlViewport.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.contextMenuSensor.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlViewport;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem agregarSensorToolTipMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ContextMenuStrip contextMenuSensor;
        private System.Windows.Forms.PictureBox picCanvas;
        private System.Windows.Forms.ToolStripMenuItem renombrarSensorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem eliminarSensorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem normalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem laserToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeTypeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toNormalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toLaserToolStripMenuItem;
    }
}