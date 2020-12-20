
using System;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

namespace Milky.OsuPlayer.Presentation
{
    public interface IWindowBase
    {
        bool IsClosed { get; set; }
    }

    [MarkupExtensionReturnType(typeof(FrameworkElement))]
    public class RootObject : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var rootObjectProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
            return rootObjectProvider?.RootObject;
        }
    }

    [MarkupExtensionReturnType(typeof(object))]
    public class RootObjectDataContext : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var rootObjectProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
            return (rootObjectProvider?.RootObject as FrameworkElement)?.DataContext;
        }
    }
}