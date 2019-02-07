using OSharp.Storyboard;
using OSharp.Storyboard.Common;
using OSharp.Storyboard.Events;
using SharpDX;
using System;
using System.IO;
using System.Linq;
using Milky.OsuPlayer.Media.Storyboard.Util;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;

namespace Milky.OsuPlayer.Media.Storyboard.Animation
{
    public class StoryboardObject : IDisposable
    {
        public D2D.Bitmap[] TextureList;
        public D2D.Bitmap Texture;

        public Size2F Size => Texture.Size;
        public float Width => Size.Width;
        public float Height => Size.Height;

        public OriginType Origin { get; set; }
        public float OriginOffsetX;
        public float OriginOffsetY;

        public TimeRange FadeTime = TimeRange.Default;
        public TimeRange RotateTime = TimeRange.Default;
        public TimeRange VTime = TimeRange.Default;
        public TimeRange MovTime = TimeRange.Default;
        public TimeRange PTime = TimeRange.Default;
        public TimeRange CTime = TimeRange.Default;
        public int MaxTime => TimeRange.GetMaxTime(FadeTime, RotateTime, MovTime, VTime, PTime, CTime);
        public int MinTime => TimeRange.GetMinTime(FadeTime, RotateTime, MovTime, VTime, PTime, CTime);

        public Static<float> F;
        public Static<float> X, Y, W, H;
        public Static<float> Rad, Vx, Vy;
        public Static<float> R, G, B;
        public Static<Mathe.RawRectangleF> Rect => new Static<Mathe.RawRectangleF>
        {
            Source = new Mathe.RawRectangleF(X.Source, Y.Source, X.Source + W.Source, Y.Source + H.Source),
            RealTime =
                new Mathe.RawRectangleF(X.RealTime, Y.RealTime, X.RealTime + W.RealTime, Y.RealTime + H.RealTime),
            Target = new Mathe.RawRectangleF(X.Target, Y.Target, X.Target + W.Target, Y.Target + H.Target)
        };

        public bool UseH, UseV, UseA; //todo

        public bool IsFinished => _timing.Offset > MaxTime;

        private readonly Element _element;
        private readonly Timing _timing;
        private Size2F _vSize;

        public StoryboardObject(Element element, Timing timing, D2D.RenderTarget target, Size2F vSize)
        {
            _timing = timing;
            _element = element;
            _vSize = vSize;
            var path = Path.Combine(App.StoryboardProvider.Directory, element.ImagePath);
            if (!File.Exists(path))
            {
                var pngPath = path + ".png";
                if (File.Exists(pngPath))
                {
                    path = pngPath;
                }
                else
                {
                    path = path + ".jpg";
                }
            }

            if (File.Exists(path))
            {
                TextureList = new[] { TextureLoader.LoadBitmap(target, path) };
                Texture = TextureList[0];
            }
            else
                return;

            SetDefaultValue();
            SetMinMax();
        }

