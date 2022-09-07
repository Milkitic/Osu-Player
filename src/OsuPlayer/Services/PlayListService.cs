#nullable enable

using System.Collections.ObjectModel;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Services;

public class PlayListService : VmBase
{
    private readonly Random _random = new();
    private int[] _pathIndexList = Array.Empty<int>(); // [3,0,2,1,4]
    private PlayListMode _playListMode;
    private int? _pointer;

    public PlayListService()
    {
        PathList = new ObservableCollection<string>();
    }

    public string? CurrentPath => Pointer == null ? null : PathList[_pathIndexList[Pointer.Value]];

    public ObservableCollection<string> PathList { get; }

    public PlayListMode PlayListMode
    {
        get => _playListMode;
        set
        {
            if (_playListMode == value) return;

            var preIsRandom = _playListMode is PlayListMode.Random or PlayListMode.LoopRandom;
            _playListMode = value;
            var isRandom = value is PlayListMode.Random or PlayListMode.LoopRandom;
            if (preIsRandom != isRandom)
            {
                RebuildPathIndexes();
            }

            OnPropertyChanged();
        }
    }

    public int? Pointer
    {
        get => _pointer;
        private set
        {
            if (value == _pointer) return;
            _pointer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentPath));
        }
    }

    public void SetPathList(IEnumerable<string> paths, bool resetToCurrentPath)
    {
        var currentPath = CurrentPath;

        Execute.OnUiThread(() =>
        {
            PathList.Clear();
            foreach (var path in paths)
            {
                PathList.Add(path);
            }
        });
        RebuildPathIndexes();

        if (resetToCurrentPath && currentPath != null)
        {
            SetPointerByPath(currentPath, false);
        }
    }

    public int? SetPointerByPath(string path, bool autoAppend)
    {
        var pathIndex = PathList.IndexOf(path);
        if (pathIndex < 0)
        {
            if (autoAppend)
            {
                AppendPaths(path);
                pathIndex = PathList.IndexOf(path);
                if (pathIndex < 0)
                {
                    Pointer = null;
                    return null;
                }
            }
            else
            {
                Pointer = null;
                return null;
            }
        }

        Pointer = Array.IndexOf(_pathIndexList, pathIndex);
        return Pointer;
    }

    public void RemovePaths(params string[] paths)
    {
        RemovePaths((IEnumerable<string>)paths);
    }

    public void RemovePaths(IEnumerable<string> paths)
    {
        var currentPath = CurrentPath;

        var array = paths as string[] ?? paths.ToArray();
        if (array.Length == 0) return;

        foreach (var path in array)
        {
            var success = App.Current.Dispatcher.Invoke(() => PathList.Remove(path));
            if (success && path.Equals(currentPath, StringComparison.Ordinal))
            {
                currentPath = null;
                Pointer = null;
            }
        }

        RebuildPathIndexes();

        if (currentPath == null) return;

        var pathIndex = PathList.IndexOf(currentPath);
        var pointer = Array.IndexOf(_pathIndexList, pathIndex);
        Pointer = pointer;
    }

    public void AppendPaths(params string[] paths)
    {
        var currentPath = CurrentPath;

        if (paths.Length <= 0) return;

        var hashSet = PathList.ToHashSet(StringComparer.Ordinal);
        Execute.OnUiThread(() =>
        {
            foreach (var path in paths.Where(k => !hashSet.Contains(k)))
            {
                PathList.Add(path);
            }
        });

        RebuildPathIndexes();

        if (currentPath == null) return;

        var pathIndex = PathList.IndexOf(currentPath);
        var pointer = Array.IndexOf(_pathIndexList, pathIndex);
        Pointer = pointer;
    }

    public string? GetAndSetNextPath(PlayDirection playDirection, bool forceLoop)
    {
        if (Pointer == null && PathList.Count > 0)
        {
            return SetPathByPointer(0);
        }

        var currentPath = CurrentPath;

        if (forceLoop || PlayListMode is PlayListMode.Loop or PlayListMode.LoopRandom)
        {
            if (playDirection == PlayDirection.Next)
            {
                return SetPathByPointer(Pointer == _pathIndexList.Length - 1 ? 0 : Pointer + 1);
            }

            return SetPathByPointer(Pointer == 0 ? _pathIndexList.Length - 1 : Pointer - 1);
        }

        if (PlayListMode is PlayListMode.Normal or PlayListMode.Random)
        {
            if (playDirection == PlayDirection.Next)
            {
                if (Pointer == _pathIndexList.Length - 1) return null;
                return SetPathByPointer(Pointer + 1);
            }

            if (Pointer == 0) return null;
            return SetPathByPointer(Pointer - 1);
        }

        if (PlayListMode == PlayListMode.SingleLoop) return currentPath;
        if (PlayListMode == PlayListMode.Single) return null;

        throw new ArgumentOutOfRangeException(nameof(PlayListMode), PlayListMode, null);
    }

    private void RebuildPathIndexes()
    {
        if (PathList.Count == 0) return;
        var array = Enumerable.Range(0, PathList.Count).ToArray();
        if (PlayListMode is PlayListMode.LoopRandom or PlayListMode.Random)
        {
            Shuffle(array);
        }

        _pathIndexList = array;
    }

    private string? SetPathByPointer(int? value)
    {
        if (value < 0)
        {
            value = 0;
        }
        else if (value > PathList.Count - 1)
        {
            value = PathList.Count - 1;
        }

        var currentPath = value == null ? null : PathList[_pathIndexList[value.Value]];
        Pointer = value;

        return currentPath;
    }

    private void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = _random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}