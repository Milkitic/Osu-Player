using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Utils;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest.DxAnimation
{
    internal class ElementObject : AnimationObject, IDisposable
    {
        public Size2F Size => CurrentBitmap.Size;
        public float Width => Size.Width;
        public float Height => Size.Height;
        private OriginType Origin { get; set; }

        protected readonly D2D.RenderTarget Target;
        protected D2D.Bitmap[] Bitmaps;
        protected D2D.Bitmap CurrentBitmap;

        // control
        protected readonly bool EnableLog;
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        protected readonly BackgroundLayer.Timing Timing;
        private readonly bool _innerWatch = true;
        private readonly Element _element;

        //public long Offset;
        private long _timeOffset;

        private float _originOffsetX;
        private float _originOffsetY;

        // debug
#if DEBUG
        private readonly D2D.Brush _redBrush;
#endif

        #region Private statics

        private TimeRange _fadeTime = TimeRange.Default;
        private TimeRange _rotateTime = TimeRange.Default;
        private TimeRange _vTime = TimeRange.Default;
        private TimeRange _movTime = TimeRange.Default;
        private TimeRange _pTime = TimeRange.Default;
        private TimeRange _cTime = TimeRange.Default;

        private Static<float> _f;
        private Static<float> _x, _y, _w, _h;
        private Static<float> _r, _vx, _vy;
        private Static<float> _cR, _cG, _cB;

        private bool _useH, _useV, _useA; //todo

        public int MaxTime => TimeRange.GetMaxTime(_fadeTime, _rotateTime, _movTime, _vTime, _pTime, _cTime);
        public int MinTime => TimeRange.GetMinTime(_fadeTime, _rotateTime, _movTime, _vTime, _pTime, _cTime);

        private Static<Mathe.RawRectangleF> Rect => new Static<Mathe.RawRectangleF>
        {
            Source = new Mathe.RawRectangleF(_x.Source, _y.Source, _x.Source + _w.Source, _y.Source + _h.Source),
            RealTime =
                new Mathe.RawRectangleF(_x.RealTime, _y.RealTime, _x.RealTime + _w.RealTime, _y.RealTime + _h.RealTime),
            Target = new Mathe.RawRectangleF(_x.Target, _y.Target, _x.Target + _w.Target, _y.Target + _h.Target)
        };

        #endregion private statics

        public ElementObject(D2D.RenderTarget target, Element element, bool enableLog = false)
            : this(target, element, null, enableLog)
        {
        }

        public ElementObject(D2D.RenderTarget target, Element element, BackgroundLayer.Timing timing,
            bool enableLog = false)
        {
            _element = element;
            Target = target;
#if DEBUG
            _redBrush = new D2D.SolidColorBrush(target, new Mathe.RawColor4(1, 0, 0, 1));
#endif
            EnableLog = enableLog;

            if (timing != null)
            {
                Timing = timing;
                _innerWatch = false;
            }
            else
                Timing = new BackgroundLayer.Timing(0, new Stopwatch());

            var path = Path.Combine(Program.Fi.Directory.FullName, element.ImagePath);
            if (File.Exists(path))
            {
                Bitmaps = new[] { Loader.LoadBitmap(target, path) };
                CurrentBitmap = Bitmaps[0];
            }
            else
                return;
            SetDefaultValue();
            SetMinMax();
        }

        protected void SetMinMax()
        {
            // 与bitmapObject不同，由于sb是纯静态的，所以可先判断出范围，为提高效率，这里先进行计算
            if (_element.FadeList.Count > 0)
            {
                _fadeTime = new TimeRange
                {
                    Min = (int)_element.FadeList.Min(k => k.StartTime),
                    Max = (int)_element.FadeList.Max(k => k.EndTime),
                };
                _f.Source = _element.FadeList.First().F1;
                _f.Target = _element.FadeList.Last().F2;
            }
            if (_element.RotateList.Count > 0)
            {
                _rotateTime = new TimeRange
                {
                    Min = (int)_element.RotateList.Min(k => k.StartTime),
                    Max = (int)_element.RotateList.Max(k => k.EndTime),
                };
                _r.Source = _element.RotateList.First().R1;
                _r.Target = _element.RotateList.Last().R2;
            }

            if (_element.ScaleList.Count > 0)
            {
                _vTime = new TimeRange
                {
                    Min = (int)_element.ScaleList.Min(k => k.StartTime),
                    Max = (int)_element.ScaleList.Max(k => k.EndTime)
                };
                _vx.Source = _element.ScaleList.First().S1;
                _vy.Source = _element.ScaleList.First().S1;
                _vx.Target = _element.ScaleList.Last().S2;
                _vy.Target = _element.ScaleList.Last().S2;
            }
            if (_element.VectorList.Count > 0)
            {
                if (!_vTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.VectorList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.VectorList.Max(k => k.EndTime);

                    if (tmpMin < _vTime.Min)
                    {
                        _vTime.Min = tmpMin;
                        _vx.Source = _element.VectorList.First().Vx1;
                        _vy.Source = _element.VectorList.First().Vy1;
                    }

                    if (tmpMax > _vTime.Max)
                    {
                        _vTime.Max = tmpMax;
                        _vx.Target = _element.VectorList.Last().Vx2;
                        _vy.Target = _element.VectorList.Last().Vy2;
                    }
                }
                else
                {
                    _vTime = new TimeRange
                    {
                        Min = (int)_element.VectorList.Min(k => k.StartTime),
                        Max = (int)_element.VectorList.Max(k => k.EndTime)
                    };
                    _vx.Source = _element.VectorList.First().Vx1;
                    _vy.Source = _element.VectorList.First().Vy1;
                    _vx.Target = _element.VectorList.Last().Vx2;
                    _vy.Target = _element.VectorList.Last().Vy2;
                }
            }

            if (_element.MoveList.Count > 0)
            {
                _movTime = new TimeRange
                {
                    Min = (int)_element.MoveList.Min(k => k.StartTime),
                    Max = (int)_element.MoveList.Max(k => k.EndTime)
                };
                _x.Source = _element.MoveList.First().X1 + 107;
                _y.Source = _element.MoveList.First().Y1;
                _x.Target = _element.MoveList.Last().X2 + 107;
                _y.Target = _element.MoveList.Last().Y2;
            }
            if (_element.MoveXList.Count > 0)
            {
                if (!_movTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveXList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveXList.Max(k => k.EndTime);

                    if (tmpMin < _movTime.Min)
                    {
                        _movTime.Min = tmpMin;
                        _x.Source = _element.MoveXList.First().X1 + 107;
                    }

                    if (tmpMax > _movTime.Max)
                    {
                        _movTime.Max = tmpMax;
                        _x.Target = _element.MoveXList.Last().X2 + 107;
                    }
                }
                else
                {
                    _movTime = new TimeRange
                    {
                        Min = (int)_element.MoveXList.Min(k => k.StartTime),
                        Max = (int)_element.MoveXList.Max(k => k.EndTime)
                    };
                    _x.Source = _element.MoveXList.First().X1 + 107;
                    _x.Target = _element.MoveXList.Last().X2 + 107;
                }
            }
            if (_element.MoveYList.Count > 0)
            {
                if (!_movTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveYList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveYList.Max(k => k.EndTime);

                    if (tmpMin <= _movTime.Min)
                    {
                        _movTime.Min = tmpMin;
                        _y.Source = _element.MoveYList.First().Y1;
                    }

                    if (tmpMax >= _movTime.Max)
                    {
                        _movTime.Max = tmpMax;
                        _y.Target = _element.MoveYList.Last().Y2;
                    }
                }
                else
                {
                    _movTime = new TimeRange
                    {
                        Min = (int)_element.MoveYList.Min(k => k.StartTime),
                        Max = (int)_element.MoveYList.Max(k => k.EndTime)
                    };
                    _y.Source = _element.MoveYList.First().Y1;
                    _y.Target = _element.MoveYList.Last().Y2;
                }
            }

            if (_element.ColorList.Count > 0)
            {
                _cTime = new TimeRange
                {
                    Min = (int)_element.ColorList.Min(k => k.StartTime),
                    Max = (int)_element.ColorList.Max(k => k.EndTime)
                };
                _cR.Source = _element.ColorList.First().R1;
                _cG.Source = _element.ColorList.First().G1;
                _cB.Source = _element.ColorList.First().B1;
                _cR.Target = _element.ColorList.Last().R2;
                _cG.Target = _element.ColorList.Last().G2;
                _cB.Target = _element.ColorList.Last().B2;
            }

            if (_element.ParameterList.Count > 0)
            {
                _pTime = new TimeRange
                {
                    Min = (int)_element.ParameterList.Min(k => k.StartTime),
                    Max = (int)_element.ParameterList.Max(k => k.EndTime)
                };
            }

            _x.RealTimeToSource();
            _y.RealTimeToSource();
            _r.RealTimeToSource();
            _vx.RealTimeToSource();
            _vy.RealTimeToSource();
            _f.RealTimeToSource();
        }

        protected void SetDefaultValue()
        {
            Origin = _element.Origin;

            _x = (Static<float>)(_element.DefaultX + 107);
            _y = (Static<float>)_element.DefaultY;
            _f = (Static<float>)1;
            _r = (Static<float>)0;
            _vx = (Static<float>)1;
            _vy = (Static<float>)1;

            _useH = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Horizontal);
            _useV = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Vertical);
            _useA = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Additive);

            // rects
            _w = (Static<float>)CurrentBitmap.Size.Width;
            _h = (Static<float>)CurrentBitmap.Size.Height;

            //origion
            switch (_element.Origin)
            {
                case OriginType.BottomLeft:
                    _originOffsetX = 0;
                    _originOffsetY = Height;
                    break;
                case OriginType.BottomCentre:
                    _originOffsetX = Width / 2;
                    _originOffsetY = Height;
                    break;
                case OriginType.BottomRight:
                    _originOffsetX = Width;
                    _originOffsetY = Height;
                    break;
                case OriginType.CentreLeft:
                    _originOffsetX = 0;
                    _originOffsetY = Height / 2;
                    break;
                case OriginType.Centre:
                    _originOffsetX = Width / 2;
                    _originOffsetY = Height / 2;
                    break;
                case OriginType.CentreRight:
                    _originOffsetX = Width;
                    _originOffsetY = Height / 2;
                    break;
                case OriginType.TopLeft:
                    _originOffsetX = 0;
                    _originOffsetY = 0;
                    break;
                case OriginType.TopCentre:
                    _originOffsetX = Width / 2;
                    _originOffsetY = 0;
                    break;
                case OriginType.TopRight:
                    _originOffsetX = Width;
                    _originOffsetY = 0;
                    break;
            }
        }

        /// <summary>
        /// Do not use with MOVEX or MOVEY at same time!
        /// </summary>
        public override void Move(EasingType easingEnum, int startTime, int endTime, Gdip.PointF startPoint, Gdip.PointF endPoint)
        {
            float ms = Timing.Offset;
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
            float ms = Timing.Offset;
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
            float ms = Timing.Offset;
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
            float ms = Timing.Offset;
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

        public void FlipH(int startTime, int endTime)
        {
            float ms = Timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
                _useH = true;
            if (ms >= endTime && startTime != endTime)
                _useH = false;
        }

        public void FlipV(int startTime, int endTime)
        {
            float ms = Timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
                _useV = true;
            if (ms >= endTime && startTime != endTime)
                _useV = false;
        }
        /// <summary>
        /// Do not use with SCALE or FREERECT at same time!
        /// </summary>
        public override void ScaleVec(EasingType easingEnum, int startTime, int endTime, float startVx, float startVy, float endVx,
            float endVy)
        {
            float ms = Timing.Offset;
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
            float ms = Timing.Offset;
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

        public void Color(EasingType easingEnum, int startTime, int endTime, float r1, float g1, float b1, float r2, float g2, float b2)
        {
            // Todo: Not implemented
        }

        public void Additive(int startTime, int endTime)
        {
            // Todo: Not implemented
        }

        public void StartDraw()
        {
            if (!IsStarted)
            {
                if (_innerWatch)
                    Timing.Watch.Start();
                IsStarted = true;
            }
            else if (IsStarted && Timing.Offset >= MaxTime)
            {
                IsFinished = true;

                if (_innerWatch)
                {
                    Timing.Watch.Stop();
                    Timing.Watch.Reset();
                }
            }
        }

        public virtual void EndDraw()
        {
            if (IsFinished || Timing.Offset < MinTime || _f.RealTime <= 0) return;

            Target.Transform = Matrix3x2.Translation(-_x.RealTime, -_y.RealTime);
            Target.Transform = Target.Transform
                               * Matrix3x2.Scaling((_useH ? -1 : 1) * _vx.RealTime, (_useV ? -1 : 1) * _vy.RealTime)
                               * Matrix3x2.Rotation(_r.RealTime);
            Target.Transform = Target.Transform * Matrix3x2.Translation(_x.RealTime, _y.RealTime);

            float rectL = Rect.RealTime.Left - _originOffsetX - (_useH ? 2 * (Width / 2 - _originOffsetX) : 0);
            float rectT = Rect.RealTime.Top - _originOffsetY - (_useV ? 2 * (Height / 2 - _originOffsetY) : 0);
            float rectW = Rect.RealTime.Right - Rect.RealTime.Left;
            float rectH = Rect.RealTime.Bottom - Rect.RealTime.Top;

            var realRect = new RectangleF(rectL, rectT, rectW, rectH);
            if (CurrentBitmap != null)
                Target.DrawBitmap(CurrentBitmap, realRect, _f.RealTime, D2D.BitmapInterpolationMode.Linear);
#if DEBUG
            //Target.DrawRectangle(realRect, _redBrush, 1);
#endif
            Target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
        }

        private int? _preMs;
        private Queue<int> _ms = new Queue<int>();

        public void AdjustTime(int ms)
        {
            if (_ms.Count >= 20)
                _ms.Dequeue();
            _ms.Enqueue(ms);

            if (!_innerWatch)
            {
                _preMs = ms;
                if (Math.Abs(Timing.Offset - ms) <= 3)
                    return;
                if (EnableLog) LogUtil.LogInfo($"OFFSET CORRECTION: {Timing.Offset}>{ms}");
                if (ms < MaxTime)
                    IsFinished = false;
                _timeOffset = ms;
                return;
            }

            //if (_pausedMs == null || _pausedMs == ms)
            if (ms != _preMs || _ms.All(q => q == _ms.Average()))
            {
                _preMs = ms;
                if (Math.Abs(Timing.Offset - ms) <= 3)
                    return;

                if (EnableLog) LogUtil.LogInfo($"OFFSET CORRECTION: {Timing.Offset}>{ms}");

                if (ms < MaxTime)
                    IsFinished = false;
                Timing.Watch.Restart();
                _timeOffset = ms;
            }
        }

        public void Dispose()
        {
            if (Bitmaps != null)
                foreach (var b in Bitmaps)
                    b?.Dispose();
#if DEBUG
            _redBrush?.Dispose();
#endif
        }
    }
}
