using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Media
{

    public enum PlayerStatus
    {
        NotInitialized, Ready, Playing, Paused, Stopped, Finished
    }

    public enum PlayerMode
    {
        Normal, Random, Loop, LoopRandom, Single, SingleLoop,
    }


}
