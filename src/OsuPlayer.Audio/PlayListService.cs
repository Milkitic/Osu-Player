using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Audio;

public class PlayListService
{
    private readonly Random _random = new();
    private PlaylistMode _mode;

    private int? _pointer;
    private int[] _pathIndexList = Array.Empty<int>(); // [3,0,2,1,4]
    private List<string> _pathList = new(); // ["a","b","c","d","e"]

    public PlaylistMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;

            var preIsRandom = _mode is PlaylistMode.Random or PlaylistMode.LoopRandom;
            _mode = value;
            var isRandom = value is PlaylistMode.Random or PlaylistMode.LoopRandom;
            if (preIsRandom != isRandom)
            {
                RebuildPathIndexes();
            }
        }
    }

    public void SetPathList(IEnumerable<string> paths, bool resetToCurrentPath)
    {
        var currentPath = GetCurrentPath();

        _pathList = new List<string>(paths);
        RebuildPathIndexes();

        if (resetToCurrentPath && currentPath != null)
        {
            SetPointerByPath(currentPath, false);
        }
    }

    public int? SetPointerByPath(string path, bool autoAppend)
    {
        var pathIndex = _pathList.IndexOf(path);
        if (pathIndex < 0)
        {
            if (autoAppend)
            {
                AppendPaths(path);
                pathIndex = _pathList.IndexOf(path);
                if (pathIndex < 0)
                {
                    _pointer = null;
                    return null;
                }
            }
            else
            {
                _pointer = null;
                return null;
            }
        }

        _pointer = Array.IndexOf(_pathIndexList, pathIndex);
        return _pointer;
    }

    public void RemovePaths(params string[] paths)
    {
        var currentPath = GetCurrentPath();

        if (paths.Length <= 0) return;

        foreach (var path in paths)
        {
            var success = _pathList.Remove(path);
            if (success && path.Equals(currentPath, StringComparison.Ordinal))
            {
                currentPath = null;
                _pointer = null;
            }
        }

        RebuildPathIndexes();

        if (currentPath == null) return;

        var pathIndex = _pathList.IndexOf(currentPath);
        var pointer = Array.IndexOf(_pathIndexList, pathIndex);
        _pointer = pointer;
    }

    public void AppendPaths(params string[] paths)
    {
        var currentPath = GetCurrentPath();

        if (paths.Length <= 0) return;

        var hashSet = _pathList.ToHashSet(StringComparer.Ordinal);

        _pathList.AddRange(paths.Where(k => !hashSet.Contains(k)));
        RebuildPathIndexes();

        if (currentPath == null) return;

        var pathIndex = _pathList.IndexOf(currentPath);
        var pointer = Array.IndexOf(_pathIndexList, pathIndex);
        _pointer = pointer;
    }

    public string? GetCurrentPath()
    {
        return _pointer == null ? null : _pathList[_pathIndexList[_pointer.Value]];
    }

    public string? GetAndSetNextPath(PlayDirection playDirection, bool forceLoop)
    {
        if (_pointer == null && _pathList.Count > 0)
        {
            return SetPathByPointer(0);
        }

        var currentPath = GetCurrentPath();

        if (forceLoop || Mode is PlaylistMode.Loop or PlaylistMode.LoopRandom)
        {
            if (playDirection == PlayDirection.Next)
            {
                return SetPathByPointer(_pointer == _pathIndexList.Length - 1 ? 0 : _pointer + 1);
            }

            return SetPathByPointer(_pointer == 0 ? _pathIndexList.Length - 1 : _pointer - 1);
        }

        if (Mode is PlaylistMode.Normal or PlaylistMode.Random)
        {
            if (playDirection == PlayDirection.Next)
            {
                if (_pointer == _pathIndexList.Length - 1) return null;
                return SetPathByPointer(_pointer + 1);
            }

            if (_pointer == 0) return null;
            return SetPathByPointer(_pointer - 1);
        }

        if (Mode == PlaylistMode.SingleLoop) return currentPath;
        if (Mode == PlaylistMode.Single) return null;

        throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null);
    }

    private void RebuildPathIndexes()
    {
        if (_pathList.Count == 0) return;
        var array = Enumerable.Range(0, _pathList.Count).ToArray();
        if (Mode is PlaylistMode.LoopRandom or PlaylistMode.Random)
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
        else if (value > _pathList.Count - 1)
        {
            value = _pathList.Count - 1;
        }

        var currentPath = value == null ? null : _pathList[_pathIndexList[value.Value]];
        _pointer = value;

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