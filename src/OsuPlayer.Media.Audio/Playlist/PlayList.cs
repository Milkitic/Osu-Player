using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Media.Audio.Playlist;

public readonly record struct PlaylistSelectionChange(
    BeatmapContext? Previous,
    BeatmapContext? Current)
{
    public bool Changed => !Equals(Previous, Current);
}

public partial class PlayList : ObservableObject
{
    public event Action? SongListChanged;

    private readonly IPlayerDataStore _playerData;
    private readonly IUiThreadDispatcher _uiThreadDispatcher;
    private readonly Action? _onSongListChanged;
    private readonly Action<PlaylistMode>? _onModeChanged;

    private readonly List<int> _playOrder = new();
    private PlaylistMode _mode;

    public PlayList()
        : this(new PlayerDataService(), Execute.UiThreadDispatcher, null)
    {
    }

    public PlayList(IPlayerDataStore playerData)
        : this(playerData, Execute.UiThreadDispatcher, null)
    {
    }

    public PlayList(
        IPlayerDataStore playerData,
        IUiThreadDispatcher uiThreadDispatcher,
        Action? onSongListChanged,
        Action<PlaylistMode>? onModeChanged = null)
    {
        _playerData = playerData;
        _uiThreadDispatcher = uiThreadDispatcher;

        SongList = new ObservableCollection<Beatmap>();
        SongList.CollectionChanged += SongList_CollectionChanged;

        _onSongListChanged = onSongListChanged;
        _onModeChanged = onModeChanged;
    }

    [ObservableProperty]
    public partial ObservableCollection<Beatmap> SongList { get; private set; }

    partial void OnSongListChanged(ObservableCollection<Beatmap> value)
    {
        RebuildPlayOrder();
        SyncPointerToCurrent();
        NotifySongListChanged();
    }

    [ObservableProperty]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032", Justification = "ObservableProperty")]
    public partial BeatmapContext? CurrentInfo { get; private set; }

    [ObservableProperty]
    public partial BeatmapContext? PreInfo { get; private set; }

    [ObservableProperty]
    public partial int IndexPointer { get; private set; } = -1;

