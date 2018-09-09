namespace Milkitic.OsuPlayer.Winforms
{
    partial class RenderForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnControlPlayPause = new System.Windows.Forms.Button();
            this.btnControlStop = new System.Windows.Forms.Button();
            this.tkProgress = new System.Windows.Forms.TrackBar();
            this.tkVolume = new System.Windows.Forms.TrackBar();
            this.pbBackground = new System.Windows.Forms.PictureBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tssLblMeta = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuFile_Open = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuPlay = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuPlay_Volume = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbTime = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tkProgress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tkVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnControlPlayPause
            // 
            this.btnControlPlayPause.Location = new System.Drawing.Point(3, 11);
            this.btnControlPlayPause.Name = "btnControlPlayPause";
            this.btnControlPlayPause.Size = new System.Drawing.Size(41, 23);
            this.btnControlPlayPause.TabIndex = 1;
            this.btnControlPlayPause.Text = "▶";
            this.btnControlPlayPause.UseVisualStyleBackColor = true;
            this.btnControlPlayPause.Click += new System.EventHandler(this.BtnControlPlayPause_Click);
            // 
            // btnControlStop
            // 
            this.btnControlStop.Location = new System.Drawing.Point(50, 11);
            this.btnControlStop.Name = "btnControlStop";
            this.btnControlStop.Size = new System.Drawing.Size(41, 23);
            this.btnControlStop.TabIndex = 2;
            this.btnControlStop.Text = "■";
            this.btnControlStop.UseVisualStyleBackColor = true;
            this.btnControlStop.Click += new System.EventHandler(this.BtnControlStop_Click);
            // 
            // tkProgress
            // 
            this.tkProgress.Location = new System.Drawing.Point(12, 394);
            this.tkProgress.Maximum = 50;
            this.tkProgress.Name = "tkProgress";
            this.tkProgress.Size = new System.Drawing.Size(579, 45);
            this.tkProgress.TabIndex = 5;
            this.tkProgress.TickFrequency = 2147483647;
            this.tkProgress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseDown);
            this.tkProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseUp);
            // 
            // tkVolume
            // 
            this.tkVolume.Location = new System.Drawing.Point(529, 11);
            this.tkVolume.Maximum = 100;
            this.tkVolume.Name = "tkVolume";
            this.tkVolume.Size = new System.Drawing.Size(108, 45);
            this.tkVolume.TabIndex = 11;
            this.tkVolume.TickFrequency = 5;
            this.tkVolume.Scroll += new System.EventHandler(this.TkVolume_Scroll);
            // 
            // pbBackground
            // 
            this.pbBackground.BackColor = System.Drawing.Color.Black;
            this.pbBackground.Location = new System.Drawing.Point(12, 28);
            this.pbBackground.Name = "pbBackground";
            this.pbBackground.Size = new System.Drawing.Size(640, 360);
            this.pbBackground.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbBackground.TabIndex = 13;
            this.pbBackground.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tssLblMeta});
            this.statusStrip1.Location = new System.Drawing.Point(0, 457);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(666, 22);
            this.statusStrip1.TabIndex = 14;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tssLblMeta
            // 
            this.tssLblMeta.Name = "tssLblMeta";
            this.tssLblMeta.Size = new System.Drawing.Size(17, 17);
            this.tssLblMeta.Text = "...";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFile,
            this.MenuPlay});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(666, 25);
            this.menuStrip1.TabIndex = 15;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuFile
            // 
            this.MenuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFile_Open});
            this.MenuFile.Name = "MenuFile";
            this.MenuFile.Size = new System.Drawing.Size(58, 21);
            this.MenuFile.Text = "文件(&F)";
            // 
            // MenuFile_Open
            // 
            this.MenuFile_Open.Name = "MenuFile_Open";
            this.MenuFile_Open.Size = new System.Drawing.Size(180, 22);
            this.MenuFile_Open.Text = "打开(&O)...";
            this.MenuFile_Open.Click += new System.EventHandler(this.MenuFile_Open_Click);
            // 
            // MenuPlay
            // 
            this.MenuPlay.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuPlay_Volume});
            this.MenuPlay.Name = "MenuPlay";
            this.MenuPlay.Size = new System.Drawing.Size(59, 21);
            this.MenuPlay.Text = "播放(&P)";
            // 
            // MenuPlay_Volume
            // 
            this.MenuPlay_Volume.Name = "MenuPlay_Volume";
            this.MenuPlay_Volume.Size = new System.Drawing.Size(180, 22);
            this.MenuPlay_Volume.Text = "音效调节(&E)...";
            this.MenuPlay_Volume.Click += new System.EventHandler(this.MenuPlay_Volume_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(504, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 12);
            this.label1.TabIndex = 16;
            this.label1.Text = "◀)";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnControlPlayPause);
            this.panel1.Controls.Add(this.btnControlStop);
            this.panel1.Controls.Add(this.tkVolume);
            this.panel1.Location = new System.Drawing.Point(12, 415);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 100);
            this.panel1.TabIndex = 17;
            // 
            // lbTime
            // 
            this.lbTime.AutoSize = true;
            this.lbTime.Location = new System.Drawing.Point(587, 398);
            this.lbTime.Name = "lbTime";
            this.lbTime.Size = new System.Drawing.Size(71, 12);
            this.lbTime.TabIndex = 18;
            this.lbTime.Text = "00:00/00:00";
            // 
            // RenderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(666, 479);
            this.Controls.Add(this.lbTime);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.pbBackground);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tkProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "RenderForm";
            this.Text = "Osu Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.tkProgress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tkVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnControlPlayPause;
        private System.Windows.Forms.Button btnControlStop;
        private System.Windows.Forms.TrackBar tkProgress;
        private System.Windows.Forms.TrackBar tkVolume;
        private System.Windows.Forms.PictureBox pbBackground;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MenuFile;
        private System.Windows.Forms.ToolStripMenuItem MenuFile_Open;
        private System.Windows.Forms.ToolStripStatusLabel tssLblMeta;
        private System.Windows.Forms.ToolStripMenuItem MenuPlay;
        private System.Windows.Forms.ToolStripMenuItem MenuPlay_Volume;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lbTime;
    }
}

