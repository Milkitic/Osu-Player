namespace Milkitic.OsuPlayer.Winforms
{
    partial class VolumeForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.tkHsVolume = new System.Windows.Forms.TrackBar();
            this.tkBgVolume = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tkHsVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tkBgVolume)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "音效音量";
            // 
            // tkHsVolume
            // 
            this.tkHsVolume.Location = new System.Drawing.Point(69, 63);
            this.tkHsVolume.Maximum = 100;
            this.tkHsVolume.Name = "tkHsVolume";
            this.tkHsVolume.Size = new System.Drawing.Size(317, 45);
            this.tkHsVolume.TabIndex = 6;
            this.tkHsVolume.TickFrequency = 5;
            this.tkHsVolume.Scroll += new System.EventHandler(this.TkHsVolume_Scroll);
            // 
            // tkBgVolume
            // 
            this.tkBgVolume.Location = new System.Drawing.Point(69, 12);
            this.tkBgVolume.Maximum = 100;
            this.tkBgVolume.Name = "tkBgVolume";
            this.tkBgVolume.Size = new System.Drawing.Size(317, 45);
            this.tkBgVolume.TabIndex = 7;
            this.tkBgVolume.TickFrequency = 5;
            this.tkBgVolume.Scroll += new System.EventHandler(this.TkBgVolume_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 9;
            this.label2.Text = "音乐音量";
            // 
            // VolumeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 104);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tkHsVolume);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tkBgVolume);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "VolumeForm";
            this.Text = "音效调节";
            this.Load += new System.EventHandler(this.VolumeForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tkHsVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tkBgVolume)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar tkHsVolume;
        private System.Windows.Forms.TrackBar tkBgVolume;
        private System.Windows.Forms.Label label2;
    }
}