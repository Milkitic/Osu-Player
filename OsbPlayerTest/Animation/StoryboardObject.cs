using Milkitic.OsbLib;
using Milkitic.OsbLib.Enums;
using Milkitic.OsbLib.Utils;
using OsbPlayerTest.Layer;
using OsbPlayerTest.Util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = SharpDX.Direct2D1;
using Gdip = System.Drawing;
using Mathe = SharpDX.Mathematics.Interop;

namespace OsbPlayerTest.Animation
{
    public class StoryboardObject : IDisposable
    {
        public class TimeRanges
        {
            public TimeRange FadeTime = TimeRange.Default;
            public TimeRange RotateTime = TimeRange.Default;
            public TimeRange VTime = TimeRange.Default;
            public TimeRange MovTime = TimeRange.Default;
            public TimeRange PTime = TimeRange.Default;
            public TimeRange CTime = TimeRange.Default;
            public int MaxTime => TimeRange.GetMaxTime(FadeTime, RotateTime, MovTime, VTime, PTime, CTime);
            public int MinTime => TimeRange.GetMinTime(FadeTime, RotateTime, MovTime, VTime, PTime, CTime);
        }

        public class EventStatics
        {
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
        }

        public D2D.Bitmap[] TextureList;
        public D2D.Bitmap Texture;

        public Size2F Size => Texture.Size;
        public float Width => Size.Width;
        public float Height => Size.Height;
        public int MaxTime => Ranges.MaxTime;
        public int MinTime => Ranges.MinTime;

        public OriginType Origin { get; set; }
        public float OriginOffsetX;
        public float OriginOffsetY;

        public TimeRanges Ranges { get; set; } = new TimeRanges();
        public EventStatics Statics { get; set; } = new EventStatics();

        public bool UseH, UseV, UseA; //todo

        public bool IsFinished => _timing.Offset > MaxTime;

        private readonly Element _element;
        private readonly Timing _timing;

