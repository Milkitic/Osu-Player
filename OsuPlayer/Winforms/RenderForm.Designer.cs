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
            this.components = new System.ComponentModel.Container();
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
            this.lblOffset = new System.Windows.Forms.Label();
            this.tkOffset = new System.Windows.Forms.TrackBar();
            this.btnControlNext = new System.Windows.Forms.Button();
            this.lbTime = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.diffList = new System.Windows.Forms.ListView();
            this.chCreator = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chVersion = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLength = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.playList = new System.Windows.Forms.ListView();
            this.chArtist = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cbSortType = new System.Windows.Forms.ComboBox();
            this.tbKeyword = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.tkProgress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tkVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tkOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnControlPlayPause
            // 
            this.btnControlPlayPause.Font = new System.Drawing.Font("微软雅黑", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnControlPlayPause.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnControlPlayPause.Location = new System.Drawing.Point(3, 11);
            this.btnControlPlayPause.Name = "btnControlPlayPause";
            this.btnControlPlayPause.Size = new System.Drawing.Size(65, 68);
            this.btnControlPlayPause.TabIndex = 1;
            this.btnControlPlayPause.Text = " ▶";
            this.btnControlPlayPause.UseVisualStyleBackColor = true;
            this.btnControlPlayPause.Click += new System.EventHandler(this.BtnControlPlayPause_Click);
            // 
            // btnControlStop
            // 
            this.btnControlStop.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnControlStop.Location = new System.Drawing.Point(74, 11);
            this.btnControlStop.Name = "btnControlStop";
            this.btnControlStop.Size = new System.Drawing.Size(64, 31);
            this.btnControlStop.TabIndex = 2;
            this.btnControlStop.Text = "■";
            this.btnControlStop.UseVisualStyleBackColor = true;
            this.btnControlStop.Click += new System.EventHandler(this.BtnControlStop_Click);
            // 
            // tkProgress
            // 
            this.tkProgress.Location = new System.Drawing.Point(3, 369);
            this.tkProgress.Maximum = 50;
            this.tkProgress.Name = "tkProgress";
            this.tkProgress.Size = new System.Drawing.Size(576, 45);
            this.tkProgress.TabIndex = 5;
            this.tkProgress.TickFrequency = 2147483647;
            this.tkProgress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseDown);
            this.tkProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseUp);
            // 
            // tkVolume
            // 
            this.tkVolume.Location = new System.Drawing.Point(609, 3);
            this.tkVolume.Maximum = 100;
            this.tkVolume.Name = "tkVolume";
            this.tkVolume.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tkVolume.Size = new System.Drawing.Size(45, 65);
            this.tkVolume.TabIndex = 11;
            this.tkVolume.TickFrequency = 5;
            this.tkVolume.Scroll += new System.EventHandler(this.TkVolume_Scroll);
            // 
            // pbBackground
            // 
            this.pbBackground.BackColor = System.Drawing.Color.Black;
            this.pbBackground.Location = new System.Drawing.Point(3, 3);
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
            this.statusStrip1.Location = new System.Drawing.Point(0, 508);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(963, 22);
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
            this.menuStrip1.Size = new System.Drawing.Size(963, 25);
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
            this.MenuFile_Open.Size = new System.Drawing.Size(127, 22);
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
            this.MenuPlay_Volume.Size = new System.Drawing.Size(148, 22);
            this.MenuPlay_Volume.Text = "音效调节(&E)...";
            this.MenuPlay_Volume.Click += new System.EventHandler(this.MenuPlay_Volume_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(612, 71);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 12);
            this.label1.TabIndex = 16;
            this.label1.Text = "◀)";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblOffset);
            this.panel1.Controls.Add(this.tkOffset);
            this.panel1.Controls.Add(this.btnControlNext);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnControlPlayPause);
            this.panel1.Controls.Add(this.btnControlStop);
            this.panel1.Controls.Add(this.tkVolume);
            this.panel1.Location = new System.Drawing.Point(3, 390);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 90);
            this.panel1.TabIndex = 17;
            // 
            // lblOffset
            // 
            this.lblOffset.AutoSize = true;
            this.lblOffset.Location = new System.Drawing.Point(161, 44);
            this.lblOffset.Name = "lblOffset";
            this.lblOffset.Size = new System.Drawing.Size(77, 12);
            this.lblOffset.TabIndex = 19;
            this.lblOffset.Text = "单曲音效偏移";
            // 
            // tkOffset
            // 
            this.tkOffset.Location = new System.Drawing.Point(144, 11);
            this.tkOffset.Maximum = 200;
            this.tkOffset.Minimum = -200;
            this.tkOffset.Name = "tkOffset";
            this.tkOffset.Size = new System.Drawing.Size(108, 45);
            this.tkOffset.TabIndex = 18;
            this.tkOffset.TickFrequency = 50;
            this.tkOffset.Scroll += new System.EventHandler(this.TkOffset_Scroll);
            // 
            // btnControlNext
            // 
            this.btnControlNext.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnControlNext.Location = new System.Drawing.Point(74, 48);
            this.btnControlNext.Name = "btnControlNext";
            this.btnControlNext.Size = new System.Drawing.Size(64, 31);
            this.btnControlNext.TabIndex = 17;
            this.btnControlNext.Text = "▶|";
            this.btnControlNext.UseVisualStyleBackColor = true;
            this.btnControlNext.Click += new System.EventHandler(this.BtnControlNext_Click);
            // 
            // lbTime
            // 
            this.lbTime.AutoSize = true;
            this.lbTime.Location = new System.Drawing.Point(574, 373);
            this.lbTime.Name = "lbTime";
            this.lbTime.Size = new System.Drawing.Size(71, 12);
            this.lbTime.TabIndex = 18;
            this.lbTime.Text = "00:00/00:00";
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 25);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.pbBackground);
            this.splitContainer.Panel1.Controls.Add(this.lbTime);
            this.splitContainer.Panel1.Controls.Add(this.panel1);
            this.splitContainer.Panel1.Controls.Add(this.tkProgress);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.label2);
            this.splitContainer.Panel2.Controls.Add(this.diffList);
            this.splitContainer.Panel2.Controls.Add(this.playList);
            this.splitContainer.Panel2.Controls.Add(this.cbSortType);
            this.splitContainer.Panel2.Controls.Add(this.tbKeyword);
            this.splitContainer.Size = new System.Drawing.Size(963, 483);
            this.splitContainer.SplitterDistance = 647;
            this.splitContainer.TabIndex = 19;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1, 461);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "搜索：";
            // 
            // diffList
            // 
            this.diffList.AutoArrange = false;
            this.diffList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chCreator,
            this.chVersion,
            this.chLength});
            this.diffList.FullRowSelect = true;
            this.diffList.GridLines = true;
            this.diffList.Location = new System.Drawing.Point(3, 3);
            this.diffList.MultiSelect = false;
            this.diffList.Name = "diffList";
            this.diffList.ShowItemToolTips = true;
            this.diffList.Size = new System.Drawing.Size(304, 169);
            this.diffList.TabIndex = 5;
            this.diffList.UseCompatibleStateImageBehavior = false;
            this.diffList.View = System.Windows.Forms.View.Details;
            this.diffList.DoubleClick += new System.EventHandler(this.DiffList_DoubleClick);
            // 
            // chCreator
            // 
            this.chCreator.Text = "作者";
            this.chCreator.Width = 100;
            // 
            // chVersion
            // 
            this.chVersion.Text = "难度名";
            this.chVersion.Width = 100;
            // 
            // chLength
            // 
            this.chLength.Text = "长度";
            this.chLength.Width = 100;
            // 
            // playList
            // 
            this.playList.AutoArrange = false;
            this.playList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chArtist,
            this.chTitle});
            this.playList.FullRowSelect = true;
            this.playList.GridLines = true;
            this.playList.Location = new System.Drawing.Point(3, 177);
            this.playList.MultiSelect = false;
            this.playList.Name = "playList";
            this.playList.ShowItemToolTips = true;
            this.playList.Size = new System.Drawing.Size(304, 275);
            this.playList.TabIndex = 4;
            this.playList.UseCompatibleStateImageBehavior = false;
            this.playList.View = System.Windows.Forms.View.Details;
            this.playList.SelectedIndexChanged += new System.EventHandler(this.PlayList_SelectedIndexChanged);
            // 
            // chArtist
            // 
            this.chArtist.Text = "艺术家";
            this.chArtist.Width = 112;
            // 
            // chTitle
            // 
            this.chTitle.Text = "标题";
            this.chTitle.Width = 162;
            // 
            // cbSortType
            // 
            this.cbSortType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSortType.FormattingEnabled = true;
            this.cbSortType.Location = new System.Drawing.Point(222, 458);
            this.cbSortType.Name = "cbSortType";
            this.cbSortType.Size = new System.Drawing.Size(85, 20);
            this.cbSortType.TabIndex = 3;
            this.cbSortType.SelectedIndexChanged += new System.EventHandler(this.CbSortType_SelectedIndexChanged);
            // 
            // tbKeyword
            // 
            this.tbKeyword.Location = new System.Drawing.Point(48, 458);
            this.tbKeyword.Name = "tbKeyword";
            this.tbKeyword.Size = new System.Drawing.Size(168, 21);
            this.tbKeyword.TabIndex = 2;
            this.tbKeyword.TextChanged += new System.EventHandler(this.TbKeyword_TextChanged);
            // 
            // RenderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(963, 530);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
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
            ((System.ComponentModel.ISupportInitialize)(this.tkOffset)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
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
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TextBox tbKeyword;
        private System.Windows.Forms.ComboBox cbSortType;
        private System.Windows.Forms.ListView playList;
        private System.Windows.Forms.ColumnHeader chArtist;
        private System.Windows.Forms.ColumnHeader chTitle;
        private System.Windows.Forms.ListView diffList;
        private System.Windows.Forms.ColumnHeader chCreator;
        private System.Windows.Forms.ColumnHeader chVersion;
        private System.Windows.Forms.ColumnHeader chLength;
        private System.Windows.Forms.Button btnControlNext;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblOffset;
        private System.Windows.Forms.TrackBar tkOffset;
        private System.Windows.Forms.ToolTip toolTip;
    }
}

