using Milkitic.OsbLib;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsbPlayerTest.DxAnimation
{
    class AnimatedElementObject : ElementObject
    {
        private readonly int _times;
        private readonly float _delay;
        private readonly bool _loop;
        private readonly Stopwatch _aniSw = new Stopwatch();

        private int _tmpIndex = -1;
        public AnimatedElementObject(RenderTarget target, AnimatedElement element, bool enableLog = false) : base(target, element, enableLog)
        {
        }

        public AnimatedElementObject(RenderTarget target, AnimatedElement element, BackgroundLayer.Timing timing, bool enableLog = false)
            : base(target, element, timing, enableLog)
        {
            _times = element.FrameCount;
            _delay = element.FrameDelay;
            _loop = element.LoopType == Milkitic.OsbLib.Enums.LoopType.LoopForever;

            Bitmaps = new Bitmap[_times];
            for (var i = 0; i < _times; i++)
            {
                var path = Path.Combine(Program.Fi.Directory.FullName, element.ImagePath);
                var fullPath = path.Insert(path.Length - 4, i.ToString());
                if (!File.Exists(fullPath)) continue;
                Bitmaps[i] = Loader.LoadBitmap(target, fullPath);
            }

            CurrentBitmap = Bitmaps[0];

            SetDefaultValue();
            SetMinMax();
        }

        public override void EndDraw()
        {
            if (Timing.Offset >= MinTime && Timing.Offset <= MaxTime)
            {
                int imgIndex;
                if (_loop)
                    imgIndex = (int)((Timing.Offset - MinTime) / _delay % _times);
                else
                {
                    imgIndex = (int)((Timing.Offset - MinTime) / _delay);
                    if (imgIndex >= _times)
                        imgIndex = _times - 1;
                }

                if (imgIndex != _tmpIndex)
                {
                    _tmpIndex = imgIndex;
                    CurrentBitmap = Bitmaps[imgIndex];
                }
            }

            base.EndDraw();
        }
    }
}
