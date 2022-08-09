using System;
using System.Buffers;
using System.IO;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio.New;

internal sealed class LoopProvider : IDisposable
{
    private readonly BalanceSampleProvider _balanceProvider;
    private readonly EnhancedVolumeSampleProvider _volumeProvider;
    private readonly MemoryStream _memoryStream;
    private readonly RawSourceWaveStream _waveStream;
    private readonly LoopStream _loopStream;
    private readonly byte[] _byteArray;
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
}