        protected void SetMinMax()
        {
            // 与bitmapObject不同，由于sb是纯静态的，所以可先判断出范围，为提高效率，这里先进行计算
            if (_element.FadeList.Any())
            {
                FadeTime = new TimeRange
                {
                    Min = (int)_element.FadeList.Min(k => k.StartTime),
                    Max = (int)_element.FadeList.Max(k => k.EndTime),
                };
                F.Source = _element.FadeList.First().StartOpacity;
                F.Target = _element.FadeList.Last().EndOpacity;
            }
            if (_element.RotateList.Any())
            {
                RotateTime = new TimeRange
                {
                    Min = (int)_element.RotateList.Min(k => k.StartTime),
                    Max = (int)_element.RotateList.Max(k => k.EndTime),
                };
                Rad.Source = _element.RotateList.First().StartRotate;
                Rad.Target = _element.RotateList.Last().EndRotate;
            }

            if (_element.ScaleList.Any())
            {
                VTime = new TimeRange
                {
                    Min = (int)_element.ScaleList.Min(k => k.StartTime),
                    Max = (int)_element.ScaleList.Max(k => k.EndTime)
                };
                Vx.Source = _element.ScaleList.First().StartScale * _vSize.Width;
                Vy.Source = _element.ScaleList.First().StartScale * _vSize.Width;
                Vx.Target = _element.ScaleList.Last().EndScale * _vSize.Height;
                Vy.Target = _element.ScaleList.Last().EndScale * _vSize.Height;
            }
            if (_element.VectorList.Any())
            {
                if (!VTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.VectorList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.VectorList.Max(k => k.EndTime);

                    if (tmpMin < VTime.Min)
                    {
                        VTime.Min = tmpMin;
                        Vx.Source = _element.VectorList.First().StartScaleX * _vSize.Width;
                        Vy.Source = _element.VectorList.First().StartScaleY * _vSize.Height;
                    }

                    if (tmpMax > VTime.Max)
                    {
                        VTime.Max = tmpMax;
                        Vx.Target = _element.VectorList.Last().EndScaleX * _vSize.Width;
                        Vy.Target = _element.VectorList.Last().EndScaleY * _vSize.Height;
                    }
                }
                else
                {
                    VTime = new TimeRange
                    {
                        Min = (int)_element.VectorList.Min(k => k.StartTime),
                        Max = (int)_element.VectorList.Max(k => k.EndTime)
                    };
                    Vx.Source = _element.VectorList.First().StartScaleX * _vSize.Width;
                    Vy.Source = _element.VectorList.First().StartScaleY * _vSize.Height;
                    Vx.Target = _element.VectorList.Last().EndScaleX * _vSize.Width;
                    Vy.Target = _element.VectorList.Last().EndScaleY * _vSize.Height;
                }
            }

            if (_element.MoveList.Any())
            {
                MovTime = new TimeRange
                {
                    Min = (int)_element.MoveList.Min(k => k.StartTime),
                    Max = (int)_element.MoveList.Max(k => k.EndTime)
                };
                X.Source = (_element.MoveList.First().StartX + 107) * _vSize.Width;
                Y.Source = _element.MoveList.First().StartY * _vSize.Height;
                X.Target = (_element.MoveList.Last().EndX + 107) * _vSize.Width;
                Y.Target = _element.MoveList.Last().EndY * _vSize.Height;
            }
            if (_element.MoveXList.Any())
            {
                if (!MovTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveXList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveXList.Max(k => k.EndTime);

                    if (tmpMin < MovTime.Min)
                    {
                        MovTime.Min = tmpMin;
                        X.Source = (_element.MoveXList.First().StartX + 107) * _vSize.Width;
                    }

                    if (tmpMax > MovTime.Max)
                    {
                        MovTime.Max = tmpMax;
                        X.Target = (_element.MoveXList.Last().EndX + 107) * _vSize.Width;
                    }
                }
                else
                {
                    MovTime = new TimeRange
                    {
                        Min = (int)_element.MoveXList.Min(k => k.StartTime),
                        Max = (int)_element.MoveXList.Max(k => k.EndTime)
                    };
                    X.Source = (_element.MoveXList.First().StartX + 107) * _vSize.Width;
                    X.Target = (_element.MoveXList.Last().EndX + 107) * _vSize.Width;
                }
            }
            if (_element.MoveYList.Any())
            {
                if (!MovTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveYList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveYList.Max(k => k.EndTime);

                    if (tmpMin <= MovTime.Min)
                    {
                        MovTime.Min = tmpMin;
                        Y.Source = _element.MoveYList.First().StartY * _vSize.Height;
                    }

                    if (tmpMax >= MovTime.Max)
                    {
                        MovTime.Max = tmpMax;
                        Y.Target = _element.MoveYList.Last().EndY * _vSize.Height;
                    }
                }
                else
                {
                    MovTime = new TimeRange
                    {
                        Min = (int)_element.MoveYList.Min(k => k.StartTime),
                        Max = (int)_element.MoveYList.Max(k => k.EndTime)
                    };
                    Y.Source = _element.MoveYList.First().StartY * _vSize.Height;
                    Y.Target = _element.MoveYList.Last().EndY * _vSize.Height;
                }
            }

            if (_element.ColorList.Any())
            {
                CTime = new TimeRange
                {
                    Min = (int)_element.ColorList.Min(k => k.StartTime),
                    Max = (int)_element.ColorList.Max(k => k.EndTime)
                };
                R.Source = _element.ColorList.First().StartR;
                G.Source = _element.ColorList.First().StartG;
                B.Source = _element.ColorList.First().StartB;
                R.Target = _element.ColorList.Last().EndR;
                G.Target = _element.ColorList.Last().EndG;
                B.Target = _element.ColorList.Last().EndB;

            }

            if (_element.ParameterList.Any())
            {
                PTime = new TimeRange
                {
                    Min = (int)_element.ParameterList.Min(k => k.StartTime),
                    Max = (int)_element.ParameterList.Max(k => k.EndTime)
                };
            }

            ResetRealTime();
        }

        public void ResetRealTime()
        {
            X.RealTimeToSource();
            Y.RealTimeToSource();
            Rad.RealTimeToSource();
            Vx.RealTimeToSource();
            Vy.RealTimeToSource();
            F.RealTimeToSource();
            R.RealTimeToSource();
            G.RealTimeToSource();
            B.RealTimeToSource();
        }

