using System;
using System.Threading;
using System.Threading.Tasks;
using Anotar.NLog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OsuPlayer.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OsuPlayer.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SearchPage : Page
{
    private TaskCompletionSource? _tcs;
    private CancellationTokenSource? _cts;

    public SearchPage()
    {
        this.InitializeComponent();
    }

    private async void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchTextBox.Text;
        if (!await DelayTextChanged().ConfigureAwait(false)) return;

        await using var dbContext = new ApplicationDbContext();
        var results = await dbContext
            .SearchPlayItemsAsync(searchText, BeatmapOrderOptions.Artist, 0, 5000)
            .ConfigureAwait(false);

        LogTo.Info("Find " + results.Results.Count + " results.");

        DispatcherQueue.TryEnqueue(() =>
        {
            GridView.ItemsSource = results.Results;
        });
    }

    private async Task<bool> DelayTextChanged()
    {
        if (_tcs != null)
        {
            _tcs.TrySetCanceled();
            _cts!.Dispose();
        }

        _tcs = new TaskCompletionSource();
        _cts = new CancellationTokenSource(300);
        _cts.Token.Register(() => _tcs?.TrySetResult());

        try
        {
            await _tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return false;
        }

        return true;
    }

    private void SearchPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
    }
}