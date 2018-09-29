using System.IO;
using Milkitic.OsbLib;
using Milkitic.OsuPlayer.Media.Storyboard.Util;
using SharpDX;
using SharpDX.Direct2D1;

namespace Milkitic.OsuPlayer.Media.Storyboard.Animation
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
                var path = Path.Combine(App.StoryboardProvider.Directory, element.ImagePath);
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
