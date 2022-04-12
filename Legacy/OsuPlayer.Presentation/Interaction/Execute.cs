﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Milky.OsuPlayer.Presentation.Interaction
{
    public static class Execute
    {
        private static SynchronizationContext _uiContext;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void SetMainThreadContext()
        {
            if (_uiContext != null) Logger.Warn("Current SynchronizationContext may be replaced.");

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                var fileName = Path.GetFileName(assembly.Location);
                if (fileName == "System.Windows.Forms.dll")
                {
                    var type = assembly.DefinedTypes.First(k => k.Name.StartsWith("WindowsFormsSynchronizationContext"));
                    _uiContext = (SynchronizationContext)Activator.CreateInstance(type);
                    break;
                }
                else if (fileName == "WindowsBase.dll")
                {
                    var type = assembly.DefinedTypes.First(k => k.Name.StartsWith("DispatcherSynchronizationContext"));
                    _uiContext = (SynchronizationContext)Activator.CreateInstance(type);
                    break;
                }
            }

            if (_uiContext == null) _uiContext = SynchronizationContext.Current;
        }

        public static void OnUiThread(this Action action)
        {
            if (_uiContext == null)
            {
                if (Application.Current?.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            action?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "UiContext execute error.");
                        }
                    });
                else
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "UiContext execute error.");
                    }
                }
            }
            else
            {
                _uiContext.Send(obj => { action?.Invoke(); }, null);
            }
        }

        public static void ToUiThread(this Action action)
        {
            if (_uiContext == null)
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "UiContext execute error.");
                    }
                }), DispatcherPriority.Normal);
            }
            else
            {
                _uiContext.Post(obj => { action?.Invoke(); }, null);
            }
        }

        public static bool CheckDispatcherAccess() => Thread.CurrentThread.ManagedThreadId == 1;
    }
}
