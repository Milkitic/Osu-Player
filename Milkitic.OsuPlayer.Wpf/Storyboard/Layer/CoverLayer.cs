using D2D = SharpDX.Direct2D1;

namespace Milkitic.OsuPlayer.Wpf.Storyboard.Layer
{
    internal class CoverLayer : CustomLayer
    {
        private readonly D2D.Brush _brush;

        public CoverLayer(D2D.RenderTarget renderTarget) : base(renderTarget)
        {
            _brush = new D2D.SolidColorBrush(renderTarget, new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 1));
        }

        public override void Measure()
        {

        }

        public override void OnFrameUpdate()
        {
            RenderTarget.FillRectangle(new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, 107, 480), _brush);
            RenderTarget.FillRectangle(new SharpDX.Mathematics.Interop.RawRectangleF(747, 0, 854, 480), _brush);
        }

        public override void Dispose()
        {

        }
    }
}