    public PlaylistMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode) return;

            var randomModeChanged = IsRandomMode(value) != IsRandom;
            _mode = value;
            if (randomModeChanged)
            {
                RebuildPlayOrder();
                SyncPointerToCurrent();
            }

            _onModeChanged?.Invoke(_mode);
            OnPropertyChanged(nameof(Mode));
        }
    }

    public bool HasCurrent => CurrentInfo != null;
    public bool HasItems => SongList.Count > 0;
    public bool IsLoop => _mode is PlaylistMode.Loop or PlaylistMode.LoopRandom;
    public bool IsRandom => IsRandomMode(_mode);
    public bool IsFirst => IndexPointer <= 0;
    public bool IsLast => _playOrder.Count == 0 || IndexPointer >= _playOrder.Count - 1;

    public async Task<PlaylistSelectionChange> ReplaceAsync(IEnumerable<Beatmap> beatmaps, bool startAnew)
    {
        ArgumentNullException.ThrowIfNull(beatmaps);

        var nextItems = beatmaps.Where(static beatmap => beatmap != null).ToList();
        var previous = CurrentInfo;

        SongList.CollectionChanged -= SongList_CollectionChanged;
        _uiThreadDispatcher.Send(() => SongList = new ObservableCollection<Beatmap>(nextItems));
        SongList.CollectionChanged += SongList_CollectionChanged;

        if (SongList.Count == 0)
        {
            return ClearSelection(previous);
        }

        if (startAnew || previous == null)
        {
            return await SelectOrderIndexAsync(0, previous).ConfigureAwait(false);
        }

        var songIndex = SongList.IndexOf(previous.Beatmap);
        if (songIndex < 0)
        {
            return await SelectOrderIndexAsync(0, previous).ConfigureAwait(false);
        }

        IndexPointer = _playOrder.IndexOf(songIndex);
        return new PlaylistSelectionChange(previous, CurrentInfo);
    }

    public async Task<PlaylistSelectionChange> AddOrSwitchToAsync(Beatmap beatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);

        if (!SongList.Contains(beatmap))
        {
            _uiThreadDispatcher.Send(() => SongList.Add(beatmap));
        }

        var songIndex = SongList.IndexOf(beatmap);
        var orderIndex = _playOrder.IndexOf(songIndex);
        return await SelectOrderIndexAsync(orderIndex).ConfigureAwait(false);
    }

    public Task<PlaylistSelectionChange> SelectFirstAsync()
        => SelectOrderIndexAsync(0);

    public Task<PlaylistSelectionChange> MoveNextAsync(bool wrap)
    {
        if (CurrentInfo == null)
        {
            return SelectOrderIndexAsync(Math.Max(IndexPointer, 0));
        }

        var next = IndexPointer + 1;
        if (next >= _playOrder.Count)
        {
            next = wrap ? 0 : _playOrder.Count - 1;
        }

        return SelectOrderIndexAsync(next);
    }

    public Task<PlaylistSelectionChange> MovePreviousAsync(bool wrap)
    {
        if (CurrentInfo == null)
        {
            return SelectOrderIndexAsync(Math.Max(IndexPointer, 0));
        }

        var previous = IndexPointer - 1;
        if (previous < 0)
        {
            previous = wrap ? _playOrder.Count - 1 : 0;
        }

        return SelectOrderIndexAsync(previous);
    }

    public Task<PlaylistSelectionChange> RemoveAsync(IEnumerable<Beatmap> beatmaps)
    {
        ArgumentNullException.ThrowIfNull(beatmaps);

        var toRemove = beatmaps.Where(static beatmap => beatmap != null).ToHashSet();
        if (toRemove.Count == 0)
        {
            return Task.FromResult(new PlaylistSelectionChange(CurrentInfo, CurrentInfo));
        }

        var previous = CurrentInfo;
        var currentRemoved = previous != null && toRemove.Contains(previous.Beatmap);
        var fallbackOrderIndex = IndexPointer;

        SongList.CollectionChanged -= SongList_CollectionChanged;
        _uiThreadDispatcher.Send(() =>
        {
            foreach (var beatmap in toRemove)
            {
                SongList.Remove(beatmap);
            }
        });
        SongList.CollectionChanged += SongList_CollectionChanged;

        RebuildPlayOrder();
        NotifySongListChanged();

        if (SongList.Count == 0)
        {
            return Task.FromResult(ClearSelection(previous));
        }

        if (!currentRemoved)
        {
            SyncPointerToCurrent();
            return Task.FromResult(new PlaylistSelectionChange(previous, CurrentInfo));
        }

        var nextOrderIndex = Math.Min(Math.Max(fallbackOrderIndex, 0), _playOrder.Count - 1);
        return SelectOrderIndexAsync(nextOrderIndex, previous);
    }

    public void InitializeEmptyCurrentInfo()
    {
        PreInfo = CurrentInfo;
        CurrentInfo = new BeatmapContext();
        IndexPointer = -1;
    }

    private async Task<PlaylistSelectionChange> SelectOrderIndexAsync(int orderIndex)
        => await SelectOrderIndexAsync(orderIndex, CurrentInfo).ConfigureAwait(false);

    private async Task<PlaylistSelectionChange> SelectOrderIndexAsync(
        int orderIndex,
        BeatmapContext? previous)
    {
        if (_playOrder.Count == 0)
        {
            return ClearSelection(previous);
        }

        orderIndex = Math.Clamp(orderIndex, 0, _playOrder.Count - 1);
        PreInfo = previous;
        IndexPointer = orderIndex;
        CurrentInfo = await BeatmapContext.CreateAsync(SongList[_playOrder[orderIndex]], _playerData)
            .ConfigureAwait(false);
        return new PlaylistSelectionChange(previous, CurrentInfo);
    }

    private PlaylistSelectionChange ClearSelection(BeatmapContext? previous)
    {
        PreInfo = previous;
        CurrentInfo = null;
        IndexPointer = -1;
        _playOrder.Clear();
        return new PlaylistSelectionChange(previous, null);
    }

    private void RebuildPlayOrder()
    {
        _playOrder.Clear();
        _playOrder.AddRange(Enumerable.Range(0, SongList.Count));
        if (IsRandom)
        {
            _playOrder.Shuffle();
        }
    }

    private void SyncPointerToCurrent()
    {
        if (_playOrder.Count == 0)
        {
            IndexPointer = -1;
            return;
        }

        if (CurrentInfo != null)
        {
            var songIndex = SongList.IndexOf(CurrentInfo.Beatmap);
            var orderIndex = songIndex < 0 ? -1 : _playOrder.IndexOf(songIndex);
            if (orderIndex >= 0)
            {
                IndexPointer = orderIndex;
                return;
            }
        }

        IndexPointer = Math.Clamp(IndexPointer, 0, _playOrder.Count - 1);
    }

    private void SongList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildPlayOrder();
        SyncPointerToCurrent();
        NotifySongListChanged();
    }

    private void NotifySongListChanged()
    {
        SongListChanged?.Invoke();
        _onSongListChanged?.Invoke();
    }

    private static bool IsRandomMode(PlaylistMode mode)
        => mode is PlaylistMode.Random or PlaylistMode.LoopRandom;
}
