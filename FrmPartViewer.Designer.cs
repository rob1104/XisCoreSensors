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
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.agregarSensorToolTipMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.contextMenuSensor = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.eliminarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.picCanvas = new System.Windows.Forms.PictureBox();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.renombrarSensorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlViewport.SuspendLayout();
            this.contextMenu.SuspendLayout();
            this.contextMenuSensor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).BeginInit();
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
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.agregarSensorToolTipMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(135, 26);
            // 
            // agregarSensorToolTipMenuItem
            // 
            this.agregarSensorToolTipMenuItem.Name = "agregarSensorToolTipMenuItem";
            this.agregarSensorToolTipMenuItem.Size = new System.Drawing.Size(134, 22);
            this.agregarSensorToolTipMenuItem.Text = "Add Sensor";
            this.agregarSensorToolTipMenuItem.Click += new System.EventHandler(this.agregarSensorToolTipMenuItem_Click);
            // 
            // contextMenuSensor
            // 
            this.contextMenuSensor.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.renombrarSensorToolStripMenuItem,
            this.toolStripMenuItem1,
            this.eliminarSensorToolStripMenuItem});
            this.contextMenuSensor.Name = "contextMenuSensor";
            this.contextMenuSensor.Size = new System.Drawing.Size(181, 76);
            // 
            // eliminarSensorToolStripMenuItem
            // 
            this.eliminarSensorToolStripMenuItem.Name = "eliminarSensorToolStripMenuItem";
            this.eliminarSensorToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.eliminarSensorToolStripMenuItem.Text = "Delete Sensor";
            this.eliminarSensorToolStripMenuItem.Click += new System.EventHandler(this.eliminarSensorToolStripMenuItem_Click);
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
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
            // 
            // renombrarSensorToolStripMenuItem
            // 
            this.renombrarSensorToolStripMenuItem.Name = "renombrarSensorToolStripMenuItem";
            this.renombrarSensorToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.renombrarSensorToolStripMenuItem.Text = "Edit ID...";
            this.renombrarSensorToolStripMenuItem.Click += new System.EventHandler(this.renombrarSensorToolStripMenuItem_Click);
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
            this.Load += new System.EventHandler(this.FrmPartViewer_Load);
            this.Shown += new System.EventHandler(this.FrmPartViewer_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmPartViewer_KeyDown);
            this.pnlViewport.ResumeLayout(false);
            this.contextMenu.ResumeLayout(false);
            this.contextMenuSensor.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).EndInit();
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
    }
}