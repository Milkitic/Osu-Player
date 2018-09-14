using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Extension;
using Milkitic.OsbLib.Models;
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
        private struct RenderThings
        {
            public int Index;
            public Element Elment;
            public List<Event> Events;

            public RenderThings(int index, Element elment, List<Event> events)
            {
                Index = index;
                Elment = elment;
                Events = events;
            }
        }

        private readonly ElementObject[] _objs;
        private readonly Stopwatch _watch = new Stopwatch();
        private bool _isFirstFrame;
        private IEnumerable<RenderThings> _ok;

        public BackgroundLayer(D2D.RenderTarget renderTarget, ElementGroup elementGroup) : base(renderTarget)
        {
            elementGroup.Expand();
            RenderThings[] elements = new RenderThings[elementGroup.ElementList.Count];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new RenderThings(i, elementGroup.ElementList[i], elementGroup.ElementList[i].EventList);

            _objs = new ElementObject[elements.Length];
            for (var i = 0; i < elements.Length; i++)
            {
                var item = elements[i];
                if (item.Elment.Type == ElementType.Animation)
                    continue;

                _objs[i] = new ElementObject(RenderTarget, item.Elment, _watch);
            }

            _watch.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    _ok = elements.Where(k =>
                        _watch.ElapsedMilliseconds > k.Elment.MinTime &&
                        _watch.ElapsedMilliseconds < k.Elment.MaxTime + 2000 &&
                        !k.Elment.FadeoutList.InRange((int)_watch.ElapsedMilliseconds));
                    foreach (var item in _ok) { }
                    Thread.Sleep(1500);
                }
            });
        }

        public override void Measure()
        {

        }

        public override void Draw()
        {
            foreach (var render in _ok)
            {
                int i = render.Index;
                var element = render.Elment;

                if (_objs[i] == null)
                    continue;
                _objs[i].StartDraw();
                foreach (var sb in element.ScaleList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.S1,
                        sb.S1, sb.S2, sb.S2);
                foreach (var sb in element.RotateList)
                    _objs[i].Rotate(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.R1, sb.R2);
                foreach (var sb in element.MoveList)
                    _objs[i].Move(sb.Easing, (int)sb.StartTime, (int)sb.EndTime,
                        new System.Drawing.PointF(sb.X1 + 107, sb.Y1),
                        new System.Drawing.PointF(sb.X2 + 107, sb.Y2));
                foreach (var sb in element.FadeList)
                    _objs[i].Fade(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.F1, sb.F2);
                foreach (var sb in element.VectorList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Vx1, sb.Vy1,
                        sb.Vx2, sb.Vy2);
                foreach (var sb in element.MoveXList)
                    _objs[i].MoveX(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.X1 + 107, sb.X2 + 107);
                foreach (var sb in element.MoveYList)
                    _objs[i].MoveY(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Y1, sb.Y2);
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
