using OsbPlayerTest.Util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Utils;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest.DxAnimation
{
    internal class BitmapObject : AnimationObject, IDisposable
    {
        public Size2F Size => Bitmap.Size;
        public float Width => Size.Width;
        public float Height => Size.Height;
        private OriginType Origin { get; }

        protected readonly D2D.RenderTarget Target;
        protected readonly D2D.Bitmap Bitmap;

        // control
        protected readonly bool EnableLog;
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        private readonly Stopwatch _watch;
        private bool _innerWatch = true;

        public long Offset;
        private long _timeOffset;

        private readonly int _originOffsetX, _originOffsetY;

        // debug
#if DEBUG
        private readonly D2D.Brush _redBrush;
#endif

        #region Private statics

        private TimeRange _fadeTime = TimeRange.Default;
        private TimeRange _rotateTime = TimeRange.Default;
        private TimeRange _vTime = TimeRange.Default;
        private TimeRange _movTime = TimeRange.Default;
        private TimeRange _inRectTime = TimeRange.Default;
        private Static<float> _f;
        private Static<float> _x, _y, _w, _h;
        private Static<float> _r, _vx, _vy;
        private Static<float> _inX, _inY, _inW, _inH;

        public int MaxTime => TimeRange.GetMaxTime(_fadeTime, _rotateTime, _movTime, _inRectTime);
        public int MinTime => TimeRange.GetMinTime(_fadeTime, _rotateTime, _movTime, _inRectTime);

        private Static<Mathe.RawRectangleF> InRect => new Static<Mathe.RawRectangleF>
        {
            Source = new Mathe.RawRectangleF(_inX.Source, _inY.Source, _inX.Source + _inW.Source, _inY.Source + _inH.Source),
            RealTime =
                new Mathe.RawRectangleF(_inX.RealTime - _originOffsetX, _inY.RealTime - _originOffsetY,
                    _inX.RealTime + _inW.RealTime - _originOffsetX, _inY.RealTime + _inH.RealTime - _originOffsetY),
            Target = new Mathe.RawRectangleF(_inX.Target, _inY.Target, _inX.Target + _inW.Target, _inY.Target + _inH.Target)
        };

        private Static<Mathe.RawRectangleF> Rect => new Static<Mathe.RawRectangleF>
        {
            Source = new Mathe.RawRectangleF(_x.Source, _y.Source, _x.Source + _w.Source, _y.Source + _h.Source),
            RealTime =
                new Mathe.RawRectangleF(_x.RealTime, _y.RealTime, _x.RealTime + _w.RealTime, _y.RealTime + _h.RealTime),
            Target = new Mathe.RawRectangleF(_x.Target, _y.Target, _x.Target + _w.Target, _y.Target + _h.Target)
        };

        #endregion private statics

        public BitmapObject(D2D.RenderTarget target, D2D.Bitmap bitmap, OriginType origin, Mathe.RawPoint initPosision,
            bool enableLog = false) : this(target, bitmap, origin, initPosision, null, enableLog) { }

        public BitmapObject(D2D.RenderTarget target, D2D.Bitmap bitmap, OriginType origin, Mathe.RawPoint initPosision, Stopwatch sw,
            bool enableLog = false)
        {
            Target = target;
            Bitmap = bitmap;
            Origin = origin;
#if DEBUG
            _redBrush = new D2D.SolidColorBrush(target, new Mathe.RawColor4(1, 0, 0, 1));
#endif
            _x = (Static<float>)initPosision.X;
            _y = (Static<float>)initPosision.Y;
            _f = (Static<float>)1;
            _r = (Static<float>)0;
            _vx = (Static<float>)1;
            _vy = (Static<float>)1;

            // rects
            _w = (Static<float>)bitmap.Size.Width;
            _h = (Static<float>)bitmap.Size.Height;

            _inX = (Static<float>)0;
            _inY = (Static<float>)0;
            _inW = (Static<float>)bitmap.Size.Width;
            _inH = (Static<float>)bitmap.Size.Height;

            //origion
            switch (origin)
            {
                //case OriginType.Free:
                //    _originOffsetX = (origin.X ?? 0) - initPosision.X;
                //    _originOffsetY = (origin.Y ?? 0) - initPosision.Y;
                //    break;
                case OriginType.BottomLeft:
                    _originOffsetX = 0;
                    _originOffsetY = (int)(Height);
                    break;
                case OriginType.BottomCentre:
                    _originOffsetX = (int)(Width / 2);
                    _originOffsetY = (int)(Height);
                    break;
                case OriginType.BottomRight:
                    _originOffsetX = (int)(Width);
                    _originOffsetY = (int)(Height);
                    break;
                case OriginType.CentreLeft:
                    _originOffsetX = 0;
                    _originOffsetY = (int)(Height / 2);
                    break;
                case OriginType.Centre:
                    _originOffsetX = (int)(Width / 2);
                    _originOffsetY = (int)(Height / 2);
                    break;
                case OriginType.CentreRight:
                    _originOffsetX = (int)(Width);
                    _originOffsetY = (int)(Height / 2);
                    break;
                case OriginType.TopLeft:
                    _originOffsetX = 0;
                    _originOffsetY = 0;
                    break;
                case OriginType.TopCentre:
                    _originOffsetX = (int)(Width / 2);
                    _originOffsetY = 0;
                    break;
                case OriginType.TopRight:
                    _originOffsetX = (int)(Width);
                    _originOffsetY = 0;
                    break;
            }

            if (sw != null)
            {
                _watch = sw;
                _innerWatch = false;
            }
            else
                _watch = new Stopwatch();

            EnableLog = enableLog;
        }


        
        /// <summary>
        /// Do not use with MOVEX or MOVEY at same time!
        /// </summary>
        public override void Move(EasingType easingEnum, int startTime, int endTime, Gdip.PointF startPoint, Gdip.PointF endPoint)
        {
            if (_movTime.Max == int.MaxValue || endTime >= _movTime.Max)
            {
                _movTime.Max = endTime;
                _x.Target = endPoint.X;
                _y.Target = endPoint.Y;
            }

            if (_movTime.Min == int.MinValue || startTime <= _movTime.Min)
            {
                _movTime.Min = startTime;
                _x.Source = startPoint.X;
                _y.Source = startPoint.Y;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _movTime.Min)
            {
                _x.RealTimeToSource();
                _y.RealTimeToSource();
            }

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _x.RealTime = startPoint.X + (float)easingEnum.Ease(t) * (endPoint.X - startPoint.X);
                _y.RealTime = startPoint.Y + (float)easingEnum.Ease(t) * (endPoint.Y - startPoint.Y);
            }

            if (ms >= _movTime.Max)
            {
                _x.RealTimeToTarget();
                _y.RealTimeToTarget();
            }
        }

        public override void MoveX(EasingType easingEnum, int startTime, int endTime, float startX, float endX)
        {
            if (_movTime.Max == int.MaxValue || endTime > _movTime.Max)
            {
                _movTime.Max = endTime;
                _x.Target = endX;
            }

            if (_movTime.Min == int.MinValue || startTime < _movTime.Min)
            {
                _movTime.Min = startTime;
                _x.Source = startX;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _movTime.Min)
                _x.RealTimeToSource();

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _x.RealTime = startX + (float)easingEnum.Ease(t) * (endX - startX);
            }

            if (ms >= _movTime.Max)
                _x.RealTimeToTarget();
        }

        public override void MoveY(EasingType easingEnum, int startTime, int endTime, float startY, float endY)
        {
            if (_movTime.Max == int.MaxValue || endTime > _movTime.Max)
            {
                _movTime.Max = endTime;
                _y.Target = endY;
            }

            if (_movTime.Min == int.MinValue || startTime < _movTime.Min)
            {
                _movTime.Min = startTime;
                _y.Source = startY;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _movTime.Min)
                _y.RealTimeToSource();

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _y.RealTime = startY + (float)easingEnum.Ease(t) * (endY - startY);
            }

            if (ms >= _movTime.Max)
                _y.RealTimeToTarget();
        }

        public override void Rotate(EasingType easingEnum, int startTime, int endTime, float startRad, float endRad)
        {
            //float startRad = (float)(Math.PI * startDeg / 180d);
            //float endRad = (float)(Math.PI * endDeg / 180d);

            if (_rotateTime.Max == int.MaxValue || endTime > _rotateTime.Max)
            {
                _rotateTime.Max = endTime;
                _r.Target = endRad;
            }

            if (_rotateTime.Min == int.MinValue || startTime < _rotateTime.Min)
            {
                _rotateTime.Min = startTime;
                _r.Source = startRad;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _rotateTime.Min)
            {
                _r.RealTimeToSource();
            }

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _r.RealTime = startRad + (float)easingEnum.Ease(t) * (endRad - startRad);
            }

            if (ms >= _rotateTime.Max)
            {
                _r.RealTimeToTarget();
            }
        }

        /// <summary>
        /// Do not use with SCALE or FREERECT at same time!
        /// </summary>
        public override void ScaleVec(EasingType easingEnum, int startTime, int endTime, float startVx, float startVy, float endVx,
            float endVy)
        {
            if (_vTime.Max == int.MaxValue || endTime > _vTime.Max)
            {
                _vTime.Max = endTime;
                _vx.Target = endVx;
                _vy.Target = endVy;
            }

            if (_vTime.Min == int.MinValue || startTime < _vTime.Min)
            {
                _vTime.Min = startTime;
                _vx.Source = startVx;
                _vy.Source = startVy;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _vTime.Min)
            {
                _vx.RealTimeToSource();
                _vy.RealTimeToSource();
            }

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _vx.RealTime = startVx + (float)easingEnum.Ease(t) * (endVx - startVx);
                _vy.RealTime = startVy + (float)easingEnum.Ease(t) * (endVy - startVy);
            }

            if (ms >= _vTime.Max)
            {
                _vx.RealTimeToTarget();
                _vy.RealTimeToTarget();
            }
        }


        public override void Fade(EasingType easingEnum, int startTime, int endTime, float startOpacity, float endOpacity)
        {
            if (_fadeTime.Max == int.MaxValue || endTime > _fadeTime.Max)
            {
                _fadeTime.Max = endTime;
                _f.Target = endOpacity;
            }

            if (_fadeTime.Min == int.MinValue || startTime < _fadeTime.Min)
            {
                _fadeTime.Min = startTime;
                _f.Source = startOpacity;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _fadeTime.Min)
            {
                _f.RealTimeToSource();
            }
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _f.RealTime = startOpacity + (float)easingEnum.Ease(t) * (endOpacity - startOpacity);
            }

            if (ms >= _fadeTime.Max)
            {
                _f.RealTimeToTarget();
            }
        }

        /// <summary>
        /// Do not use with any MOVE or any SCALE at same time!
        /// </summary>
        public void FreeRect(EasingType easingEnum, int startTime, int endTime, Mathe.RawRectangleF startRect,
            Mathe.RawRectangleF endRect)
        {
            if (_movTime.Max == int.MaxValue || endTime > _movTime.Max)
            {
                _movTime.Max = endTime;
                _x.Target = endRect.Left;
                _y.Target = endRect.Top;
                _w.Target = endRect.Right - endRect.Left;
                _h.Target = endRect.Bottom - endRect.Top;
            }

            if (_movTime.Min == int.MinValue || startTime < _movTime.Min)
            {
                _movTime.Min = startTime;
                _x.Source = startRect.Left;
                _y.Source = startRect.Top;
                _w.Source = startRect.Right - startRect.Left;
                _h.Source = startRect.Bottom - startRect.Top;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _movTime.Min)
            {
                _x.RealTimeToSource();
                _y.RealTimeToSource();
                _w.RealTimeToSource();
                _h.RealTimeToSource();
            }

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _x.RealTime = startRect.Left + (float)easingEnum.Ease(t) * (endRect.Left - startRect.Left);
                _y.RealTime = startRect.Top + (float)easingEnum.Ease(t) * (endRect.Top - startRect.Top);
                float r = startRect.Right + (float)easingEnum.Ease(t) * (endRect.Right - startRect.Right);
                float b = startRect.Bottom + (float)easingEnum.Ease(t) * (endRect.Bottom - startRect.Bottom);
                _w.RealTime = r - _x.RealTime;
                _h.RealTime = b - _y.RealTime;
            }

            if (ms >= _movTime.Max)
            {
                _x.RealTimeToTarget();
                _y.RealTimeToTarget();
                _w.RealTimeToTarget();
                _h.RealTimeToTarget();
            }
        }

        /// <summary>
        /// todo: Still have bugs.
        /// </summary>
        public void FreeCutRect(EasingType easingEnum, int startTime, int endTime, Mathe.RawRectangleF startRect,
            Mathe.RawRectangleF endRect)
        {
            if (_inRectTime.Max == int.MaxValue || endTime > _inRectTime.Max)
            {
                _inRectTime.Max = endTime;
                _inX.Target = endRect.Left;
                _inY.Target = endRect.Top;
                _inW.Target = endRect.Right - endRect.Left;
                _inH.Target = endRect.Bottom - endRect.Top;
            }

            if (_inRectTime.Min == int.MinValue || startTime < _inRectTime.Min)
            {
                _inRectTime.Min = startTime;
                _inX.Source = startRect.Left;
                _inY.Source = startRect.Top;
                _inW.Source = startRect.Right - startRect.Left;
                _inH.Source = startRect.Bottom - startRect.Top;
            }

            float ms = Offset;
            if (!IsFinished && ms <= _inRectTime.Min)
            {
                _inX.RealTimeToSource();
                _inY.RealTimeToSource();
                _inW.RealTimeToSource();
                _inH.RealTimeToSource();
            }

            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                _inX.RealTime = startRect.Left + (float)easingEnum.Ease(t) * (endRect.Left - startRect.Left);
                _inY.RealTime = startRect.Top + (float)easingEnum.Ease(t) * (endRect.Top - startRect.Top);
                float r = startRect.Right + (float)easingEnum.Ease(t) * (endRect.Right - startRect.Right);
                float b = startRect.Bottom + (float)easingEnum.Ease(t) * (endRect.Bottom - startRect.Bottom);
                _inW.RealTime = r - _inX.RealTime;
                _inH.RealTime = b - _inY.RealTime;
            }

            if (ms >= _inRectTime.Max)
            {
                _inX.RealTimeToTarget();
                _inY.RealTimeToTarget();
                _inW.RealTimeToTarget();
                _inH.RealTimeToTarget();
            }
        }

        public void StartDraw()
        {
            if (!IsStarted)
            {
                if (_innerWatch)
                    _watch.Start();
                IsStarted = true;
            }
            else
            {
                if (Offset >= MaxTime)
                {
                    if (_innerWatch)
                    {
                        _watch.Stop();
                        _watch.Reset();
                    }
                    IsFinished = true;
                    //if (EnableLog) LogUtil.LogInfo("finished");
                }
            }

            Offset = _timeOffset + _watch.ElapsedMilliseconds;
        }

        public void EndDraw()
        {
            if (!IsFinished)
            {
                if (Offset >= MinTime)
                {
                    Target.Transform = Matrix3x2.Translation(-_x.RealTime, -_y.RealTime) *
                                       Matrix3x2.Scaling(_vx.RealTime, _vy.RealTime) *
                                       Matrix3x2.Rotation(_r.RealTime) *
                                       Matrix3x2.Translation(_x.RealTime, _y.RealTime);
                    if (_f.RealTime > 0)
                    {
                        Target.DrawBitmap(Bitmap,
                            new RectangleF(Rect.RealTime.Left - _originOffsetX, Rect.RealTime.Top - _originOffsetY,
                                Rect.RealTime.Right - Rect.RealTime.Left, Rect.RealTime.Bottom - Rect.RealTime.Top),
                            _f.RealTime, D2D.BitmapInterpolationMode.Linear);
#if DEBUG
                        //Target.DrawRectangle(Rect.RealTime, _redBrush, 1);
#endif
                    }
                    Target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
                }
            }
            else
            {
                //Dispose();
                //                Target.Transform = Matrix3x2.Translation(-_x.RealTime - _originOffsetX, -_y.RealTime - _originOffsetY) *
                //                                   Matrix3x2.Scaling(_vx.RealTime, _vy.RealTime) *
                //                                   Matrix3x2.Rotation(_r.RealTime) *
                //                                   Matrix3x2.Translation(_x.RealTime + _originOffsetX, _y.RealTime + _originOffsetY);
                //                //if (EnableLog) LogUtil.LogInfo(string.Format("[{0},{1},{2},{3}]", InRect.RealTime.Left,
                //                //    InRect.RealTime.Top, InRect.RealTime.Right, InRect.RealTime.Bottom));
                //                //Target.FillOpacityMask(Bitmap, _brush, D2D.OpacityMaskContent.TextGdiCompatible, Rect.Target, null);
                //                if (_f.Target > 0)
                //                {
                //                    Target.DrawBitmap(Bitmap, Rect.Target, _f.Target, D2D.BitmapInterpolationMode.Linear/*, TarInRect*/); //todo: bug
                //#if DEBUG
                //                    Target.DrawRectangle(Rect.Target, _redBrush, 1);
                //#endif
                //                }
                //                Target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
            }
        }

        public BitmapObject Reset(OriginType origin, Mathe.RawPoint posision) =>
            new BitmapObject(Target, Bitmap, origin, posision, EnableLog);

        private int? _preMs;
        private Queue<int> _ms = new Queue<int>();

        public void AdjustTime(int ms)
        {
            if (!_innerWatch) return;
            if (_ms.Count >= 20)
                _ms.Dequeue();
            _ms.Enqueue(ms);
            //if (_pausedMs == null || _pausedMs == ms)
            if (ms != _preMs || _ms.All(q => q == _ms.Average()))
            {
                _preMs = ms;
                if (Math.Abs(Offset - ms) <= 3)
                    return;

                if (EnableLog) LogUtil.LogInfo($"OFFSET CORRECTION: {Offset}>{ms}");

                if (ms < MaxTime)
                    IsFinished = false;
                _watch.Restart();
                _timeOffset = ms;
                Offset = _timeOffset + _watch.ElapsedMilliseconds;
            }
        }

        public void Dispose()
        {
            //Target?.Dispose();
            Bitmap?.Dispose();
#if DEBUG
            _redBrush?.Dispose();
#endif
        }

    }
}
