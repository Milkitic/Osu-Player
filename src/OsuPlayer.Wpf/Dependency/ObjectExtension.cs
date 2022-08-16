using System.Windows;
using System.Windows.Media;

namespace Milki.OsuPlayer.Wpf.Dependency;

public static class ObjectExtension
{
    public static T GetParentObjectByName<T>(this DependencyObject obj, string name) where T : FrameworkElement
    {
        DependencyObject parent = VisualTreeHelper.GetParent(obj);
        while (parent != null)
        {
            if (parent is T && (((T)parent).Name == name || string.IsNullOrEmpty(name)))
            {
                return (T)parent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    public static T GetParentObject<T>(this FrameworkElement obj) where T : FrameworkElement
    {
        return FindParentObjects(obj) as T;
    }

    public static FrameworkElement FindParentObjects(this FrameworkElement obj, params Type[] types)
    {
        DependencyObject parent = VisualTreeHelper.GetParent(obj);
        while (parent != null)
        {
            if (parent is FrameworkElement fe)
            {
                if (types.Length == 0)
                    return fe;

                var type = fe.GetType();
                if (types.Any(k => type.IsSubclassOf(k) || k == type))
                {
                    return fe;
                }
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    public static T FindChildObjects<T>(this DependencyObject parent, string childName) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        T foundChild = null;

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            T childType = child as T;

            if (childType == null)
            {
                foundChild = FindChildObjects<T>(child, childName);

                if (foundChild != null) break;
            }
            else
            if (!string.IsNullOrEmpty(childName))
            {
                var frameworkElement = child as FrameworkElement;

                if (frameworkElement != null && frameworkElement.Name == childName)
                {
                    foundChild = (T)child;
                    break;
                }
                else
                {
                    foundChild = FindChildObjects<T>(child, childName);

                    if (foundChild != null)
                    {
                        break;
                    }
                }
            }
            else
            {
                foundChild = (T)child;
                break;
            }
        }

        return foundChild;
    }
}