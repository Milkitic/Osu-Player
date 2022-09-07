using System.Buffers;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.Mixing;

internal sealed class LoopProvider : IDisposable
{
    private readonly BalanceSampleProvider _balanceProvider;
    private readonly byte[] _byteArray;
    private readonly LoopStream _loopStream;
    private readonly MemoryStream _memoryStream;
    private readonly EnhancedVolumeSampleProvider _volumeProvider;
    private readonly RawSourceWaveStream _waveStream;
    private bool _isAdded;

    public LoopProvider(BalanceSampleProvider balanceProvider,
        EnhancedVolumeSampleProvider volumeProvider,
        MemoryStream memoryStream,
        RawSourceWaveStream waveStream,
        LoopStream loopStream,
        byte[] byteArray)
    {
        _balanceProvider = balanceProvider;
        _volumeProvider = volumeProvider;
        _memoryStream = memoryStream;
        _waveStream = waveStream;
        _loopStream = loopStream;
        _byteArray = byteArray;
    }

    public void Dispose()
    {
        try
        {
            _loopStream.Dispose();
            _waveStream.Dispose();
            _memoryStream.Dispose();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(_byteArray);
        }
    }

    public void SetBalance(float balance)
    {
        _balanceProvider.Balance = balance;
    }

    public void SetVolume(float volume)
    {
        _volumeProvider.Volume = volume;
    }

    public void AddTo(MixingSampleProvider? mixer)
    {
        if (_isAdded) return;
        mixer?.AddMixerInput(_balanceProvider);
        _isAdded = true;
    }

    public void RemoveFrom(MixingSampleProvider? mixer)
    {
        mixer?.RemoveMixerInput(_balanceProvider);
        _isAdded = false;
    }
}