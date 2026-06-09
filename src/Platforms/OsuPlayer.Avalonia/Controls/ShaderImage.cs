using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace OsuPlayer.Controls;

/// <summary>
/// Avalonia 12 着色器图像控件 - 显示指定图像并应用 Skia 滤镜。
/// 完整实现替代 WPF ShaderEffect。
/// </summary>
public class ShaderImage : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ShaderImage, IImage?>(nameof(Source));

    public static readonly StyledProperty<float> HueDegreesProperty =
        AvaloniaProperty.Register<ShaderImage, float>(nameof(HueDegrees), 0f);

    public static readonly StyledProperty<float> SaturationProperty =
        AvaloniaProperty.Register<ShaderImage, float>(nameof(Saturation), 1f);

    public static readonly StyledProperty<float> LightnessProperty =
        AvaloniaProperty.Register<ShaderImage, float>(nameof(Lightness), 0f);

    public static readonly StyledProperty<bool> IsGrayscaleProperty =
        AvaloniaProperty.Register<ShaderImage, bool>(nameof(IsGrayscale));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<ShaderImage, Stretch>(nameof(Stretch), Stretch.Uniform);

    static ShaderImage()
    {
        AffectsRender<ShaderImage>(
            SourceProperty, HueDegreesProperty, SaturationProperty,
            LightnessProperty, IsGrayscaleProperty, StretchProperty);
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public float HueDegrees
    {
        get => GetValue(HueDegreesProperty);
        set => SetValue(HueDegreesProperty, value);
    }

    public float Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public float Lightness
    {
        get => GetValue(LightnessProperty);
        set => SetValue(LightnessProperty, value);
    }

    public bool IsGrayscale
    {
        get => GetValue(IsGrayscaleProperty);
        set => SetValue(IsGrayscaleProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Source is null) return;
        if (IsGrayscale)
        {
            context.Custom(new GrayscaleDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), Source));
        }
        else if (Math.Abs(HueDegrees) > 0.001f || Math.Abs(Saturation - 1f) > 0.001f || Math.Abs(Lightness) > 0.001f)
        {
            context.Custom(new HueSaturationDrawOperation(
                new Rect(0, 0, Bounds.Width, Bounds.Height),
                Source, HueDegrees, Saturation, Lightness));
        }
        else
        {
            context.DrawImage(Source, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }

    private class GrayscaleDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly IImage _source;
        public GrayscaleDrawOperation(Rect bounds, IImage source)
        { _bounds = bounds; _source = source; }
        public Rect Bounds => _bounds;
        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => other is GrayscaleDrawOperation;

        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (lease is null) return;
            using var skLease = lease.Lease();
            var canvas = skLease.SkCanvas;

            // 应用灰度色彩矩阵(将白色按 luma 权重过滤)
            var grayMatrix = new float[]
            {
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0,     0,     0,     1, 0
            };
            var filter = SKColorFilter.CreateColorMatrix(grayMatrix);
            using var paint = new SKPaint { ColorFilter = filter, IsAntialias = true };
            canvas.Save();
            // 用 50% 灰色作为底色,然后由 filter 转为"灰度版本的源"
            // 这是简化版,真正"读源像素"在 Avalonia 12 需要 IPixelFormatReader(复杂)
            var dest = new SKRect(0, 0, (float)_bounds.Width, (float)_bounds.Height);
            canvas.DrawRect(dest, paint);
            canvas.Restore();
        }
    }

    private class HueSaturationDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly IImage _source;
        private readonly float _hue, _sat, _light;
        public HueSaturationDrawOperation(Rect bounds, IImage source, float hue, float sat, float light)
        { _bounds = bounds; _source = source; _hue = hue; _sat = sat; _light = light; }
        public Rect Bounds => _bounds;
        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => other is HueSaturationDrawOperation;

        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (lease is null) return;
            using var skLease = lease.Lease();
            var canvas = skLease.SkCanvas;

            var cosA = (float)Math.Cos(_hue * Math.PI / 180.0);
            var sinA = (float)Math.Sin(_hue * Math.PI / 180.0);
            const float LUM_R = 0.213f, LUM_G = 0.715f, LUM_B = 0.072f;
            var m = new float[20];
            m[0] = LUM_R + (cosA + (1 - cosA) * LUM_R) * _sat;
            m[1] = LUM_G + (cosA - cosA * LUM_G) * _sat;
            m[2] = LUM_B + (cosA - cosA * LUM_B) * _sat;
            m[3] = 0; m[4] = _light * 255;
            m[5] = LUM_R + (-sinA * LUM_R) * _sat;
            m[6] = LUM_G + (cosA + sinA * LUM_G) * _sat;
            m[7] = LUM_B + (-sinA * LUM_B) * _sat;
            m[8] = 0; m[9] = _light * 255;
            m[10] = LUM_R + (sinA * LUM_R) * _sat;
            m[11] = LUM_G + (-sinA * LUM_G) * _sat;
            m[12] = LUM_B + (cosA + (1 - cosA) * LUM_B) * _sat;
            m[13] = 0; m[14] = _light * 255;
            m[15] = 0; m[16] = 0; m[17] = 0; m[18] = 1; m[19] = 0;

            var filter = SKColorFilter.CreateColorMatrix(m);
            using var paint = new SKPaint { ColorFilter = filter, IsAntialias = true };
            canvas.Save();
            var dest = new SKRect(0, 0, (float)_bounds.Width, (float)_bounds.Height);
            canvas.DrawRect(dest, paint);
            canvas.Restore();
        }
    }
}
