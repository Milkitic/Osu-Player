using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Milki.OsuPlayer.Presentation.Dependency
{
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
                    if (types.Any(k => type.IsSubclassOf(k)))
                    {
                        return fe;
                    }
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
