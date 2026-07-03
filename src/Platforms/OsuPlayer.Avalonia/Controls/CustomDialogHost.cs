using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DialogHostAvalonia;

namespace OsuPlayer.Controls;

/// <summary>
/// 封装DialogHost，提供增强的对话框宿主功能
/// </summary>
public sealed class CustomDialogHost : DialogHost
{
    /// <summary>
    /// 定义自动标识符的属性
    /// </summary>
    public static readonly DirectProperty<CustomDialogHost, string?> AutoIdentifierProperty =
        AvaloniaProperty.RegisterDirect<CustomDialogHost, string?>(nameof(AutoIdentifier),
            o => o.AutoIdentifier, (o, v) => o.AutoIdentifier = v);

    private string? _autoIdentifier;

    /// <summary>
    /// 获取或设置自动生成的对话框标识符前缀
    /// </summary>
    public string? AutoIdentifier
    {
        get => _autoIdentifier;
        set
        {
            if (!SetAndRaise(AutoIdentifierProperty, ref _autoIdentifier, value)) return;
            UpdateIdentifier();
        }
    }

    /// <summary>
    /// 当控件附加到可视树时调用
    /// </summary>
    /// <param name="e">附加到可视树的事件参数</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateIdentifier();
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride { get; } = typeof(DialogHost);

    /// <summary>
    /// 显示对话框内容
    /// </summary>
    /// <param name="content">要显示的对话框内容</param>
    /// <param name="dialogIdentifier">对话框标识符</param>
    /// <param name="visual">关联的视觉元素</param>
    /// <returns>对话框的结果</returns>
    public static async Task<object?> Show(object content, string dialogIdentifier, Visual? visual = null)
    {
        var postfix = GetPostFix(visual);
        var identifier = dialogIdentifier + postfix;
        try
        {
            var isOpen = IsDialogOpen(identifier);
            try
            {
                return await DialogHost.Show(content, identifier);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("is already open", StringComparison.InvariantCulture))
                {
                    return false;
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("No loaded", StringComparison.InvariantCulture))
            {
                return await DialogHost.Show(content, dialogIdentifier);
            }

            throw;
        }
    }

    private void UpdateIdentifier()
    {
        var postFix = GetPostFix(this);
        Identifier = AutoIdentifier + postFix;
    }

    private static string? GetPostFix(Visual? visual)
    {
        if (TopLevel.GetTopLevel(visual) is not { } topLevel) return null;
        if (topLevel is not Window window) return null;
        if (Application.Current is { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime desktop } && desktop.MainWindow == window) return null;
        return topLevel.GetHashCode().ToString();
    }
}

/// <summary>
/// 为Visual提供扩展方法，简化对话框操作
/// </summary>
public static class CustomDialogHostExtensions
{
    /// <summary>
    /// 在指定的视觉元素上显示内容对话框
    /// </summary>
    /// <param name="visual">要显示对话框的视觉元素</param>
    /// <param name="content">对话框的内容</param>
    /// <param name="dialogIdentifier">对话框的标识符</param>
    /// <returns>对话框的结果</returns>
    public static Task<object?> ShowContentDialog(this Visual? visual, object content, string dialogIdentifier)
    {
        return CustomDialogHost.Show(content, dialogIdentifier, visual);
    }
}
