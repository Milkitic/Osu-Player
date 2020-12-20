using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
    }
}
