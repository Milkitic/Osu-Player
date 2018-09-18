using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Utils;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;
using RectangleF = SharpDX.RectangleF;

namespace OsbPlayerTest.Animation
{
    internal sealed class ElementInstance : AnimatedInstance, IDisposable
    {
        private readonly D2D.RenderTarget _target;

        // control
        private bool IsStarted { get; set; }
        private readonly Timing _timing;
        private readonly bool _innerWatch = true;

        private int? _prevMs;
        private readonly Queue<int> _avgMs = new Queue<int>();

        // debug
#if DEBUG
        private readonly D2D.Brush _redBrush;
#endif
        private readonly StoryboardObject _sbObj;

        public ElementInstance(D2D.RenderTarget target, Element element, Timing timing = null)
        {
            _target = target;
#if DEBUG
            _redBrush = new D2D.SolidColorBrush(target, new Mathe.RawColor4(1, 0, 0, 1));
#endif

            if (timing != null)
            {
                _timing = timing;
                _innerWatch = false;
            }
            else
                _timing = new Timing(0, new Stopwatch());

            _sbObj = element.Type == ElementType.Animation
                ? new AnimatedObject((AnimatedElement)element, timing, target)
                : new StoryboardObject(element, timing, target);
        }

        public void StartDraw()
        {
            if (!IsStarted)
            {
                if (_innerWatch)
                    _timing.Watch.Start();
                IsStarted = true;
            }
            else if (IsStarted && _timing.Offset >= _sbObj.MaxTime)
            {
                if (_innerWatch)
                {
                    _timing.Watch.Stop();
                    _timing.Watch.Reset();
                }
            }
        }

        public void EndDraw()
        {
            if (_sbObj is AnimatedObject ani && _timing.Offset >= ani.MinTime && _timing.Offset <= ani.MaxTime)
            {
                int imgIndex;
                if (ani.Loop)
                    imgIndex = (int)((_timing.Offset - ani.MinTime) / ani.Delay % ani.Times);
                else
                {
                    imgIndex = (int)((_timing.Offset - ani.MinTime) / ani.Delay);
                    if (imgIndex >= ani.Times)
                        imgIndex = ani.Times - 1;
                }

                if (imgIndex != ani.PrevIndex)
                {
                    ani.PrevIndex = imgIndex;
                    ani.Texture = ani.TextureList[imgIndex];
                }
            }

            if (_sbObj.IsFinished || _timing.Offset < _sbObj.MinTime || _sbObj.Statics.F.RealTime <= 0) return;

            var translateMtx = Matrix3x2.Translation(-_sbObj.Statics.X.RealTime, -_sbObj.Statics.Y.RealTime);
            var scaleMtx = Matrix3x2.Scaling((_sbObj.UseH ? -1 : 1) * _sbObj.Statics.Vx.RealTime, (_sbObj.UseV ? -1 : 1) * _sbObj.Statics.Vy.RealTime);
            var rotateMtx = Matrix3x2.Rotation(_sbObj.Statics.Rad.RealTime);
            var negTranslateMtx = Matrix3x2.Translation(_sbObj.Statics.X.RealTime, _sbObj.Statics.Y.RealTime);
            _target.Transform = translateMtx * scaleMtx * rotateMtx * negTranslateMtx;

            float rectL = _sbObj.Statics.Rect.RealTime.Left - _sbObj.OriginOffsetX - (_sbObj.UseH ? 2 * (_sbObj.Width / 2 - _sbObj.OriginOffsetX) : 0);
            float rectT = _sbObj.Statics.Rect.RealTime.Top - _sbObj.OriginOffsetY - (_sbObj.UseV ? 2 * (_sbObj.Height / 2 - _sbObj.OriginOffsetY) : 0);
            float rectW = _sbObj.Statics.Rect.RealTime.Right - _sbObj.Statics.Rect.RealTime.Left;
            float rectH = _sbObj.Statics.Rect.RealTime.Bottom - _sbObj.Statics.Rect.RealTime.Top;

            var realRect = new RectangleF(rectL, rectT, rectW, rectH);
            if (_sbObj.Texture != null)
            {
                if (_sbObj.Statics.R.RealTime != 255 || _sbObj.Statics.G.RealTime != 255 || _sbObj.Statics.B.RealTime != 255)
                {
                    //JUST FAKE THING
                    var sb = new D2D.SolidColorBrush(_target,
                       new Mathe.RawColor4(_sbObj.Statics.R.RealTime / 255f, _sbObj.Statics.G.RealTime / 255f,
                           _sbObj.Statics.B.RealTime / 255f, _sbObj.Statics.F.RealTime));
                    _target.FillOpacityMask(_sbObj.Texture, sb, D2D.OpacityMaskContent.Graphics, realRect, null);
                    sb.Dispose();
                }
                else
                    _target.DrawBitmap(_sbObj.Texture, realRect, _sbObj.Statics.F.RealTime, D2D.BitmapInterpolationMode.Linear);
            }

#if DEBUG
            //_target.DrawRectangle(realRect, _redBrush, 1);
#endif
            _target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
        }

        public void AdjustTime(int ms)
        {
            if (_avgMs.Count >= 20)
                _avgMs.Dequeue();
            _avgMs.Enqueue(ms);

            if (!_innerWatch || ms != _prevMs || _avgMs.All(q => q == _avgMs.Average()))
            {
                _prevMs = ms;
                if (Math.Abs(_timing.Offset - ms) <= 3)
                    return;

                _timing.SetTiming(ms);
            }
        }

        public override void Move(EasingType easingEnum, int startTime, int endTime, Gdip.PointF startPoint, Gdip.PointF endPoint) => _sbObj.Move(easingEnum, startTime, endTime, startPoint, endPoint);
        public override void MoveX(EasingType easingEnum, int startTime, int endTime, float startX, float endX) => _sbObj.MoveX(easingEnum, startTime, endTime, startX, endX);
        public override void MoveY(EasingType easingEnum, int startTime, int endTime, float startY, float endY) => _sbObj.MoveY(easingEnum, startTime, endTime, startY, endY);
        public override void Rotate(EasingType easingEnum, int startTime, int endTime, float startRad, float endRad) => _sbObj.Rotate(easingEnum, startTime, endTime, startRad, endRad);
        public void FlipH(int startTime, int endTime) => _sbObj.FlipH(startTime, endTime);
        public void FlipV(int startTime, int endTime) => _sbObj.FlipV(startTime, endTime);
        public override void ScaleVec(EasingType easingEnum, int startTime, int endTime, float startVx, float startVy, float endVx, float endVy) => _sbObj.ScaleVec(easingEnum, startTime, endTime, startVx, startVy, endVx, endVy);
        public override void Fade(EasingType easingEnum, int startTime, int endTime, float startOpacity, float endOpacity) => _sbObj.Fade(easingEnum, startTime, endTime, startOpacity, endOpacity);
        public void Color(EasingType easingEnum, int startTime, int endTime, float r1, float g1, float b1, float r2, float g2, float b2) => _sbObj.Color(easingEnum, startTime, endTime, r1, g1, b1, r2, g2, b2);
        public void Additive(int startTime, int endTime) => _sbObj.Additive(startTime, endTime);

        public void Dispose()
        {
#if DEBUG
            _redBrush?.Dispose();
#endif
            _sbObj.Dispose();
        }
    }
}
