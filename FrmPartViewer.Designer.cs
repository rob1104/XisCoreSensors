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
            this.lblClock = new XisCoreSensors.Controls.VerticalLabel();
            this.picCanvas = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.agregarSensorToolTipMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.normalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.laserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.contextMenuSensor = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.renombrarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toNormalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toLaserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.eliminarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clockTimer = new System.Windows.Forms.Timer(this.components);
            this.stopwatchTimer = new System.Windows.Forms.Timer(this.components);
            this.lblMessage = new XisCoreSensors.Controls.VerticalLabel();
            this.lblStopWatch = new XisCoreSensors.Controls.VerticalLabel();
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
            this.pnlViewport.Controls.Add(this.lblClock);
            this.pnlViewport.Controls.Add(this.picCanvas);
            this.pnlViewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlViewport.Location = new System.Drawing.Point(0, 0);
            this.pnlViewport.Name = "pnlViewport";
            this.pnlViewport.Size = new System.Drawing.Size(1824, 1002);
            this.pnlViewport.TabIndex = 0;
            this.pnlViewport.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlViewport_Paint);
            // 
            // lblClock
            // 
            this.lblClock.BackColor = System.Drawing.Color.DarkMagenta;
            this.lblClock.Font = new System.Drawing.Font("Verdana", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClock.ForeColor = System.Drawing.Color.GhostWhite;
            this.lblClock.Location = new System.Drawing.Point(304, 8);
            this.lblClock.Name = "lblClock";
            this.lblClock.Size = new System.Drawing.Size(62, 448);
            this.lblClock.TabIndex = 1;
            this.lblClock.Text = "00:00:00";
            this.lblClock.Paint += new System.Windows.Forms.PaintEventHandler(this.lblClock_Paint);
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
            this.normalToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.normalToolStripMenuItem.Text = "Normal";
            this.normalToolStripMenuItem.Click += new System.EventHandler(this.normalToolStripMenuItem_Click);
            // 
            // laserToolStripMenuItem
            // 
            this.laserToolStripMenuItem.Name = "laserToolStripMenuItem";
            this.laserToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
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
            this.contextMenuSensor.Size = new System.Drawing.Size(146, 76);
            this.contextMenuSensor.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuSensor_Opening);
            // 
            // renombrarSensorToolStripMenuItem
            // 
            this.renombrarSensorToolStripMenuItem.Name = "renombrarSensorToolStripMenuItem";
            this.renombrarSensorToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.renombrarSensorToolStripMenuItem.Text = "Edit ID...";
            this.renombrarSensorToolStripMenuItem.Click += new System.EventHandler(this.renombrarSensorToolStripMenuItem_Click);
            // 
            // changeTypeToolStripMenuItem
            // 
            this.changeTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toNormalToolStripMenuItem,
            this.toLaserToolStripMenuItem});
            this.changeTypeToolStripMenuItem.Name = "changeTypeToolStripMenuItem";
            this.changeTypeToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.changeTypeToolStripMenuItem.Text = "Change type";
            // 
            // toNormalToolStripMenuItem
            // 
            this.toNormalToolStripMenuItem.Enabled = false;
            this.toNormalToolStripMenuItem.Name = "toNormalToolStripMenuItem";
            this.toNormalToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.toNormalToolStripMenuItem.Text = "To Normal";
            this.toNormalToolStripMenuItem.Click += new System.EventHandler(this.toNormalToolStripMenuItem_Click);
            // 
            // toLaserToolStripMenuItem
            // 
            this.toLaserToolStripMenuItem.Enabled = false;
            this.toLaserToolStripMenuItem.Name = "toLaserToolStripMenuItem";
            this.toLaserToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.toLaserToolStripMenuItem.Text = "To Laser";
            this.toLaserToolStripMenuItem.Click += new System.EventHandler(this.toLaserToolStripMenuItem_Click);
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
            // clockTimer
            // 
            this.clockTimer.Enabled = true;
            this.clockTimer.Interval = 1000;
            this.clockTimer.Tick += new System.EventHandler(this.clockTimer_Tick);
            // 
            // stopwatchTimer
            // 
            this.stopwatchTimer.Tick += new System.EventHandler(this.stopwatchTimer_Tick);
            // 
            // lblMessage
            // 
            this.lblMessage.BackColor = System.Drawing.Color.Black;
            this.lblMessage.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblMessage.Font = new System.Drawing.Font("Verdana", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessage.ForeColor = System.Drawing.Color.Yellow;
            this.lblMessage.Location = new System.Drawing.Point(1701, 0);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(123, 1002);
            this.lblMessage.TabIndex = 1;
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStopWatch
            // 
            this.lblStopWatch.BackColor = System.Drawing.Color.Black;
            this.lblStopWatch.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblStopWatch.Font = new System.Drawing.Font("Verdana", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStopWatch.ForeColor = System.Drawing.Color.Yellow;
            this.lblStopWatch.Location = new System.Drawing.Point(0, 0);
            this.lblStopWatch.Name = "lblStopWatch";
            this.lblStopWatch.Size = new System.Drawing.Size(106, 1002);
            this.lblStopWatch.TabIndex = 2;
            this.lblStopWatch.Text = "00:00";
            // 
            // FrmPartViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1824, 1002);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblStopWatch);
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
        private Controls.VerticalLabel lblMessage;
        private System.Windows.Forms.Timer clockTimer;
        private Controls.VerticalLabel lblStopWatch;
        private System.Windows.Forms.Timer stopwatchTimer;
        private Controls.VerticalLabel lblClock;
    }
}