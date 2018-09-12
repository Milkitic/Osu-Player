using SharpDX.Direct2D1;

namespace OsbPlayerTest.Layer
{
    internal abstract class DxLayer
    {
        protected readonly RenderTarget RenderTarget;

        protected DxLayer(RenderTarget renderTarget)
        {
            RenderTarget = renderTarget;
        }

        public abstract void Measure();
        public abstract void Draw();
        public abstract void Dispose();
    }
}

