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
            public IEnumerable<Event> Events;
        }

        private readonly ElementObject[] _objs;
        private readonly Stopwatch _watch = new Stopwatch();
        private RenderThings[] _renderList;

        public BackgroundLayer(D2D.RenderTarget renderTarget, ElementGroup elementGroup) : base(renderTarget)
        {
            _watch.Start();
            Console.WriteLine(@"Expanding..");
            elementGroup.Expand();
            Console.WriteLine($@"Expand done in {_watch.ElapsedMilliseconds} ms");
            _watch.Stop();
            _watch.Reset();
            RenderThings[] elements = new RenderThings[elementGroup.ElementList.Count];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new RenderThings
                {
                    Index = i,
                    Elment = elementGroup.ElementList[i],
                    Events = elementGroup.ElementList[i].EventList
                };

            _objs = new ElementObject[elements.Length];
            for (var i = 0; i < elements.Length; i++)
            {
                var item = elements[i];
                if (item.Elment.Type == ElementType.Animation)
                    continue;

                _objs[i] = new ElementObject(RenderTarget, item.Elment, _watch);
            }

            const int updateDelay = 1000;

            bool first = true;
            Task.Run(() =>
            {
                while (true)
                {
                    var ms = _watch.ElapsedMilliseconds;
                    _renderList = elements.Where(k => ms > k.Elment.MinTime - (updateDelay + 100) && ms < k.Elment.MaxTime &&
                        !k.Elment.FadeoutList.InRange((int)ms)).ToArray();
                    Console.WriteLine($@"更新图象队列：{_renderList.Length}");

                    // todo: 有点问题
                    for (var i = 0; i < _renderList.Length; i++)
                    {
                        _renderList[i].Events = _renderList[i].Elment.EventList
                            .Where(k => k.StartTime >= ms - (k.EndTime - k.StartTime) && ms <= k.EndTime);
                    }
                    //

                    Console.WriteLine($@"更新动作队列：{_renderList.Select(k => k.Events.Count()).Sum()}");
                    Thread.Sleep(updateDelay);

                    first = false;
                }
            });
            Console.WriteLine(@"Loading...");
            while (first)
            {
                Thread.Sleep(1);
            }
            _watch.Start();
        }

        public override void Measure()
        {

        }

        public override void Draw()
        {
            foreach (var render in _renderList)
            {
                int i = render.Index;
                var element = render.Elment;
                var events = render.Events;

                if (_objs[i] == null)
                    continue;
                _objs[i].StartDraw();
                foreach (Event e in events)
                {
                    switch (e.EventType)
                    {
                        case EventEnum.Fade:
                            _objs[i].Fade(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.End[0]);
                            break;
                        case EventEnum.Move:
                            _objs[i].Move(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                    new System.Drawing.PointF(e.Start[0] + 107, e.Start[1]),
                                    new System.Drawing.PointF(e.End[0] + 107, e.End[1]));
                            break;
                        case EventEnum.MoveX:
                            _objs[i].MoveX(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0] + 107, e.End[0] + 107);
                            break;
                        case EventEnum.MoveY:
                            _objs[i].MoveY(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.End[0]);
                            break;
                        case EventEnum.Scale:
                            _objs[i].ScaleVec(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.Start[0], e.End[0], e.End[0]);
                            break;
                        case EventEnum.Vector:
                            _objs[i].ScaleVec(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.Start[1], e.End[0], e.End[1]);
                            break;
                        case EventEnum.Rotate:
                            _objs[i].Rotate(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.End[0]);
                            break;
                            //case EventEnum.Color:
                            //    break;
                            //case EventEnum.Parameter:
                            //    break;
                    }
                }
                //foreach (var sb in element.ScaleList)
                //    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.S1,
                //        sb.S1, sb.S2, sb.S2);
                //foreach (var sb in element.RotateList)
                //    _objs[i].Rotate(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.R1, sb.R2);
                //foreach (var sb in element.MoveList)
                //    _objs[i].Move(sb.Easing, (int)sb.StartTime, (int)sb.EndTime,
                //        new System.Drawing.PointF(sb.X1 + 107, sb.Y1),
                //        new System.Drawing.PointF(sb.X2 + 107, sb.Y2));
                //foreach (var sb in element.FadeList)
                //    _objs[i].Fade(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.F1, sb.F2);
                //foreach (var sb in element.VectorList)
                //    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Vx1, sb.Vy1,
                //        sb.Vx2, sb.Vy2);
                //foreach (var sb in element.MoveXList)
                //    _objs[i].MoveX(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.X1 + 107, sb.X2 + 107);
                //foreach (var sb in element.MoveYList)
                //    _objs[i].MoveY(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Y1, sb.Y2);
                _objs[i].EndDraw();

            }

            //Console.WriteLine(_objs[1].Offset);
        }

        public override void Dispose()
        {

        }
    }
}
