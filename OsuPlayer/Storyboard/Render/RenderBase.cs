using Milkitic.OsuPlayer.Storyboard.Layer;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;
using Mathe = SharpDX.Mathematics.Interop;

namespace Milkitic.OsuPlayer.Storyboard.Render
{
    public class RenderBase
    {
        protected D2D.Factory Factory { get; } = new D2D.Factory(D2D.FactoryType.SingleThreaded);
        public D2D.RenderTarget RenderTarget { get; set; }

        private readonly List<TaskLayerList> _layerList = new List<TaskLayerList>();
        private Task _task;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _render, _isRendering;
        public bool Vsync { get; set; } = true;
        public bool DisposeRequested { get; set; } = false;

        public RenderBase()
        {
            _render = true;
            StartMeasure();
        }

        public void AddLayers(IEnumerable<CustomLayer> layers)
        {
            _render = false;
            while (_isRendering) ;
            CancelMeasure();
            _layerList.AddRange(layers.Select(k => new TaskLayerList(k, null)));
            StartMeasure();
            _render = true;
        }

        public void RemoveAllLayers()
        {
            _render = false;
            CancelMeasure();
            while (_isRendering) ;
            foreach (var item in _layerList)
                item.Layer?.Dispose();
            _layerList.Clear();
            _render = true;
        }

        public void RemoveLayer(CustomLayer layer)
        {
            _render = false;
            CancelMeasure();
            while (_isRendering) ;
            var choice = _layerList.FirstOrDefault(k => k.Layer == layer);
            if (choice != null)
            {
                choice.Layer.Dispose();
                _layerList.Remove(choice);
            }
            _render = true;
        }

        public void Dispose()
        {
            DisposeRequested = true;
            if (_layerList != null)
                foreach (var item in _layerList)
                    item.Layer?.Dispose();
            CancelMeasure();
            RenderTarget?.Dispose();
            Factory?.Dispose();
        }

        private void CancelMeasure()
        {
            _cts.Cancel();
            Task.WaitAll(_layerList.Select(k => k.MeasureTask).Where(k => !k.IsCanceled && !k.IsCompleted).ToArray());
            Task.WaitAll(_task);
        }
        private void StartMeasure()
        {
            _cts = new CancellationTokenSource();
            _task = Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    foreach (var t in _layerList)
                    {
                        if (t.MeasureTask == null || t.MeasureTask.IsCanceled || t.MeasureTask.IsCompleted)
                            t.MeasureTask = Task.Run(() => { t.Layer.Measure(); }, _cts.Token);
                    }

                    Thread.Sleep(1);
                }
            }, _cts.Token);
        }

        public void UpdateFrame()
        {
            if (!_render) return;
            _isRendering = true;
            // Begin rendering
            RenderTarget.BeginDraw();
            RenderTarget.Clear(new Mathe.RawColor4(0, 0, 0, 1));

            foreach (var item in _layerList)
                item.Layer.OnFrameUpdate();
            // End drawing
            RenderTarget.TryEndDraw(out _, out _);
            _isRendering = false;
        }

        private class TaskLayerList
        {
            public readonly CustomLayer Layer;
            public Task MeasureTask;

            public TaskLayerList(CustomLayer layer, Task measureTask)
            {
                Layer = layer;
                MeasureTask = measureTask;
            }
        }
    }
}
