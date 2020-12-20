using Milky.OsuPlayer.Presentation.Interaction;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xaml;

namespace Milky.OsuPlayer.UserControls.ItemsControls
{
    /// <summary>
    /// BeatmapDataGrid.xaml 的交互逻辑
    /// </summary>
    public partial class BeatmapDataGrid : BeatmapItemsControlBase
    {

        public BeatmapDataGrid()
        {
            InitializeComponent();
        }
    }

    public class BeatmapItemsControlBase : UserControl
    {
        public BeatmapItemsControlBase()
        {
        }

        public static readonly DependencyProperty DataListProperty = DependencyProperty.Register(
            "DataList",
            typeof(ObservableCollection<OrderedBeatmap>),
            typeof(BeatmapItemsControlBase),
            new FrameworkPropertyMetadata(null, OnDataListChanged
            )
        );

        private static void OnDataListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public ObservableCollection<OrderedBeatmap> DataList
        {
            get => (ObservableCollection<OrderedBeatmap>)GetValue(DataListProperty);
            set => SetValue(DataListProperty, value);
        }
        public ICommand TestCommand => new DelegateCommand<OrderedBeatmap>(param =>
        {

        });
    }

    [MarkupExtensionReturnType(typeof(BeatmapDataGrid))]
    class RootBeatmapDataGrid : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) =>
            ((IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider)))?.RootObject;
    }
}
