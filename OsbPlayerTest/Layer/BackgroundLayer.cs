using LibOsb;
using OsbPlayerTest.DxAnimation;
using OsbPlayerTest.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;
namespace OsbPlayerTest.Layer
{
    internal class BackgroundLayer : DxLayer
    {
        private readonly ElementObject[] _objs;
        private readonly Stopwatch _watch = new Stopwatch();
        private bool _isFirstFrame;
        private IEnumerable<(int index, Element elment)> _ok;

        public BackgroundLayer(D2D.RenderTarget renderTarget) : base(renderTarget)
        {

        }

        public BackgroundLayer(D2D.RenderTarget renderTarget, ElementGroup elementGroup) : base(renderTarget)
        {
            elementGroup.Expand();
            (int index, Element elment)[] elements = new (int, Element)[elementGroup.ElementList.Count];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = (i, elementGroup.ElementList[i]);

            _objs = new ElementObject[elements.Length];
            for (var i = 0; i < elements.Length; i++)
            {
                var item = elements[i];
                if (item.elment.Type == LibOsb.Enums.ElementType.Animation)
                    continue;

                _objs[i] = new ElementObject(RenderTarget, item.elment, _watch);
            }

            _watch.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    _ok = elements.Where(k =>
                        _watch.ElapsedMilliseconds > k.elment.MinTime &&
                        _watch.ElapsedMilliseconds < k.elment.MaxTime + 2000 &&
                        !k.elment.FadeoutList.InRange((int)_watch.ElapsedMilliseconds));
                    Thread.Sleep(1500);
                }
            });
        }

        public override void Measure()
        {

        }

        public override void Draw()
        {
            foreach (var (i, element) in _ok)
            {
                if (_objs[i] == null)
                    continue;
                _objs[i].StartDraw();
                foreach (var sb in element.ScaleList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start,
                        sb.Start, sb.End, sb.End);
                foreach (var sb in element.RotateList)
                    _objs[i].Rotate(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                foreach (var sb in element.MoveList)
                    _objs[i].Move(sb.Easing, (int)sb.StartTime, (int)sb.EndTime,
                        new System.Drawing.PointF(sb.Start.x + 107, sb.Start.y),
                        new System.Drawing.PointF(sb.End.x + 107, sb.End.y));
                foreach (var sb in element.FadeList)
                    _objs[i].Fade(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                foreach (var sb in element.VectorList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start.x,
                        sb.Start.y, sb.End.x, sb.End.y);
                foreach (var sb in element.MoveXList)
                    _objs[i].MoveX(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start + 107, sb.End + 107);
                foreach (var sb in element.MoveYList)
                    _objs[i].MoveY(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                _objs[i].EndDraw();

            }

            //Console.WriteLine(_objs[1].Offset);
            _isFirstFrame = true;
        }

        public override void Dispose()
        {

        }
    }
}