        public StoryboardObject(Element element, Timing timing, D2D.RenderTarget target)
        {
            _timing = timing;
            _element = element;

            var path = Path.Combine(Program.Fi.Directory.FullName, element.ImagePath);
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
            if (_element.FadeList.Count > 0)
            {
                Ranges.FadeTime = new TimeRange
                {
                    Min = (int)_element.FadeList.Min(k => k.StartTime),
                    Max = (int)_element.FadeList.Max(k => k.EndTime),
                };
                Statics.F.Source = _element.FadeList.First().F1;
                Statics.F.Target = _element.FadeList.Last().F2;
            }
            if (_element.RotateList.Count > 0)
            {
                Ranges.RotateTime = new TimeRange
                {
                    Min = (int)_element.RotateList.Min(k => k.StartTime),
                    Max = (int)_element.RotateList.Max(k => k.EndTime),
                };
                Statics.Rad.Source = _element.RotateList.First().R1;
                Statics.Rad.Target = _element.RotateList.Last().R2;
            }

            if (_element.ScaleList.Count > 0)
            {
                Ranges.VTime = new TimeRange
                {
                    Min = (int)_element.ScaleList.Min(k => k.StartTime),
                    Max = (int)_element.ScaleList.Max(k => k.EndTime)
                };
                Statics.Vx.Source = _element.ScaleList.First().S1;
                Statics.Vy.Source = _element.ScaleList.First().S1;
                Statics.Vx.Target = _element.ScaleList.Last().S2;
                Statics.Vy.Target = _element.ScaleList.Last().S2;
            }
            if (_element.VectorList.Count > 0)
            {
                if (!Ranges.VTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.VectorList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.VectorList.Max(k => k.EndTime);

                    if (tmpMin < Ranges.VTime.Min)
                    {
                        Ranges.VTime.Min = tmpMin;
                        Statics.Vx.Source = _element.VectorList.First().Vx1;
                        Statics.Vy.Source = _element.VectorList.First().Vy1;
                    }

                    if (tmpMax > Ranges.VTime.Max)
                    {
                        Ranges.VTime.Max = tmpMax;
                        Statics.Vx.Target = _element.VectorList.Last().Vx2;
                        Statics.Vy.Target = _element.VectorList.Last().Vy2;
                    }
                }
                else
                {
                    Ranges.VTime = new TimeRange
                    {
                        Min = (int)_element.VectorList.Min(k => k.StartTime),
                        Max = (int)_element.VectorList.Max(k => k.EndTime)
                    };
                    Statics.Vx.Source = _element.VectorList.First().Vx1;
                    Statics.Vy.Source = _element.VectorList.First().Vy1;
                    Statics.Vx.Target = _element.VectorList.Last().Vx2;
                    Statics.Vy.Target = _element.VectorList.Last().Vy2;
                }
            }

            if (_element.MoveList.Count > 0)
            {
                Ranges.MovTime = new TimeRange
                {
                    Min = (int)_element.MoveList.Min(k => k.StartTime),
                    Max = (int)_element.MoveList.Max(k => k.EndTime)
                };
                Statics.X.Source = _element.MoveList.First().X1 + 107;
                Statics.Y.Source = _element.MoveList.First().Y1;
                Statics.X.Target = _element.MoveList.Last().X2 + 107;
                Statics.Y.Target = _element.MoveList.Last().Y2;
            }
            if (_element.MoveXList.Count > 0)
            {
                if (!Ranges.MovTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveXList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveXList.Max(k => k.EndTime);

                    if (tmpMin < Ranges.MovTime.Min)
                    {
                        Ranges.MovTime.Min = tmpMin;
                        Statics.X.Source = _element.MoveXList.First().X1 + 107;
                    }

                    if (tmpMax > Ranges.MovTime.Max)
                    {
                        Ranges.MovTime.Max = tmpMax;
                        Statics.X.Target = _element.MoveXList.Last().X2 + 107;
                    }
                }
                else
                {
                    Ranges.MovTime = new TimeRange
                    {
                        Min = (int)_element.MoveXList.Min(k => k.StartTime),
                        Max = (int)_element.MoveXList.Max(k => k.EndTime)
                    };
                    Statics.X.Source = _element.MoveXList.First().X1 + 107;
                    Statics.X.Target = _element.MoveXList.Last().X2 + 107;
                }
            }
            if (_element.MoveYList.Count > 0)
            {
                if (!Ranges.MovTime.Equals(TimeRange.Default))
                {
                    var tmpMin = (int)_element.MoveYList.Min(k => k.StartTime);
                    var tmpMax = (int)_element.MoveYList.Max(k => k.EndTime);

                    if (tmpMin <= Ranges.MovTime.Min)
                    {
                        Ranges.MovTime.Min = tmpMin;
                        Statics.Y.Source = _element.MoveYList.First().Y1;
                    }

                    if (tmpMax >= Ranges.MovTime.Max)
                    {
                        Ranges.MovTime.Max = tmpMax;
                        Statics.Y.Target = _element.MoveYList.Last().Y2;
                    }
                }
                else
                {
                    Ranges.MovTime = new TimeRange
                    {
                        Min = (int)_element.MoveYList.Min(k => k.StartTime),
                        Max = (int)_element.MoveYList.Max(k => k.EndTime)
                    };
                    Statics.Y.Source = _element.MoveYList.First().Y1;
                    Statics.Y.Target = _element.MoveYList.Last().Y2;
                }
            }

            if (_element.ColorList.Count > 0)
            {
                Ranges.CTime = new TimeRange
                {
                    Min = (int)_element.ColorList.Min(k => k.StartTime),
                    Max = (int)_element.ColorList.Max(k => k.EndTime)
                };
                Statics.R.Source = _element.ColorList.First().R1;
                Statics.G.Source = _element.ColorList.First().G1;
                Statics.B.Source = _element.ColorList.First().B1;
                Statics.R.Target = _element.ColorList.Last().R2;
                Statics.G.Target = _element.ColorList.Last().G2;
                Statics.B.Target = _element.ColorList.Last().B2;
            }

            if (_element.ParameterList.Count > 0)
            {
                Ranges.PTime = new TimeRange
                {
                    Min = (int)_element.ParameterList.Min(k => k.StartTime),
                    Max = (int)_element.ParameterList.Max(k => k.EndTime)
                };
            }

            Statics.X.RealTimeToSource();
            Statics.Y.RealTimeToSource();
            Statics.Rad.RealTimeToSource();
            Statics.Vx.RealTimeToSource();
            Statics.Vy.RealTimeToSource();
            Statics.F.RealTimeToSource();
        }

