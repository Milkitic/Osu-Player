using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Extension;
using Milkitic.OsbLib.Models;
using Milkitic.OsbLib.Models.EventType;
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
