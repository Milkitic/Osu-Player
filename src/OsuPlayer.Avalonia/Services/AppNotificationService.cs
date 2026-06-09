using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using OsuPlayer.Avalonia.Interaction;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia.Services;

/// <summary>
/// Avalonia 版本的 AppNotificationService - 维护通知队列,等待 NotifyOverlay 在 UI 中显示
/// </summary>
public sealed class AppNotificationService : IAppNotificationService
{
    private static AppNotificationService? s_instance;
    public static AppNotificationService Instance => s_instance ??= new AppNotificationService();

    private readonly Queue<NotificationItem> _queue = new();

    /// <summary>
    /// 通知 UI 组件订阅此事件
    /// </summary>
    public event Action<string, string?>? OnPush;

    public void Push(string message) => Push(message, null);

    public void Push(string message, string title)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            OnPush?.Invoke(message, title);
        }
        else
        {
            Dispatcher.UIThread.Post(() => OnPush?.Invoke(message, title));
        }
    }
}

internal record NotificationItem(string Message, string? Title);
