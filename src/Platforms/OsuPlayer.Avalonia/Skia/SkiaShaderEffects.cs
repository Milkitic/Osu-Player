using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace OsuPlayer.Skia;

/// <summary>
/// Avalonia 12 自定义渲染操作 - 应用 Skia 灰度滤镜(替代 WPF GrayscaleEffect 像素着色器)。
/// 参考 KeyASIO 项目 SkiaColorMatrixUtils 思路。
/// </summary>
public sealed class GrayscaleEffect : ICustomDrawOperation
{
    public Rect Bounds { get; set; }

    public void Dispose() { }

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other) => other is GrayscaleEffect;

    public void Render(ImmediateDrawingContext context)
    {
        var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (lease is null) return;
        using var skLease = lease.Lease();
        var canvas = skLease.SkCanvas;
        canvas.Save();
        // 0.21, 0.72, 0.07 权重(W3C 标准的 ITU-R 601-2 luma transform)
        var grayMatrix = new[]
        {
            0.21f, 0.72f, 0.07f, 0, 0,
            0.21f, 0.72f, 0.07f, 0, 0,
            0.21f, 0.72f, 0.07f, 0, 0,
            0,     0,     0,     1, 0
        };
        var filter = SKColorFilter.CreateColorMatrix(grayMatrix);
        using var paint = new SKPaint
        {
            ColorFilter = filter,
            IsAntialias = true
        };
        // 以白色填充 + filter 即可对整个目标产生灰度效果
        canvas.DrawRect(SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height), paint);
        canvas.Restore();
    }
}

/// <summary>
/// Avalonia 12 自定义渲染操作 - 应用 Skia 色相/饱和度/亮度调整(替代 WPF HueRotationEffect)。
/// </summary>
public sealed class HueSaturationEffect : ICustomDrawOperation
{
    public Rect Bounds { get; set; }
    public float HueDegrees { get; set; } = 0f;
    public float Saturation { get; set; } = 1f;
    public float Lightness { get; set; } = 0f;

    public void Dispose() { }

    public bool HitTest(Point p) => false;

    public bool Equals(ICustomDrawOperation? other)
        => other is HueSaturationEffect h
           && h.HueDegrees == HueDegrees
           && h.Saturation == Saturation
           && h.Lightness == Lightness;

    public void Render(ImmediateDrawingContext context)
    {
        var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (lease is null) return;
        using var skLease = lease.Lease();
        var canvas = skLease.SkCanvas;

        // 计算色相旋转矩阵
        var cosA = (float)Math.Cos(HueDegrees * Math.PI / 180.0);
        var sinA = (float)Math.Sin(HueDegrees * Math.PI / 180.0);
        const float LUM_R = 0.213f, LUM_G = 0.715f, LUM_B = 0.072f;

        // 构建 4x5 色彩旋转矩阵(ColorMatrix)
        var m = new float[20];
        // Row 0
        m[0] = LUM_R + (cosA + (1 - cosA) * LUM_R) * Saturation;
        m[1] = LUM_G + (cosA - cosA * LUM_G) * Saturation;
        m[2] = LUM_B + (cosA - cosA * LUM_B) * Saturation;
        m[3] = 0; m[4] = Lightness * 255;
        // Row 1
        m[5] = LUM_R + (cosA * 0 + -sinA * LUM_R) * Saturation;
        m[6] = LUM_G + (cosA + sinA * LUM_G) * Saturation;
        m[7] = LUM_B + (cosA - sinA * LUM_B) * Saturation;
        m[8] = 0; m[9] = Lightness * 255;
        // Row 2
        m[10] = LUM_R + (cosA * 0 + sinA * LUM_R) * Saturation;
        m[11] = LUM_G + (cosA - sinA * LUM_G) * Saturation;
        m[12] = LUM_B + (cosA + (1 - cosA) * LUM_B) * Saturation;
        m[13] = 0; m[14] = Lightness * 255;
        // Row 3
        m[15] = 0; m[16] = 0; m[17] = 0; m[18] = 1; m[19] = 0;

        var filter = SKColorFilter.CreateColorMatrix(m);
        using var paint = new SKPaint
        {
            ColorFilter = filter,
            IsAntialias = true
        };
        canvas.Save();
        canvas.DrawRect(SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height), paint);
        canvas.Restore();
    }
}
