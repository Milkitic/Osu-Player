using System;
using System.Collections.Generic;
using System.Diagnostics;
using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using SharpDX;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;
using RectangleF = SharpDX.RectangleF;

namespace Milkitic.OsuPlayer.Wpf.Storyboard.Animation
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

        public ElementInstance(D2D.RenderTarget target, Element element, Size2F vSize, Timing timing = null)
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
                ? new AnimatedObject((AnimatedElement)element, timing, target, vSize)
                : new StoryboardObject(element, timing, target, vSize);
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

        private bool _flag = false;
        public void EndDraw()
        {
            if (_sbObj.IsFinished || _timing.Offset < _sbObj.MinTime || _sbObj.F.RealTime <= 0)
            {
                if (_flag)
                {
                    _sbObj.ResetRealTime();
                    _flag = false;
                }
                return;
            }

            _flag = true;
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


            var translateMtx = Matrix3x2.Translation(-_sbObj.X.RealTime, -_sbObj.Y.RealTime);
            var scaleMtx = Matrix3x2.Scaling((_sbObj.UseH ? -1 : 1) * _sbObj.Vx.RealTime, (_sbObj.UseV ? -1 : 1) * _sbObj.Vy.RealTime);
            var rotateMtx = Matrix3x2.Rotation(_sbObj.Rad.RealTime);
            var negTranslateMtx = Matrix3x2.Translation(_sbObj.X.RealTime, _sbObj.Y.RealTime);
            _target.Transform = translateMtx * scaleMtx * rotateMtx * negTranslateMtx;

            float rectL = _sbObj.Rect.RealTime.Left - _sbObj.OriginOffsetX - (_sbObj.UseH ? 2 * (_sbObj.Width / 2 - _sbObj.OriginOffsetX) : 0);
            float rectT = _sbObj.Rect.RealTime.Top - _sbObj.OriginOffsetY - (_sbObj.UseV ? 2 * (_sbObj.Height / 2 - _sbObj.OriginOffsetY) : 0);
            float rectW = _sbObj.Rect.RealTime.Right - _sbObj.Rect.RealTime.Left;
            float rectH = _sbObj.Rect.RealTime.Bottom - _sbObj.Rect.RealTime.Top;

            var realRect = new RectangleF(rectL, rectT, rectW, rectH);
            if (_sbObj.Texture != null)
            {
                if (_sbObj.R.RealTime != 255 || _sbObj.G.RealTime != 255 || _sbObj.B.RealTime != 255)
                {
                    //JUST FAKE THING
                    var sb = new D2D.SolidColorBrush(_target,
                       new Mathe.RawColor4(_sbObj.R.RealTime / 255f, _sbObj.G.RealTime / 255f,
                           _sbObj.B.RealTime / 255f, _sbObj.F.RealTime));
                    _target.FillOpacityMask(_sbObj.Texture, sb, D2D.OpacityMaskContent.Graphics, realRect, null);
                    sb.Dispose();
                    sb = null;
                }
                else
                    _target.DrawBitmap(_sbObj.Texture, realRect, _sbObj.F.RealTime, D2D.BitmapInterpolationMode.Linear);
            }

#if DEBUG
            //_target.DrawRectangle(realRect, _redBrush, 1);
#endif
            _target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
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
