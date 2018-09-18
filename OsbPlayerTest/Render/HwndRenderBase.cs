using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using D2D = SharpDX.Direct2D1;
using DX = SharpDX;
using DXGI = SharpDX.DXGI;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest.Render
{
    public sealed class HwndRenderBase : RenderBase
    {
        public HwndRenderBase(Control control)
        {
            var hwndProp = new D2D.HwndRenderTargetProperties
            {
                Hwnd = control.Handle,
                PixelSize = new DX.Size2(control.ClientSize.Width, control.ClientSize.Height),
                PresentOptions = Vsync ? D2D.PresentOptions.None : D2D.PresentOptions.Immediately
            };

            var pixelFormat = new D2D.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied);
            var renderProp = new D2D.RenderTargetProperties(D2D.RenderTargetType.Hardware, pixelFormat, 96, 96,
                D2D.RenderTargetUsage.ForceBitmapRemoting, D2D.FeatureLevel.Level_DEFAULT);

            RenderTarget = new D2D.WindowRenderTarget(Factory, renderProp, hwndProp)
            {
                AntialiasMode = D2D.AntialiasMode.Aliased,
                TextAntialiasMode = D2D.TextAntialiasMode.Default,
                Transform = new Mathe.RawMatrix3x2(1, 0, 0, 1, 0, 0)
            };
        }
    }
}
