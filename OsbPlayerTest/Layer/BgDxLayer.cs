using LibOsb;
using OsbPlayerTest.DxAnimation;
using OsbPlayerTest.Util;
using System;
using System.IO;
using D2D = SharpDX.Direct2D1;
namespace OsbPlayerTest.Layer
{
    internal class BgDxLayer : DxLayer
    {
        private readonly ElementGroup _elementGroup;
        private readonly BitmapObject[] _objs;

        public BgDxLayer(D2D.RenderTarget renderTarget) : base(renderTarget)
        {

        }

        public BgDxLayer(D2D.RenderTarget renderTarget, ElementGroup elementGroup) : base(renderTarget)
        {
            _elementGroup = elementGroup;
            _objs = new BitmapObject[_elementGroup.ElementList.Count];
            for (var i = 0; i < _elementGroup.ElementList.Count; i++)
            {
                var item = _elementGroup.ElementList[i];
                if (item.Type == LibOsb.Enums.ElementType.Animation)
                    continue;
                _objs[i] = new BitmapObject(RenderTarget, RenderTarget.LoadBitmap(Path.Combine(Program.Fi.Directory.FullName, item.ImagePath)),
                    item.Origin, new SharpDX.Mathematics.Interop.RawPoint((int)item.DefaultX + 107, (int)item.DefaultY));
            }
        }

        public override void Measure()
        {


        }

        private bool ok = false;
        public override void Draw()
        {
            for (var i = 0; i < _elementGroup.ElementList.Count; i++)
            {
                var item = _elementGroup.ElementList[i];
                if (_objs[i] == null)
                    continue;

                _objs[i].StartDraw();
                if (ok && _objs[i].Offset < _objs[i].MinTime)
                    continue;
                foreach (var sb in item.ScaleList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start,
                        sb.Start, sb.End, sb.End);
                foreach (var sb in item.RotateList)
                    _objs[i].Rotate(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                foreach (var sb in item.MoveList)
                    _objs[i].Move(sb.Easing, (int)sb.StartTime, (int)sb.EndTime,
                        new System.Drawing.PointF(sb.Start.x + 107, sb.Start.y),
                        new System.Drawing.PointF(sb.End.x + 107, sb.End.y));
                foreach (var sb in item.FadeList)
                    _objs[i].Fade(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                foreach (var sb in item.VectorList)
                    _objs[i].ScaleVec(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start.x,
                        sb.Start.y, sb.End.x, sb.End.y);
                foreach (var sb in item.MoveXList)
                    _objs[i].MoveX(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start + 107, sb.End + 107);
                foreach (var sb in item.MoveYList)
                    _objs[i].MoveY(sb.Easing, (int)sb.StartTime, (int)sb.EndTime, sb.Start, sb.End);
                _objs[i].EndDraw();
            }

            //Console.WriteLine(_objs[1].Offset);
            ok = true;
        }

        public override void Dispose()
        {

        }
    }
}