        protected void SetDefaultValue()
        {
            Origin = _element.Origin;

            X = (Static<float>)((_element.DefaultX + 107) * _vSize.Width);
            Y = (Static<float>)(_element.DefaultY * _vSize.Height);
            F = (Static<float>)1;
            Rad = (Static<float>)0;
            Vx = (Static<float>)(1 * _vSize.Width);
            Vy = (Static<float>)(1 * _vSize.Height);
            R = (Static<float>)255;
            G = (Static<float>)255;
            B = (Static<float>)255;

            var sb = _element.ParameterList.GroupBy(k => k.Type);
            foreach (var kv in sb)
            {
                var array = kv.ToArray();
                var flag = array.Length > 0 && array[0].StartTime == array[0].EndTime;
                switch (kv.Key)
                {
                    case ParameterType.Horizontal:
                        UseH = flag;
                        break;
                    case ParameterType.Vertical:
                        UseV = flag;
                        break;
                    case ParameterType.Additive:
                        UseA = flag;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // rects
            W = (Static<float>)Texture.Size.Width;
            H = (Static<float>)Texture.Size.Height;

            //origin
            switch (_element.Origin)
            {
                case OriginType.BottomLeft:
                    OriginOffsetX = 0;
                    OriginOffsetY = Height;
                    break;
                case OriginType.BottomCentre:
                    OriginOffsetX = Width / 2;
                    OriginOffsetY = Height;
                    break;
                case OriginType.BottomRight:
                    OriginOffsetX = Width;
                    OriginOffsetY = Height;
                    break;
                case OriginType.CentreLeft:
                    OriginOffsetX = 0;
                    OriginOffsetY = Height / 2;
                    break;
                case OriginType.Centre:
                    OriginOffsetX = Width / 2;
                    OriginOffsetY = Height / 2;
                    break;
                case OriginType.CentreRight:
                    OriginOffsetX = Width;
                    OriginOffsetY = Height / 2;
                    break;
                case OriginType.TopLeft:
                    OriginOffsetX = 0;
                    OriginOffsetY = 0;
                    break;
                case OriginType.TopCentre:
                    OriginOffsetX = Width / 2;
                    OriginOffsetY = 0;
                    break;
                case OriginType.TopRight:
                    OriginOffsetX = Width;
                    OriginOffsetY = 0;
                    break;
            }
        }

        public void Move(EasingType easingType, int startTime, int endTime, Gdip.PointF startPoint, Gdip.PointF endPoint)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                X.RealTime = startPoint.X + (float)easingType.Ease(t) * (endPoint.X - startPoint.X);
                Y.RealTime = startPoint.Y + (float)easingType.Ease(t) * (endPoint.Y - startPoint.Y);
            }

            if (ms >= MovTime.Max)
            {
                X.RealTimeToTarget();
                Y.RealTimeToTarget();
            }
        }

        public void MoveX(EasingType easingEnum, int startTime, int endTime, float startX, float endX)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                X.RealTime = startX + (float)easingEnum.Ease(t) * (endX - startX);
            }

            if (ms >= MovTime.Max)
                X.RealTimeToTarget();
        }

        public void MoveY(EasingType easingEnum, int startTime, int endTime, float startY, float endY)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Y.RealTime = startY + (float)easingEnum.Ease(t) * (endY - startY);
            }

            if (ms >= MovTime.Max)
                Y.RealTimeToTarget();
        }

        public void Rotate(EasingType easingEnum, int startTime, int endTime, float startRad, float endRad)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Rad.RealTime = startRad + (float)easingEnum.Ease(t) * (endRad - startRad);
            }

            if (ms >= RotateTime.Max)
            {
                Rad.RealTimeToTarget();
            }
        }

        public void FlipH(int startTime, int endTime)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
                UseH = true;
            if (ms >= endTime && startTime != endTime)
                UseH = false;
        }

        public void FlipV(int startTime, int endTime)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
                UseV = true;
            if (ms >= endTime && startTime != endTime)
                UseV = false;
        }
        /// <summary>
        /// Do not use with SCALE or FREERECT at same time!
        /// </summary>
        public void ScaleVec(EasingType easingEnum, int startTime, int endTime, float startVx, float startVy, float endVx,
            float endVy)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Vx.RealTime = startVx + (float)easingEnum.Ease(t) * (endVx - startVx);
                Vy.RealTime = startVy + (float)easingEnum.Ease(t) * (endVy - startVy);
            }

            if (ms >= VTime.Max)
            {
                Vx.RealTimeToTarget();
                Vy.RealTimeToTarget();
            }
        }

        public void Fade(EasingType easingEnum, int startTime, int endTime, float startOpacity, float endOpacity)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                F.RealTime = startOpacity + (float)easingEnum.Ease(t) * (endOpacity - startOpacity);
            }

            if (ms >= FadeTime.Max)
            {
                F.RealTimeToTarget();
            }
        }

        public void Color(EasingType easingEnum, int startTime, int endTime, float r1, float g1, float b1, float r2, float g2, float b2)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                R.RealTime = r1 + (float)easingEnum.Ease(t) * (r2 - r1);
                G.RealTime = g1 + (float)easingEnum.Ease(t) * (g2 - g1);
                B.RealTime = b1 + (float)easingEnum.Ease(t) * (b2 - b1);
            }

            if (ms >= CTime.Max)
            {
                R.RealTimeToTarget();
                G.RealTimeToTarget();
                B.RealTimeToTarget();
            }
        }

        public void Additive(int startTime, int endTime)
        {
            // todo: Not implemented
        }

        public void Dispose()
        {
            if (TextureList == null) return;
            foreach (var b in TextureList)
                b?.Dispose();
            TextureList = null;
            Texture = null;
        }
    }
}
