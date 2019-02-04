using Milkitic.OsuPlayer.Media.Storyboard.Util;
using OSharp.Storyboard;
using SharpDX;
using SharpDX.Direct2D1;
using System.IO;

namespace Milkitic.OsuPlayer.Media.Storyboard.Animation
{
    public sealed class AnimatedObject : StoryboardObject
    {
        public readonly int Times;
        public readonly float Delay;
        public readonly bool Loop;
        public int PrevIndex = -1;

        public AnimatedObject(AnimatedElement element, Timing timing, RenderTarget target, Size2F vSize) 
            : base(element, timing, target, vSize)
        {
            Times = element.FrameCount;
            Delay = element.FrameDelay;
            Loop = element.LoopType == LoopType.LoopForever;

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
