using Avalonia.Controls;

namespace OsuPlayer.Views.UserControls;

public partial class AddCollectionControl : UserControl
{
    public AddCollectionControl()
    {
        InitializeComponent();
    }

    public string CollectionNameValue => CollectionName.Text?.Trim() ?? string.Empty;

    public void FocusCollectionName()
    {
        CollectionName.Focus();
    }
}
