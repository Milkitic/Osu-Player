using SharpDX.Direct2D1;

namespace Milkitic.OsuPlayer.Media.Storyboard.Layer
{
    public abstract class CustomLayer
    {
        protected readonly RenderTarget RenderTarget;

        protected CustomLayer(RenderTarget renderTarget)
        {
            RenderTarget = renderTarget;
        }

        public abstract void Measure(); //Calculation before drawing.
        public abstract void OnFrameUpdate();
        public abstract void Dispose();
    }
}

