using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OsuPlayer.Controls.MessageDialogs;

namespace OsuPlayer.Controls;

/// <summary>
///     提供可自定义的内容对话框控件，支持头部、内容和底部区域的自定义
/// </summary>
public class ContentDialog : ContentControl
{
    #region 构造函数

    /// <summary>
    ///     初始化 <see cref="ContentDialog" /> 类的新实例
    /// </summary>
    public ContentDialog()
    {
        _validateAndCloseDialogCommand = new AsyncRelayCommand<object?>(InternalValidateAndClose);
        _closeDialogCommand = new AsyncRelayCommand<object?>(InternalClose);
        Loaded += ContentDialog_Loaded;
    }

    #endregion

    #region 静态字段和属性定义

    /// <summary>
    ///     定义是否显示对话框头部的属性
    /// </summary>
    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(ShowHeader));

    /// <summary>
    ///     定义对话框头部内容的属性
    /// </summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<ContentDialog, object?>(nameof(Header));

    /// <summary>
    ///     定义头部字体大小的属性
    /// </summary>
    public static readonly StyledProperty<int> HeaderFontSizeProperty =
        AvaloniaProperty.Register<ContentDialog, int>(nameof(HeaderFontSize));

    /// <summary>
    ///     定义头部背景色的属性
    /// </summary>
    public static readonly StyledProperty<IBrush> HeaderBackgroundProperty =
        AvaloniaProperty.Register<ContentDialog, IBrush>(nameof(HeaderBackground));

    /// <summary>
    ///     定义头部前景色的属性
    /// </summary>
    public static readonly StyledProperty<IBrush> HeaderForegroundProperty =
        AvaloniaProperty.Register<ContentDialog, IBrush>(nameof(HeaderForeground));

    /// <summary>
    ///     定义是否在头部显示关闭按钮的属性
    /// </summary>
    public static readonly StyledProperty<bool> HeaderShowCloseProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(HeaderShowClose));

    /// <summary>
    ///     定义头部按钮主题的属性
    /// </summary>
    public static readonly StyledProperty<ControlTheme> HeaderButtonThemeProperty =
        AvaloniaProperty.Register<ContentDialog, ControlTheme>(nameof(HeaderButtonTheme));

    /// <summary>
    ///     定义是否显示对话框底部的属性
    /// </summary>
    public static readonly StyledProperty<bool> ShowFooterProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(ShowFooter));

    /// <summary>
    ///     定义对话框底部内容的属性
    /// </summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<ContentDialog, object?>(nameof(Footer));

    /// <summary>
    ///     定义底部字体大小的属性
    /// </summary>
    public static readonly StyledProperty<int> FooterFontSizeProperty =
        AvaloniaProperty.Register<ContentDialog, int>(nameof(FooterFontSize));

    /// <summary>
    ///     定义底部背景色的属性
    /// </summary>
    public static readonly StyledProperty<IBrush> FooterBackgroundProperty =
        AvaloniaProperty.Register<ContentDialog, IBrush>(nameof(FooterBackground));

    /// <summary>
    ///     定义底部前景色的属性
    /// </summary>
    public static readonly StyledProperty<IBrush> FooterForegroundProperty =
        AvaloniaProperty.Register<ContentDialog, IBrush>(nameof(FooterForeground));

    /// <summary>
    ///     定义底部按钮样式的属性
    /// </summary>
    public static readonly StyledProperty<FooterButtonStyle> FooterButtonStyleProperty =
        AvaloniaProperty.Register<ContentDialog, FooterButtonStyle>(nameof(FooterButtonStyle));

    /// <summary>
    ///     定义底部"是"按钮文本的属性
    /// </summary>
    public static readonly StyledProperty<string> FooterYesButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string>(nameof(FooterYesButtonText));

    /// <summary>
    ///     定义底部"否"按钮文本的属性
    /// </summary>
    public static readonly StyledProperty<string> FooterNoButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string>(nameof(FooterNoButtonText));

    /// <summary>
    ///     定义底部按钮主题的属性
    /// </summary>
    public static readonly StyledProperty<ControlTheme> FooterButtonThemeProperty =
        AvaloniaProperty.Register<ContentDialog, ControlTheme>(nameof(FooterButtonTheme));

    /// <summary>
    ///     定义底部按钮方向的属性
    /// </summary>
    public static readonly StyledProperty<FlowDirection> FooterButtonDirectionProperty =
        AvaloniaProperty.Register<ContentDialog, FlowDirection>(nameof(FooterButtonDirection));

    /// <summary>
    ///     定义对话框是否可确认的属性
    /// </summary>
    public static readonly StyledProperty<bool> ConfirmableProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(Confirmable), true);

    /// <summary>
    ///     定义验证并关闭对话框命令的属性
    /// </summary>
    public static readonly DirectProperty<ContentDialog, ICommand> ValidateAndCloseDialogCommandProperty =
        AvaloniaProperty.RegisterDirect<ContentDialog, ICommand>(nameof(ContentDialog),
            o => o.ValidateAndCloseDialogCommand,
            null, null!, BindingMode.TwoWay);

    /// <summary>
    ///     定义关闭对话框命令的属性
    /// </summary>
    public static readonly DirectProperty<ContentDialog, ICommand> CloseDialogCommandProperty =
        AvaloniaProperty.RegisterDirect<ContentDialog, ICommand>(nameof(ContentDialog),
            o => o.CloseDialogCommand,
            null, null!, BindingMode.TwoWay);

    #endregion

    #region 实例字段

    private ICommand _closeDialogCommand;
    private ICommand _validateAndCloseDialogCommand;
    private StackPanel? _stackPanel;

    #endregion

    #region 公共属性

    /// <summary>
    ///     获取或设置是否显示对话框的头部
    /// </summary>
    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>
    ///     获取或设置对话框的头部内容
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    ///     获取或设置头部文本的字体大小
    /// </summary>
    public int HeaderFontSize
    {
        get => GetValue(HeaderFontSizeProperty);
        set => SetValue(HeaderFontSizeProperty, value);
    }

    /// <summary>
    ///     获取或设置头部区域的背景色
    /// </summary>
    public IBrush HeaderBackground
    {
        get => GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    /// <summary>
    ///     获取或设置头部区域的前景色
    /// </summary>
    public IBrush HeaderForeground
    {
        get => GetValue(HeaderForegroundProperty);
        set => SetValue(HeaderForegroundProperty, value);
    }

    /// <summary>
    ///     获取或设置是否在头部区域显示关闭按钮
    /// </summary>
    public bool HeaderShowClose
    {
        get => GetValue(HeaderShowCloseProperty);
        set => SetValue(HeaderShowCloseProperty, value);
    }

    /// <summary>
    ///     获取或设置头部区域按钮的主题
    /// </summary>
    public ControlTheme HeaderButtonTheme
    {
        get => GetValue(HeaderButtonThemeProperty);
        set => SetValue(HeaderButtonThemeProperty, value);
    }

    /// <summary>
    ///     获取或设置是否显示对话框的底部
    /// </summary>
    public bool ShowFooter
    {
        get => GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    /// <summary>
    ///     获取或设置对话框的底部内容
    /// </summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>
    ///     获取或设置底部文本的字体大小
    /// </summary>
    public int FooterFontSize
    {
        get => GetValue(FooterFontSizeProperty);
        set => SetValue(FooterFontSizeProperty, value);
    }

    /// <summary>
    ///     获取或设置底部区域的背景色
    /// </summary>
    public IBrush FooterBackground
    {
        get => GetValue(FooterBackgroundProperty);
        set => SetValue(FooterBackgroundProperty, value);
    }

    /// <summary>
    ///     获取或设置底部区域的前景色
    /// </summary>
    public IBrush FooterForeground
    {
        get => GetValue(FooterForegroundProperty);
        set => SetValue(FooterForegroundProperty, value);
    }

    /// <summary>
    ///     获取或设置底部按钮的样式
    /// </summary>
    public FooterButtonStyle FooterButtonStyle
    {
        get => GetValue(FooterButtonStyleProperty);
        set => SetValue(FooterButtonStyleProperty, value);
    }

    /// <summary>
    ///     获取或设置底部"是"按钮的文本
    /// </summary>
    public string FooterYesButtonText
    {
        get => GetValue(FooterYesButtonTextProperty);
        set => SetValue(FooterYesButtonTextProperty, value);
    }

    /// <summary>
    ///     获取或设置底部"否"按钮的文本
    /// </summary>
    public string FooterNoButtonText
    {
        get => GetValue(FooterNoButtonTextProperty);
        set => SetValue(FooterNoButtonTextProperty, value);
    }

    /// <summary>
    ///     获取或设置底部按钮的主题
    /// </summary>
    public ControlTheme FooterButtonTheme
    {
        get => GetValue(FooterButtonThemeProperty);
        set => SetValue(FooterButtonThemeProperty, value);
    }

    /// <summary>
    ///     获取或设置底部按钮的排列方向
    /// </summary>
    public FlowDirection FooterButtonDirection
    {
        get => GetValue(FooterButtonDirectionProperty);
        set => SetValue(FooterButtonDirectionProperty, value);
    }

    /// <summary>
    ///     获取或设置对话框是否可以通过确认按钮关闭
    /// </summary>
    public bool Confirmable
    {
        get => GetValue(ConfirmableProperty);
        set => SetValue(ConfirmableProperty, value);
    }

    /// <summary>
    ///     获取任务对话框常量实例
    /// </summary>
    public TaskDialogConstants TaskDialogConstants { get; } = TaskDialogConstants.Instance;

    /// <summary>
    ///     获取验证并关闭对话框的命令
    /// </summary>
    public ICommand ValidateAndCloseDialogCommand
    {
        get => _validateAndCloseDialogCommand;
        private set => SetAndRaise(ValidateAndCloseDialogCommandProperty, ref _validateAndCloseDialogCommand, value);
    }

    /// <summary>
    ///     获取关闭对话框的命令
    /// </summary>
    public ICommand CloseDialogCommand
    {
        get => _closeDialogCommand;
        private set => SetAndRaise(CloseDialogCommandProperty, ref _closeDialogCommand, value);
    }

    #endregion

    #region 受保护的属性和方法

    /// <inheritdoc />
    protected override Type StyleKeyOverride { get; } = typeof(ContentDialog);

    /// <summary>
    ///     当模板应用时调用
    /// </summary>
    /// <param name="e">模板应用事件参数</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _stackPanel = e.NameScope.Find<StackPanel>("PART_FooterButtonsHost");
        ApplyFooterButtonsDirection();
    }

    /// <summary>
    ///     当属性值变化时调用
    /// </summary>
    /// <param name="e">属性变化事件参数</param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == FooterButtonDirectionProperty)
        {
            ApplyFooterButtonsDirection();
        }
    }

    protected virtual Task<bool> OnAsyncValidating()
    {
        return Task.FromResult(true);
    }

    protected virtual Task<bool> OnAsyncClosing(bool? value)
    {
        return Task.FromResult(true);
    }

    protected virtual Task OnAsyncClosed(bool? value)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 私有方法

    private void ApplyFooterButtonsDirection()
    {
        if (_stackPanel == null) return;
        var firstButton = (Button)_stackPanel.Children[0];
        if (FooterButtonDirection == FlowDirection.RightToLeft && firstButton.CommandParameter is true ||
            FooterButtonDirection == FlowDirection.LeftToRight && firstButton.CommandParameter is false)
        {
            _stackPanel.Children.RemoveAt(0);
            _stackPanel.Children.Insert(1, firstButton);
        }
    }

    private void ContentDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_stackPanel == null) return;
        if (FooterButtonDirection == FlowDirection.RightToLeft)
        {
            if (_stackPanel.Children.Count >= 1)
                _stackPanel.Children[1].Focus();
        }
        else if (FooterButtonDirection == FlowDirection.LeftToRight)
        {
            if (_stackPanel.Children.Count >= 0)
                _stackPanel.Children[0].Focus();
        }
    }

    private async Task InternalValidateAndClose(object? obj)
    {
        var result = await OnAsyncValidating();
        if (result)
        {
            await InternalClose(true);
        }
    }

    private async Task InternalClose(object? obj)
    {
        var result = await OnAsyncClosing(obj as bool?);
        if (!result) return;

        var dialogHost = FindParentElement<DialogHost>(this);
        if (dialogHost == null) return;

        var canClose = dialogHost.CloseDialogCommand.CanExecute(obj);
        if (!canClose) return;

        dialogHost.CloseDialogCommand?.Execute(obj);
        await OnAsyncClosed(obj as bool?);
    }

    #endregion

    /// <summary>
    /// 查找指定类型的父元素。
    /// </summary>
    /// <typeparam name="T">父元素类型</typeparam>
    /// <param name="obj">当前对象</param>
    /// <returns>找到的父元素，如果未找到则返回null</returns>
    public static T? FindParentElement<T>(StyledElement obj) where T : StyledElement
    {
        return FindParentElement(obj, typeof(T)) as T;
    }

    /// <summary>
    /// 查找指定类型的父元素。
    /// </summary>
    /// <param name="obj">当前对象</param>
    /// <param name="types">父元素类型数组</param>
    /// <returns>找到的父元素，如果未找到则返回null</returns>
    public static StyledElement? FindParentElement(StyledElement obj, params Type[] types)
    {
        var parent = obj.Parent;
        while (parent != null)
        {
            if (types.Length == 0)
            {
                return parent;
            }

            var type = parent.GetType();
            if (types.Any(k => type.IsSubclassOf(k) || k == type))
            {
                return parent;
            }

            parent = parent.Parent;
        }

        return null;
    }
}
