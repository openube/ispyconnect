namespace iSpyApplication
{
    partial class CameraPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CameraPanel));
            this._pnlCameras = new iSpyApplication.Controls.LayoutPanel();
            this.ctxtMainForm = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._addCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._addMicrophoneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._addFloorPlanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._remoteCommandsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._applyScheduleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.opacityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.opacityToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.opacityToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.opacityToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.layoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLayoutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.resetLayoutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.displayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullScreenToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMenuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mediaPaneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pTZControllerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewControllerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alwaysOnTopToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxtMainForm.SuspendLayout();
            this.SuspendLayout();
            // 
            // _pnlCameras
            // 
            this._pnlCameras.AutoScroll = true;
            this._pnlCameras.AutoSize = true;
            this._pnlCameras.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._pnlCameras.BackColor = System.Drawing.Color.DimGray;
            this._pnlCameras.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this._pnlCameras.ContextMenuStrip = this.ctxtMainForm;
            this._pnlCameras.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnlCameras.Location = new System.Drawing.Point(0, 0);
            this._pnlCameras.Margin = new System.Windows.Forms.Padding(0);
            this._pnlCameras.Name = "_pnlCameras";
            this._pnlCameras.Size = new System.Drawing.Size(284, 261);
            this._pnlCameras.TabIndex = 19;
            this._pnlCameras.Scroll += new System.Windows.Forms.ScrollEventHandler(this.layoutPanel1_Scroll);
            // 
            // ctxtMainForm
            // 
            this.ctxtMainForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._addCameraToolStripMenuItem,
            this._addMicrophoneToolStripMenuItem,
            this._addFloorPlanToolStripMenuItem,
            this._remoteCommandsToolStripMenuItem,
            this._settingsToolStripMenuItem,
            this._applyScheduleToolStripMenuItem,
            this.opacityToolStripMenuItem,
            this.layoutToolStripMenuItem,
            this.displayToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.ctxtMainForm.Name = "_ctxtMainForm";
            this.ctxtMainForm.Size = new System.Drawing.Size(181, 224);
            // 
            // _addCameraToolStripMenuItem
            // 
            this._addCameraToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_addCameraToolStripMenuItem.Image")));
            this._addCameraToolStripMenuItem.Name = "_addCameraToolStripMenuItem";
            this._addCameraToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._addCameraToolStripMenuItem.Text = "Add &Camera";
            // 
            // _addMicrophoneToolStripMenuItem
            // 
            this._addMicrophoneToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_addMicrophoneToolStripMenuItem.Image")));
            this._addMicrophoneToolStripMenuItem.Name = "_addMicrophoneToolStripMenuItem";
            this._addMicrophoneToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._addMicrophoneToolStripMenuItem.Text = "Add &Microphone";
            // 
            // _addFloorPlanToolStripMenuItem
            // 
            this._addFloorPlanToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_addFloorPlanToolStripMenuItem.Image")));
            this._addFloorPlanToolStripMenuItem.Name = "_addFloorPlanToolStripMenuItem";
            this._addFloorPlanToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._addFloorPlanToolStripMenuItem.Text = "Add Floor &Plan";
            // 
            // _remoteCommandsToolStripMenuItem
            // 
            this._remoteCommandsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_remoteCommandsToolStripMenuItem.Image")));
            this._remoteCommandsToolStripMenuItem.Name = "_remoteCommandsToolStripMenuItem";
            this._remoteCommandsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._remoteCommandsToolStripMenuItem.Text = "Remote Commands";
            // 
            // _settingsToolStripMenuItem
            // 
            this._settingsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_settingsToolStripMenuItem.Image")));
            this._settingsToolStripMenuItem.Name = "_settingsToolStripMenuItem";
            this._settingsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._settingsToolStripMenuItem.Text = "&Settings";
            // 
            // _applyScheduleToolStripMenuItem
            // 
            this._applyScheduleToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("_applyScheduleToolStripMenuItem.Image")));
            this._applyScheduleToolStripMenuItem.Name = "_applyScheduleToolStripMenuItem";
            this._applyScheduleToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this._applyScheduleToolStripMenuItem.Text = "Apply Schedule";
            // 
            // opacityToolStripMenuItem
            // 
            this.opacityToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.opacityToolStripMenuItem1,
            this.opacityToolStripMenuItem2,
            this.opacityToolStripMenuItem3});
            this.opacityToolStripMenuItem.Name = "opacityToolStripMenuItem";
            this.opacityToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.opacityToolStripMenuItem.Text = "Opacity";
            // 
            // opacityToolStripMenuItem1
            // 
            this.opacityToolStripMenuItem1.Name = "opacityToolStripMenuItem1";
            this.opacityToolStripMenuItem1.Size = new System.Drawing.Size(146, 22);
            this.opacityToolStripMenuItem1.Text = "10% Opacity";
            // 
            // opacityToolStripMenuItem2
            // 
            this.opacityToolStripMenuItem2.Name = "opacityToolStripMenuItem2";
            this.opacityToolStripMenuItem2.Size = new System.Drawing.Size(146, 22);
            this.opacityToolStripMenuItem2.Text = "30% Opacity";
            // 
            // opacityToolStripMenuItem3
            // 
            this.opacityToolStripMenuItem3.Name = "opacityToolStripMenuItem3";
            this.opacityToolStripMenuItem3.Size = new System.Drawing.Size(146, 22);
            this.opacityToolStripMenuItem3.Text = "100% Opacity";
            // 
            // layoutToolStripMenuItem
            // 
            this.layoutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoLayoutToolStripMenuItem,
            this.saveLayoutToolStripMenuItem1,
            this.resetLayoutToolStripMenuItem1});
            this.layoutToolStripMenuItem.Name = "layoutToolStripMenuItem";
            this.layoutToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.layoutToolStripMenuItem.Text = "Layout";
            // 
            // autoLayoutToolStripMenuItem
            // 
            this.autoLayoutToolStripMenuItem.Name = "autoLayoutToolStripMenuItem";
            this.autoLayoutToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.autoLayoutToolStripMenuItem.Text = "Auto Layout";
            // 
            // saveLayoutToolStripMenuItem1
            // 
            this.saveLayoutToolStripMenuItem1.Name = "saveLayoutToolStripMenuItem1";
            this.saveLayoutToolStripMenuItem1.Size = new System.Drawing.Size(141, 22);
            this.saveLayoutToolStripMenuItem1.Text = "Save Layout";
            // 
            // resetLayoutToolStripMenuItem1
            // 
            this.resetLayoutToolStripMenuItem1.Name = "resetLayoutToolStripMenuItem1";
            this.resetLayoutToolStripMenuItem1.Size = new System.Drawing.Size(141, 22);
            this.resetLayoutToolStripMenuItem1.Text = "Reset Layout";
            // 
            // displayToolStripMenuItem
            // 
            this.displayToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fullScreenToolStripMenuItem1,
            this.statusBarToolStripMenuItem,
            this.fileMenuToolStripMenuItem,
            this.toolStripToolStripMenuItem,
            this.mediaPaneToolStripMenuItem,
            this.pTZControllerToolStripMenuItem,
            this.viewControllerToolStripMenuItem,
            this.alwaysOnTopToolStripMenuItem1});
            this.displayToolStripMenuItem.Name = "displayToolStripMenuItem";
            this.displayToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.displayToolStripMenuItem.Text = "Display";
            // 
            // fullScreenToolStripMenuItem1
            // 
            this.fullScreenToolStripMenuItem1.Checked = true;
            this.fullScreenToolStripMenuItem1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.fullScreenToolStripMenuItem1.Name = "fullScreenToolStripMenuItem1";
            this.fullScreenToolStripMenuItem1.Size = new System.Drawing.Size(155, 22);
            this.fullScreenToolStripMenuItem1.Text = "Full Screen";
            // 
            // statusBarToolStripMenuItem
            // 
            this.statusBarToolStripMenuItem.Checked = true;
            this.statusBarToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.statusBarToolStripMenuItem.Name = "statusBarToolStripMenuItem";
            this.statusBarToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.statusBarToolStripMenuItem.Text = "Status Bar";
            // 
            // fileMenuToolStripMenuItem
            // 
            this.fileMenuToolStripMenuItem.Checked = true;
            this.fileMenuToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.fileMenuToolStripMenuItem.Name = "fileMenuToolStripMenuItem";
            this.fileMenuToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.fileMenuToolStripMenuItem.Text = "File Menu";
            // 
            // toolStripToolStripMenuItem
            // 
            this.toolStripToolStripMenuItem.Checked = true;
            this.toolStripToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripToolStripMenuItem.Name = "toolStripToolStripMenuItem";
            this.toolStripToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.toolStripToolStripMenuItem.Text = "Tool Strip";
            // 
            // mediaPaneToolStripMenuItem
            // 
            this.mediaPaneToolStripMenuItem.Checked = true;
            this.mediaPaneToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.mediaPaneToolStripMenuItem.Name = "mediaPaneToolStripMenuItem";
            this.mediaPaneToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.mediaPaneToolStripMenuItem.Text = "Media Pane";
            // 
            // pTZControllerToolStripMenuItem
            // 
            this.pTZControllerToolStripMenuItem.Checked = true;
            this.pTZControllerToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pTZControllerToolStripMenuItem.Name = "pTZControllerToolStripMenuItem";
            this.pTZControllerToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.pTZControllerToolStripMenuItem.Text = "PTZ Controller";
            // 
            // viewControllerToolStripMenuItem
            // 
            this.viewControllerToolStripMenuItem.Checked = true;
            this.viewControllerToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.viewControllerToolStripMenuItem.Name = "viewControllerToolStripMenuItem";
            this.viewControllerToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.viewControllerToolStripMenuItem.Text = "View Controller";
            // 
            // alwaysOnTopToolStripMenuItem1
            // 
            this.alwaysOnTopToolStripMenuItem1.Checked = true;
            this.alwaysOnTopToolStripMenuItem1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.alwaysOnTopToolStripMenuItem1.Name = "alwaysOnTopToolStripMenuItem1";
            this.alwaysOnTopToolStripMenuItem1.Size = new System.Drawing.Size(155, 22);
            this.alwaysOnTopToolStripMenuItem1.Text = "Always on Top";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // CameraPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this._pnlCameras);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CameraPanel";
            this.Text = "CameraPanel";
            this.Load += new System.EventHandler(this.CameraPanel_Load);
            this.ctxtMainForm.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip ctxtMainForm;
        private System.Windows.Forms.ToolStripMenuItem _addCameraToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _addMicrophoneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _addFloorPlanToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _remoteCommandsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _applyScheduleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem opacityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem opacityToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem opacityToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem opacityToolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem layoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem resetLayoutToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem displayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fullScreenToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem statusBarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileMenuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mediaPaneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pTZControllerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewControllerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        public Controls.LayoutPanel _pnlCameras;
    }
}