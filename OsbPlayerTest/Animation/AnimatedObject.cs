using Milkitic.OsbLib;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
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

        public AnimatedObject(AnimatedElement element, Timing timing, RenderTarget target) : base(element, timing, target)
        {
            Times = element.FrameCount;
            Delay = element.FrameDelay;
            Loop = element.LoopType == Milkitic.OsbLib.Enums.LoopType.LoopForever;

            TextureList = new Bitmap[Times];
            for (var i = 0; i < Times; i++)
            {
                var path = Path.Combine(Program.Fi.Directory.FullName, element.ImagePath);
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
