using LibOsb;
using LibOsb.Enums;
using LibOsb.Utils;
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
        public Size2F Size => Bitmap.Size;
        public float Width => Size.Width;
        public float Height => Size.Height;
        private OriginType Origin { get; set; }

        protected readonly D2D.RenderTarget Target;
        protected readonly D2D.Bitmap Bitmap;

        // control
        protected readonly bool EnableLog;
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        private readonly Stopwatch _watch;
        private readonly bool _innerWatch = true;
        private readonly Element _element;

        public long Offset;
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
        private Static<float> _f;
        private Static<float> _x, _y, _w, _h;
        private Static<float> _r, _vx, _vy;

        private bool[] _start = new bool[6];
        private bool[] _end = new bool[6];

        public int MaxTime => TimeRange.GetMaxTime(_fadeTime, _rotateTime, _movTime, _vTime);
        public int MinTime => TimeRange.GetMinTime(_fadeTime, _rotateTime, _movTime, _vTime);

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

        public ElementObject(D2D.RenderTarget target, Element element, Stopwatch sw,
            bool enableLog = false)
        {
            _element = element;
            Target = target;
#if DEBUG
            _redBrush = new D2D.SolidColorBrush(target, new Mathe.RawColor4(1, 0, 0, 1));
#endif
            Bitmap = Loader.LoadBitmap(target, Path.Combine(Program.Fi.Directory.FullName, element.ImagePath));

            SetDefaultValue();
            SetMinMax();
            if (sw != null)
            {
                _watch = sw;
                _innerWatch = false;
            }
            else
                _watch = new Stopwatch();

            EnableLog = enableLog;
        }

        private void SetMinMax()
        {
            // 与bitmapObject不同，由于sb是纯静态的，所以可先判断出范围，为提高效率，这里先进行计算
            if (_element.FadeList.Count > 0)
            {
                _fadeTime = new TimeRange
                {
                    Min = (int)_element.FadeList.Min(k => k.StartTime),
                    Max = (int)_element.FadeList.Max(k => k.EndTime),
                };
                _f.Source = _element.FadeList.First().Start;
                _f.Target = _element.FadeList.Last().End;
            }
            if (_element.RotateList.Count > 0)
            {
                _rotateTime = new TimeRange
                {
                    Min = (int)_element.RotateList.Min(k => k.StartTime),
                    Max = (int)_element.RotateList.Max(k => k.EndTime),
                };
                _r.Source = _element.RotateList.First().Start;
                _r.Target = _element.RotateList.Last().End;
            }

            if (_element.ScaleList.Count > 0)
            {
                _vTime = new TimeRange
                {
                    Min = (int)_element.ScaleList.Min(k => k.StartTime),
                    Max = (int)_element.ScaleList.Max(k => k.EndTime)
                };
                _vx.Source = _element.ScaleList.First().Start;
                _vy.Source = _element.ScaleList.First().Start;
                _vx.Target = _element.ScaleList.Last().End;
                _vy.Target = _element.ScaleList.Last().End;
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
                        _vx.Source = _element.VectorList.First().Start.x;
                        _vy.Source = _element.VectorList.First().Start.y;
                    }

                    if (tmpMax > _vTime.Max)
                    {
                        _vTime.Max = tmpMax;
                        _vx.Target = _element.VectorList.Last().End.x;
                        _vy.Target = _element.VectorList.Last().End.y;
                    }
                }
                else
                {
                    _vTime = new TimeRange
                    {
                        Min = (int)_element.VectorList.Min(k => k.StartTime),
                        Max = (int)_element.VectorList.Max(k => k.EndTime)
                    };
                    _vx.Source = _element.VectorList.First().Start.x;
                    _vy.Source = _element.VectorList.First().Start.y;
                    _vx.Target = _element.VectorList.Last().End.x;
                    _vy.Target = _element.VectorList.Last().End.y;
                }
            }

            if (_element.MoveList.Count > 0)
            {
                _movTime = new TimeRange
                {
                    Min = (int)_element.MoveList.Min(k => k.StartTime),
                    Max = (int)_element.MoveList.Max(k => k.EndTime)
                };
                _x.Source = _element.MoveList.First().Start.x + 107;
                _y.Source = _element.MoveList.First().Start.y;
                _x.Target = _element.MoveList.Last().End.x + 107;
                _y.Target = _element.MoveList.Last().End.y;
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
                        _x.Source = _element.MoveXList.First().Start + 107;
                    }

                    if (tmpMax > _movTime.Max)
                    {
                        _movTime.Max = tmpMax;
                        _x.Target = _element.MoveXList.Last().End + 107;
                    }
                }
                else
                {
                    _movTime = new TimeRange
                    {
                        Min = (int)_element.MoveXList.Min(k => k.StartTime),
                        Max = (int)_element.MoveXList.Max(k => k.EndTime)
                    };
                    _x.Source = _element.MoveXList.First().Start + 107;
                    _x.Target = _element.MoveXList.Last().End + 107;
                }
            }
            if (_element.MoveYList.Count > 0)
            {
                if (!_movTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveYList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveYList.Max(k => k.EndTime);

                    if (tmpMin < _movTime.Min)
                    {
                        _movTime.Min = tmpMin;
                        _y.Source = _element.MoveYList.First().Start;
                    }

                    if (tmpMax > _movTime.Max)
                    {
                        _movTime.Max = tmpMax;
                        _y.Target = _element.MoveYList.Last().End;
                    }
                }
                else
                {
                    _movTime = new TimeRange
                    {
                        Min = (int)_element.MoveYList.Min(k => k.StartTime),
                        Max = (int)_element.MoveYList.Max(k => k.EndTime)
                    };
                    _y.Source = _element.MoveYList.First().Start;
                    _y.Target = _element.MoveYList.Last().End;
                }
            }

            _x.RealTimeToSource();
            _y.RealTimeToSource();
            _r.RealTimeToSource();
            _vx.RealTimeToSource();
            _vy.RealTimeToSource();
            _f.RealTimeToSource();
        }

        private void SetDefaultValue()
        {
            Origin = _element.Origin;

            _x = (Static<float>)(_element.DefaultX + 107);
            _y = (Static<float>)_element.DefaultY;
            _f = (Static<float>)1;
            _r = (Static<float>)0;
            _vx = (Static<float>)1;
            _vy = (Static<float>)1;

            // rects
            _w = (Static<float>)Bitmap.Size.Width;
            _h = (Static<float>)Bitmap.Size.Height;

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
            const int i = 0;
            float ms = Offset;
            //if (!_start[i] && ms <= _movTime.Min)
            //{
            //    _start[i] = true;
            //    _x.RealTimeToSource();
            //    _y.RealTimeToSource();
            //}

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
            const int i = 1;
            float ms = Offset;
            //if (!_start[i] && !IsFinished && ms <= _movTime.Min)
            //{
            //    _start[i] = true;
            //    _x.RealTimeToSource();
            //}

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
            const int i = 2;
            float ms = Offset;
            //if (!_start[i] && !IsFinished && ms <= _movTime.Min)
            //{
            //    _start[i] = true;
            //    _y.RealTimeToSource();
            //}

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
            const int i = 3;
            float ms = Offset;
            //if (!_start[i] && !IsFinished && ms <= _rotateTime.Min)
            //{
            //    _start[i] = true;
            //    _r.RealTimeToSource();
            //}

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
            const int i = 4;
            float ms = Offset;
            //if (!_start[i] && !IsFinished && ms <= _vTime.Min)
            //{
            //    _start[i] = true;
            //    _vx.RealTimeToSource();
            //    _vy.RealTimeToSource();
            //}

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
            const int i = 5;
            float ms = Offset;
            //if (!_start[i] && !IsFinished && ms <= _fadeTime.Min)
            //{
            //    _start[i] = true;
            //    _f.RealTimeToSource();
            //}
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

        public void StartDraw()
        {
            if (!IsStarted)
            {
                if (_innerWatch)
                    _watch.Start();
                IsStarted = true;
            }
            else if (IsStarted && Offset >= MaxTime)
            {
                IsFinished = true;

                if (_innerWatch)
                {
                    _watch.Stop();
                    _watch.Reset();
                }
            }

            Offset = _timeOffset + _watch.ElapsedMilliseconds;
        }

        public void EndDraw()
        {
            if (IsFinished || Offset < MinTime || _f.RealTime <= 0) return;

            Target.Transform = Matrix3x2.Translation(-_x.RealTime, -_y.RealTime) *
                               Matrix3x2.Scaling(_vx.RealTime, _vy.RealTime) *
                               Matrix3x2.Rotation(_r.RealTime) *
                               Matrix3x2.Translation(_x.RealTime, _y.RealTime);

            Target.DrawBitmap(Bitmap,
                new RectangleF(Rect.RealTime.Left - _originOffsetX, Rect.RealTime.Top - _originOffsetY,
                    Rect.RealTime.Right - Rect.RealTime.Left, Rect.RealTime.Bottom - Rect.RealTime.Top),
                _f.RealTime, D2D.BitmapInterpolationMode.Linear);
#if DEBUG
            //Target.DrawRectangle(Rect.RealTime, _redBrush, 1);
#endif
            Target.Transform = new Matrix3x2(1, 0, 0, 1, 0, 0);
        }

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
            Bitmap?.Dispose();
#if DEBUG
            _redBrush?.Dispose();
#endif
        }
    }
}
