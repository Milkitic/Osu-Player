using Milkitic.OsuPlayer.Media.Storyboard.Animation;
using OSharp.Storyboard;
using OSharp.Storyboard.Events;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Media.Storyboard.Layer
{
    public class StoryboardLayer : CustomLayer
    {
        private ElementInstance[] _instances;
        private RenderThings[] _renderList;
        private Timing _timing;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _task;

        private readonly Size2F _vSize;

        public StoryboardLayer(RenderTarget renderTarget, IReadOnlyList<Element> elements, Timing timing) : base(renderTarget)
        {
            if (elements == null)
                return;
            _vSize = new Size2F(renderTarget.Size.Width / 854f, renderTarget.Size.Height / 480f);
            RenderThings[] instants = new RenderThings[elements.Count];
            for (int i = 0; i < instants.Length; i++)
                instants[i] = new RenderThings
                {
                    Index = i,
                    Elment = elements[i],
                    Events = elements[i].EventList.ToList()
                };

            _timing = timing ?? new Timing(0, new Stopwatch());
            _instances = new ElementInstance[instants.Length];
            for (var i = 0; i < instants.Length; i++)
            {
                var item = instants[i];
                _instances[i] = new ElementInstance(RenderTarget, item.Elment, _vSize, _timing);
            }

            LoadFirstFrame(instants);

            _timing.Watch.Start();
        }

        public override void Measure()
        {

        }

        public override void OnFrameUpdate()
        {
            if (_renderList == null)
                return;
            foreach (var render in _renderList)
            {
                int i = render.Index;
                var events = render.Events;

                if (_instances[i] == null)
                    continue;
                _instances[i].StartDraw();
                foreach (ICommonEvent e in events)
                {
                    switch (e.EventType)
                    {
                        case EventType.Fade:
                            _instances[i].Fade(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.End[0]);
                            break;
                        case EventType.Move:
                            _instances[i].Move(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                new System.Drawing.PointF((e.Start[0] + 107) * _vSize.Width,
                                    e.Start[1] * _vSize.Height),
                                new System.Drawing.PointF((e.End[0] + 107) * _vSize.Width, e.End[1] * _vSize.Height));
                            break;
                        case EventType.MoveX:
                            _instances[i].MoveX(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                (e.Start[0] + 107) * _vSize.Width, (e.End[0] + 107) * _vSize.Width);
                            break;
                        case EventType.MoveY:
                            _instances[i].MoveY(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                e.Start[0] * _vSize.Height, e.End[0] * _vSize.Height);
                            break;
                        case EventType.Scale:
                            _instances[i].ScaleVec(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                e.Start[0] * _vSize.Width, e.Start[0] * _vSize.Height, e.End[0] * _vSize.Width,
                                e.End[0] * _vSize.Height);
                            break;
                        case EventType.Vector:
                            _instances[i].ScaleVec(e.Easing, (int)e.StartTime, (int)e.EndTime,
                                e.Start[0] * _vSize.Width, e.Start[1] * _vSize.Height, e.End[0] * _vSize.Width,
                                e.End[1] * _vSize.Height);
                            break;
                        case EventType.Rotate:
                            _instances[i].Rotate(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.End[0]);
                            break;
                        case EventType.Parameter:
                            var p = (Parameter)e;
                            switch (p.Type)
                            {
                                case ParameterType.Horizontal:
                                    _instances[i].FlipH((int)p.StartTime, (int)p.EndTime);
                                    break;
                                case ParameterType.Vertical:
                                    _instances[i].FlipV((int)p.StartTime, (int)p.EndTime);
                                    break;
                                case ParameterType.Additive:
                                    _instances[i].Additive((int)p.StartTime, (int)p.EndTime);
                                    break;
                            }

                            break;
                        case EventType.Color:
                            _instances[i].Color(e.Easing, (int)e.StartTime, (int)e.EndTime, e.Start[0], e.Start[1],
                                e.Start[2],
                                e.End[0], e.End[1], e.End[2]);
                            break;
                    }
                }

                _instances[i].EndDraw();
            }
        }

        public override void Dispose()
        {
            _cts.Cancel();
            if (_task != null) Task.WaitAll(_task);
            if (_instances != null)
            {
                foreach (var instance in _instances)
                    instance.Dispose();
            }
            _instances = null;

            if (_renderList != null)
                for (var i = 0; i < _renderList.Length; i++)
                {
                    _renderList[i].Elment = null;
                    _renderList[i].Events = null;
                }
            _renderList = null;

            _timing = null;
        }

        private void LoadFirstFrame(RenderThings[] elements)
        {
            bool first = true;
            _task = Task.Run(() =>
            {
                int updateDelay = (int)(500 / _timing.PlayBack);
                int delay = (int)(150 * _timing.PlayBack);
                int delay2 = (int)(150 * _timing.PlayBack);

                while (!_cts.IsCancellationRequested)
                {
                    var ms = _timing.Offset;
                    _renderList = elements.Where(k =>
                        !k.Elment.ObsoleteList.ContainsTimingPoint(out _, (int)ms, updateDelay + delay,
                            -(updateDelay + delay)) && // todo: Maybe has some defects
                        ms >= k.Elment.MinTime - (updateDelay + delay) &&
                        ms <= k.Elment.MaxTime).ToArray();
                    Console.WriteLine($@"更新图象队列：{_renderList.Length}");

                    for (var i = 0; i < _renderList.Length; i++)
                    {
                        _renderList[i].Events = _renderList[i].Elment.EventList
                            .Where(k => ms >= k.StartTime - (updateDelay + delay2) && ms <= k.EndTime);
                    }

                    Console.WriteLine($@"更新动作队列：{_renderList.Select(k => k.Events.Count()).Sum()}");
                    Thread.Sleep(updateDelay);

                    first = false;
                }
            }, _cts.Token);
            Console.WriteLine(@"Loading...");
            while (first)
                Thread.Sleep(1);
        }
    }
}
