using System.Buffers;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

internal class LoopProviderHelper
{
    private readonly Dictionary<SlideChannel, LoopProvider> _dictionary = new();

    public bool ShouldRemoveAll(SlideChannel channel)
    {
        return _dictionary.ContainsKey(channel);
    }

    public bool ChangeAllVolumes(float volume, float volumeFactor = 1.25f)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetVolume(volume * volumeFactor);
        }

        return true;
    }

    public bool ChangeAllBalances(float balance, float balanceFactor = 1)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetBalance(balance * balanceFactor);
        }

        return true;
    }

    public bool ChangeVolume(SlideChannel slideChannel, float volume, float volumeFactor = 1.25f)
    {
        if (!_dictionary.TryGetValue(slideChannel, out var loopProvider)) return false;
        loopProvider.SetVolume(volume * volumeFactor);
        return true;
    }

    public bool ChangeBalance(SlideChannel slideChannel, float balance, float balanceFactor = 1)
    {
        if (!_dictionary.TryGetValue(slideChannel, out var loopProvider)) return false;
        loopProvider.SetBalance(balance * balanceFactor);
        return true;
    }

    public bool Remove(SlideChannel slideChannel, MixingSampleProvider? mixer)
    {
        if (_dictionary.TryGetValue(slideChannel, out var loopProvider))
        {
            loopProvider.RemoveFrom(mixer);
            loopProvider.Dispose();
            return _dictionary.Remove(slideChannel);
        }

        return false;
    }

    public void RemoveAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.RemoveFrom(mixer);
            loopProvider.Dispose();
            _dictionary.Remove(channel);
        }
    }

    public void PauseAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary)
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.RemoveFrom(mixer);
        }
    }

    public void RecoverAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary)
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.AddTo(mixer);
        }
    }

    public void Create(ControlNode controlNode,
        CachedSound cachedSound,
        MixingSampleProvider mixer,
        float volume,
        float balance,
        float volumeFactor = 1.25f,
        float balanceFactor = 1)
    {
        var slideChannel = controlNode.SlideChannel;
        Remove(slideChannel, mixer);

        var audioDataLength = cachedSound.AudioData.Length * sizeof(float);
        var byteArray = ArrayPool<byte>.Shared.Rent(audioDataLength);
        Buffer.BlockCopy(cachedSound.AudioData, 0, byteArray, 0, audioDataLength);

        var memoryStream = new MemoryStream(byteArray, 0, audioDataLength);
        var waveStream = new RawSourceWaveStream(memoryStream, cachedSound.WaveFormat);
        var loopStream = new LoopStream(waveStream);
        var volumeProvider = new EnhancedVolumeSampleProvider(loopStream.ToSampleProvider())
        {
            Volume = volume * volumeFactor
        };
        var balanceProvider = new BalanceSampleProvider(volumeProvider)
        {
            Balance = balance * balanceFactor
        };

        var loopProvider = new LoopProvider(balanceProvider, volumeProvider, memoryStream, waveStream, loopStream,
            byteArray);
        _dictionary.Add(slideChannel, loopProvider);
        loopProvider.AddTo(mixer);
    }
}