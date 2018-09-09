using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    public partial class VolumeForm : Form
    {
        public VolumeForm()
        {
            InitializeComponent();
        }

        private void VolumeForm_Load(object sender, EventArgs e)
        {
            tkBgVolume.Value = (int)(Core.Config.Volume.Music * 100);
            tkHsVolume.Value = (int)(Core.Config.Volume.Hitsound * 100);
        }

        private void TkHsVolume_Scroll(object sender, EventArgs e) => Core.Config.Volume.Hitsound = tkHsVolume.Value / 100f;
        private void TkBgVolume_Scroll(object sender, EventArgs e) => Core.Config.Volume.Music = tkBgVolume.Value / 100f;
    }
}
