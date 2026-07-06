using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace OsuPlayer.Presentation.Dependency;

public static class ObjectExtension
{
    public static T? GetParentObjectByName<T>(this Control obj, string name) where T : Control
    {
        var parent = obj.GetVisualParent();
        while (parent != null)
        {
            if (parent is T control && (control.Name == name || string.IsNullOrEmpty(name)))
            {
                return control;
            }
            parent = parent.GetVisualParent();
        }
        return null;
    }

    public static T? GetParentObject<T>(this Control obj) where T : Control
    {
        return FindParentObjects(obj) as T;
    }

    public static Control? FindParentObjects(this Control obj, params Type[] types)
    {
        var parent = obj.GetVisualParent();
        while (parent != null)
        {
            if (parent is Control control)
            {
                if (types.Length == 0)
                    return control;

                var type = control.GetType();
                if (types.Any(k => type.IsSubclassOf(k)))
                {
                    return control;
                }
            }

            parent = parent.GetVisualParent();
        }

        return null;
    }
}