        protected void SetDefaultValue()
        {
            Origin = _element.Origin;

            Statics.X = (Static<float>)(_element.DefaultX + 107);
            Statics.Y = (Static<float>)_element.DefaultY;
            Statics.F = (Static<float>)1;
            Statics.Rad = (Static<float>)0;
            Statics.Vx = (Static<float>)1;
            Statics.Vy = (Static<float>)1;
            Statics.R = (Static<float>)255;
            Statics.G = (Static<float>)255;
            Statics.B = (Static<float>)255;

            UseH = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Horizontal);
            UseV = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Vertical);
            UseA = _element.ParameterList.Any(e => e.Type == Milkitic.OsbLib.Models.ParameterEnum.Additive);

            // rects
            Statics.W = (Static<float>)Texture.Size.Width;
            Statics.H = (Static<float>)Texture.Size.Height;

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

        public void Move(EasingType easingEnum, int startTime, int endTime, Gdip.PointF startPoint, Gdip.PointF endPoint)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.X.RealTime = startPoint.X + (float)easingEnum.Ease(t) * (endPoint.X - startPoint.X);
                Statics.Y.RealTime = startPoint.Y + (float)easingEnum.Ease(t) * (endPoint.Y - startPoint.Y);
            }

            if (ms >= Ranges.MovTime.Max)
            {
                Statics.X.RealTimeToTarget();
                Statics.Y.RealTimeToTarget();
            }
        }

        public void MoveX(EasingType easingEnum, int startTime, int endTime, float startX, float endX)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.X.RealTime = startX + (float)easingEnum.Ease(t) * (endX - startX);
            }

            if (ms >= Ranges.MovTime.Max)
                Statics.X.RealTimeToTarget();
        }

        public void MoveY(EasingType easingEnum, int startTime, int endTime, float startY, float endY)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.Y.RealTime = startY + (float)easingEnum.Ease(t) * (endY - startY);
            }

            if (ms >= Ranges.MovTime.Max)
                Statics.Y.RealTimeToTarget();
        }

        public void Rotate(EasingType easingEnum, int startTime, int endTime, float startRad, float endRad)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.Rad.RealTime = startRad + (float)easingEnum.Ease(t) * (endRad - startRad);
            }

            if (ms >= Ranges.RotateTime.Max)
            {
                Statics.Rad.RealTimeToTarget();
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
                Statics.Vx.RealTime = startVx + (float)easingEnum.Ease(t) * (endVx - startVx);
                Statics.Vy.RealTime = startVy + (float)easingEnum.Ease(t) * (endVy - startVy);
            }

            if (ms >= Ranges.VTime.Max)
            {
                Statics.Vx.RealTimeToTarget();
                Statics.Vy.RealTimeToTarget();
            }
        }

        public void Fade(EasingType easingEnum, int startTime, int endTime, float startOpacity, float endOpacity)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.F.RealTime = startOpacity + (float)easingEnum.Ease(t) * (endOpacity - startOpacity);
            }

            if (ms >= Ranges.FadeTime.Max)
            {
                Statics.F.RealTimeToTarget();
            }
        }

        public void Color(EasingType easingEnum, int startTime, int endTime, float r1, float g1, float b1, float r2, float g2, float b2)
        {
            float ms = _timing.Offset;
            if (!IsFinished && ms >= startTime && ms <= endTime)
            {
                var t = (ms - startTime) / (endTime - startTime);
                Statics.R.RealTime = r1 + (float)easingEnum.Ease(t) * (r2 - r1);
                Statics.G.RealTime = g1 + (float)easingEnum.Ease(t) * (g2 - g1);
                Statics.B.RealTime = b1 + (float)easingEnum.Ease(t) * (b2 - b1);
            }

            if (ms >= Ranges.CTime.Max)
            {
                Statics.R.RealTimeToTarget();
                Statics.G.RealTimeToTarget();
                Statics.B.RealTimeToTarget();
            }
        }

        public void Additive(int startTime, int endTime)
        {
            // Todo: Not implemented
        }

        public void Dispose()
        {
            if (TextureList == null) return;
            foreach (var b in TextureList)
                b?.Dispose();
            TextureList = null;
            Texture = null;
            Ranges = null;
            Statics = null;
        }
    }
}
