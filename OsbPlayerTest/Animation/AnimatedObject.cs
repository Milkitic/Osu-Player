using Milkitic.OsbLib;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsbPlayerTest.Animation
{
    public sealed class AnimatedObject : StoryboardObject
    {
        public readonly int Times;
        public readonly float Delay;
        public readonly bool Loop;
        public int PrevIndex = -1;

        public AnimatedObject(AnimatedElement element, Timing timing, RenderTarget target, Size2F vSize) : base(element, timing, target, vSize)
        {
            Times = element.FrameCount;
            Delay = element.FrameDelay;
            Loop = element.LoopType == Milkitic.OsbLib.Enums.LoopType.LoopForever;

            TextureList = new Bitmap[Times];
            for (var i = 0; i < Times; i++)
            {
                var path = Path.Combine(Program.Manager.Directory, element.ImagePath);
                var fullPath = path.Insert(path.Length - 4, i.ToString());
                if (!File.Exists(fullPath)) continue;
                TextureList[i] = TextureLoader.LoadBitmap(target, fullPath);
            }

            Texture = TextureList[0];

            SetDefaultValue();
            SetMinMax();
        }
    }
}
