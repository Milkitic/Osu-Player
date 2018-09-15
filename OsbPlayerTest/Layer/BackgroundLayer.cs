using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Extension;
using Milkitic.OsbLib.Models;
using Milkitic.OsbLib.Models.EventType;
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

        public class Timing
        {
            public long Offset => ControlOffset + Watch.ElapsedMilliseconds;
            public long ControlOffset;
            public readonly Stopwatch Watch;

            public Timing(long controlOffset, Stopwatch watch)
            {
                ControlOffset = controlOffset;
                Watch = watch;
            }
        }

        private readonly ElementObject[] _objs;
        private RenderThings[] _renderList;
        private readonly Timing _timing;
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly ElementGroup _elementGroup;

        public BackgroundLayer(D2D.RenderTarget renderTarget, ElementGroup elementGroup) : base(renderTarget)
        {
            _watch.Start();
            Console.WriteLine(@"Expanding..");
            elementGroup.Expand();
            File.WriteAllText("d:\\ok.txt", elementGroup.ToString());
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
            _elementGroup = elementGroup;
            _timing = new Timing(0, new Stopwatch());
            _objs = new ElementObject[elements.Length];
            for (var i = 0; i < elements.Length; i++)
            {
                var item = elements[i];
                if (item.Elment.Type == ElementType.Animation)
                    _objs[i] = new AnimatedElementObject(RenderTarget, (AnimatedElement)item.Elment, _timing);
                else
                    _objs[i] = new ElementObject(RenderTarget, item.Elment, _timing);
            }

            const int updateDelay = 500;

            bool first = true;
            Task.Run(() =>
            {
                while (true)
                {
                    var ms = _timing.Offset;
                    _renderList = elements.Where(k => !k.Elment.FadeoutList.InRange((int)ms, (updateDelay + 100), -(updateDelay + 100)) &&   // todo: 有点问题
                                                      ms >= k.Elment.MinTime - (updateDelay + 100) &&
                                                      ms <= k.Elment.MaxTime).ToArray();
                    Console.WriteLine($@"更新图象队列：{_renderList.Length}");

                    for (var i = 0; i < _renderList.Length; i++)
                    {
                        _renderList[i].Events = _renderList[i].Elment.EventList
                            .Where(k => ms >= k.StartTime - (updateDelay + 150) && ms <= k.EndTime);
                    }

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

            _timing.Watch.Start();
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
                        case EventEnum.Parameter:
                            var p = (Parameter)e;
                            switch (p.Type)
                            {
                                case ParameterEnum.Horizontal:
                                    _objs[i].FlipH((int)p.StartTime, (int)p.EndTime);
                                    break;
                                case ParameterEnum.Vertical:
                                    _objs[i].FlipV((int)p.StartTime, (int)p.EndTime);
                                    break;
                                case ParameterEnum.Additive:
                                    _objs[i].Additive((int)p.StartTime, (int)p.EndTime);
                                    break;
                            }
                            break;
                        case EventEnum.Color:
                            _objs[i].Color(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.Start[1], e.Start[2],
                                e.End[0], e.End[1], e.End[2]);
                            break;
                    }
                }
                _objs[i].EndDraw();

            }
        }

        public override void Dispose()
        {

        }
    }
}
