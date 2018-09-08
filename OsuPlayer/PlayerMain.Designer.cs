namespace Milkitic.OsuPlayer
{
    partial class PlayerMain
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
            this.btnLoadSingle = new System.Windows.Forms.Button();
            this.btnControlPlayPause = new System.Windows.Forms.Button();
            this.btnControlStop = new System.Windows.Forms.Button();
            this.tkProgress = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.tkProgress)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoadSingle
            // 
            this.btnLoadSingle.Location = new System.Drawing.Point(280, 63);
            this.btnLoadSingle.Name = "btnLoadSingle";
            this.btnLoadSingle.Size = new System.Drawing.Size(108, 23);
            this.btnLoadSingle.TabIndex = 0;
            this.btnLoadSingle.Text = "Load a file...";
            this.btnLoadSingle.UseVisualStyleBackColor = true;
            this.btnLoadSingle.Click += new System.EventHandler(this.BtnLoadSingle_Click);
            // 
            // btnControlPlayPause
            // 
            this.btnControlPlayPause.Location = new System.Drawing.Point(12, 63);
            this.btnControlPlayPause.Name = "btnControlPlayPause";
            this.btnControlPlayPause.Size = new System.Drawing.Size(75, 23);
            this.btnControlPlayPause.TabIndex = 1;
            this.btnControlPlayPause.Text = "Play";
            this.btnControlPlayPause.UseVisualStyleBackColor = true;
            this.btnControlPlayPause.Click += new System.EventHandler(this.BtnControlPlayPause_Click);
            // 
            // btnControlStop
            // 
            this.btnControlStop.Location = new System.Drawing.Point(95, 63);
            this.btnControlStop.Name = "btnControlStop";
            this.btnControlStop.Size = new System.Drawing.Size(75, 23);
            this.btnControlStop.TabIndex = 2;
            this.btnControlStop.Text = "Stop";
            this.btnControlStop.UseVisualStyleBackColor = true;
            this.btnControlStop.Click += new System.EventHandler(this.BtnControlStop_Click);
            // 
            // tkProgress
            // 
            this.tkProgress.Location = new System.Drawing.Point(12, 12);
            this.tkProgress.Name = "tkProgress";
            this.tkProgress.Size = new System.Drawing.Size(376, 45);
            this.tkProgress.TabIndex = 5;
            this.tkProgress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseDown);
            this.tkProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TkProgress_MouseUp);
            // 
            // PlayerMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 101);
            this.Controls.Add(this.tkProgress);
            this.Controls.Add(this.btnControlStop);
            this.Controls.Add(this.btnControlPlayPause);
            this.Controls.Add(this.btnLoadSingle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "PlayerMain";
            this.Text = "PlayerMain";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tkProgress)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoadSingle;
        private System.Windows.Forms.Button btnControlPlayPause;
        private System.Windows.Forms.Button btnControlStop;
        private System.Windows.Forms.TrackBar tkProgress;
    }
}

