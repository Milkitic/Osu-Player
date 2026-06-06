using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using Xunit;
using Xunit.Abstractions;

namespace OsuPlayer.Media.Audio.Tests;

public sealed class AudioDecoderReferenceTests
{
    private const string FixtureRootEnvironmentVariable = "OSUPLAYER_AUDIO_SYNC_FIXTURES";
    private const string CalibrationOutputEnvironmentVariable = "OSUPLAYER_AUDIO_SYNC_CALIBRATION_OUTPUT";
    private const int OutputChannels = 2;

    private readonly ITestOutputHelper _output;

    public AudioDecoderReferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReferenceCorpus_AllCasesStayWithinOffsetBudget()
    {
        var root = Environment.GetEnvironmentVariable(FixtureRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            _output.WriteLine(
                $"Set {FixtureRootEnvironmentVariable} to run decode black-box reference fixtures.");
            return;
        }

        var cases = AudioReferenceCase.Discover(root).ToArray();
        if (cases.Length == 0)
        {
            _output.WriteLine($"No reference fixtures found under {root}.");
            return;
        }

        var failures = new List<string>();
        var calibrations = new List<AudioDecodeCalibration>();
        foreach (var testCase in cases)
        {
            try
            {
                var measured = await EvaluateCaseAsync(
                    testCase,
                    calibrationProvider: null,
                    validate: false,
                    useAutomaticMp3GaplessCorrection: false);
                var calibration = await CreateCalibrationAsync(testCase, measured);
                var corrected = await EvaluateCaseAsync(
                    testCase,
                    AudioDecodeCalibrationStore.FromCalibrations([calibration]),
                    validate: true,
                    useAutomaticMp3GaplessCorrection: false);
                var automatic = await TryEvaluateAutomaticMp3GaplessCorrectionAsync(testCase);

                calibrations.Add(calibration);

                var rawOffsetMilliseconds = measured.OffsetFrames * 1000.0 / measured.SampleRate;
                var correctedOffsetMilliseconds = corrected.OffsetFrames * 1000.0 / corrected.SampleRate;
                var automaticSummary = automatic is null
                    ? string.Empty
                    : $", automaticOffset={automatic.OffsetFrames} frames, " +
                      $"automaticLengthDelta={automatic.DurationDeltaFrames} frames";
                _output.WriteLine(
                    $"{testCase.Name}: rawOffset={measured.OffsetFrames} frames ({rawOffsetMilliseconds:N3}ms), " +
                    $"rawLengthDelta={measured.DurationDeltaFrames} frames, " +
                    $"correctedOffset={corrected.OffsetFrames} frames ({correctedOffsetMilliseconds:N3}ms), " +
                    $"corr={corrected.Correlation:N5}, correctedLengthDelta={corrected.DurationDeltaFrames} frames" +
                    automaticSummary);
            }
            catch (Exception ex)
            {
                failures.Add($"{testCase.Name}: {ex.Message}");
            }
        }

        Assert.Empty(failures);

        var calibrationOutput = Environment.GetEnvironmentVariable(CalibrationOutputEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(calibrationOutput))
        {
            AudioDecodeCalibrationStore.Save(calibrationOutput, calibrations);
            _output.WriteLine($"Wrote {calibrations.Count} audio decode calibrations to {calibrationOutput}.");
        }
    }

    [Fact]
    public void OffsetEstimator_DetectsCandidateLead()
    {
        var reference = CreateSyntheticSignal(44_100, leadingFrames: 2_000, totalFrames: 20_000);
        var candidate = CreateSyntheticSignal(44_100, leadingFrames: 1_500, totalFrames: 20_000);

        var options = new AudioReferenceOptions
        {
            MaxSearchMilliseconds = 100,
            WindowMilliseconds = 80
        };

        var result = AudioAlignmentEstimator.Estimate(reference, candidate, 44_100, options);

        Assert.InRange(result.OffsetFrames, -501, -499);
        Assert.True(result.Correlation > 0.99);
    }

    [Fact]
    public void OffsetEstimator_DetectsCandidateLag()
    {
        var reference = CreateSyntheticSignal(44_100, leadingFrames: 1_500, totalFrames: 20_000);
        var candidate = CreateSyntheticSignal(44_100, leadingFrames: 2_000, totalFrames: 20_000);

        var options = new AudioReferenceOptions
        {
            MaxSearchMilliseconds = 100,
            WindowMilliseconds = 80
        };

        var result = AudioAlignmentEstimator.Estimate(reference, candidate, 44_100, options);

        Assert.InRange(result.OffsetFrames, 499, 501);
        Assert.True(result.Correlation > 0.99);
    }

    [Fact]
    public async Task CalibrationStore_CorrectsEarlyCandidateAndLengthDelta()
    {
        const int sampleRate = 44_100;
        var reference = CreateSyntheticSignal(sampleRate, leadingFrames: 2_000, totalFrames: 20_000);
        var candidate = CreateSyntheticSignal(sampleRate, leadingFrames: 1_500, totalFrames: 19_000);
        var candidateWave = CreateStereoPcm16Wave(candidate, sampleRate);
        var calibration = new AudioDecodeCalibration
        {
            SourceHash = AudioSourceHash.Compute(candidateWave),
            SampleRate = sampleRate,
            OffsetFrames = -500,
            DurationDeltaFrames = -1_000,
            Correlation = 1,
            Name = nameof(CalibrationStore_CorrectsEarlyCandidateAndLengthDelta)
        };

        var decoded = await DecodeWithCalibrationAsync(candidateWave, sampleRate, calibration);
        var result = AudioAlignmentEstimator.Estimate(
            reference,
            decoded.ToMono(),
            sampleRate,
            new AudioReferenceOptions());

        Assert.Equal(reference.Length, decoded.FrameCount);
        Assert.InRange(result.OffsetFrames, -1, 1);
        Assert.True(result.Correlation > 0.99);
    }

    [Fact]
    public async Task CalibrationStore_CorrectsLateCandidate()
    {
        const int sampleRate = 44_100;
        var reference = CreateSyntheticSignal(sampleRate, leadingFrames: 1_500, totalFrames: 20_000);
        var candidate = CreateSyntheticSignal(sampleRate, leadingFrames: 2_000, totalFrames: 20_000);
        var candidateWave = CreateStereoPcm16Wave(candidate, sampleRate);
        var calibration = new AudioDecodeCalibration
        {
            SourceHash = AudioSourceHash.Compute(candidateWave),
            SampleRate = sampleRate,
            OffsetFrames = 500,
            DurationDeltaFrames = 0,
            Correlation = 1,
            Name = nameof(CalibrationStore_CorrectsLateCandidate)
        };

        var decoded = await DecodeWithCalibrationAsync(candidateWave, sampleRate, calibration);
        var result = AudioAlignmentEstimator.Estimate(
            reference,
            decoded.ToMono(),
            sampleRate,
            new AudioReferenceOptions());

        Assert.Equal(reference.Length, decoded.FrameCount);
        Assert.InRange(result.OffsetFrames, -1, 1);
        Assert.True(result.Correlation > 0.99);
    }

    [Theory]
    [InlineData(576, 0, 1_105, 0, 1_105)]
    [InlineData(576, 600, 1_105, 71, 1_176)]
    public void Mp3GaplessInfo_ReadsLavcInfoHeaderDelayAndPadding(
        int encoderDelay,
        int encoderPadding,
        int expectedStartSkip,
        int expectedEndDiscard,
        int expectedTotalDiscard)
    {
        var mp3 = CreateLavcInfoMp3Frame(encoderDelay, encoderPadding);

        Assert.True(Mp3GaplessInfo.TryRead(mp3, out var info));
        Assert.Equal(48_000, info.SampleRate);
        Assert.Equal(expectedStartSkip, info.StartSkipSamples);
        Assert.Equal(expectedEndDiscard, info.EndDiscardSamples);
        Assert.Equal(expectedTotalDiscard, info.TotalDiscardSamples);
    }

    private static async Task<AudioAlignmentResult?> TryEvaluateAutomaticMp3GaplessCorrectionAsync(
        AudioReferenceCase testCase)
    {
        var fileData = File.ReadAllBytes(testCase.InputPath);
        if (!Mp3GaplessInfo.TryRead(fileData, out _))
            return null;

        return await EvaluateCaseAsync(
            testCase,
            calibrationProvider: null,
            validate: true,
            useAutomaticMp3GaplessCorrection: true);
    }

    private static async Task<AudioAlignmentResult> EvaluateCaseAsync(
        AudioReferenceCase testCase,
        IAudioDecodeCalibrationProvider? calibrationProvider,
        bool validate,
        bool useAutomaticMp3GaplessCorrection)
    {
        var reference = Pcm16Audio.ReadWave(testCase.ReferencePath);
        if (reference.Channels != OutputChannels)
        {
            throw new InvalidDataException(
                $"Reference WAV must be stereo PCM16 because AudioCacheManager caches music as stereo PCM16. " +
                $"Actual channels: {reference.Channels}.");
        }

        var cacheManager = new AudioCacheManager(
            NullLogger<AudioCacheManager>.Instance,
            calibrationProvider,
            useAutomaticMp3GaplessCorrection);
        try
        {
            var cacheResult = await cacheManager.GetOrCreateOrEmptyFromFileAsync(
                testCase.InputPath,
                new WaveFormat(reference.SampleRate, OutputChannels));

            var cachedAudio = cacheResult.CachedAudio ??
                              throw new InvalidOperationException("Decoder returned no cached audio.");

            if (cachedAudio.WaveFormat.SampleRate != reference.SampleRate ||
                cachedAudio.WaveFormat.Channels != reference.Channels)
            {
                throw new InvalidDataException(
                    $"Decoded format {cachedAudio.WaveFormat} does not match reference " +
                    $"{reference.SampleRate}Hz/{reference.Channels}ch.");
            }

            byte[] decodedBytes;
            if (!cachedAudio.TryAcquireSpan(out var span))
            {
                throw new InvalidOperationException("Could not acquire decoded PCM span.");
            }

            try
            {
                decodedBytes = span.ToArray();
            }
            finally
            {
                cachedAudio.ReleaseSpan();
            }

            var candidate = Pcm16Audio.FromBytes(decodedBytes, reference.SampleRate, reference.Channels);
            var durationDeltaFrames = candidate.FrameCount - reference.FrameCount;
            if (validate && Math.Abs(durationDeltaFrames) > testCase.Options.DurationToleranceFrames)
            {
                throw new InvalidDataException(
                    $"Decoded length differs by {durationDeltaFrames} frames; " +
                    $"allowed {testCase.Options.DurationToleranceFrames}.");
            }

            var result = AudioAlignmentEstimator.Estimate(
                reference.ToMono(),
                candidate.ToMono(),
                reference.SampleRate,
                testCase.Options) with
            {
                SampleRate = reference.SampleRate,
                DurationDeltaFrames = durationDeltaFrames
            };

            if (validate && Math.Abs(result.OffsetFrames) > testCase.Options.AllowedOffsetFrames)
            {
                throw new InvalidDataException(
                    $"Decoded audio is {DescribeOffset(result.OffsetFrames)} relative to reference; " +
                    $"allowed {testCase.Options.AllowedOffsetFrames} frames. " +
                    $"Correlation={result.Correlation:N5}.");
            }

            if (validate && result.Correlation < testCase.Options.MinCorrelation)
            {
                throw new InvalidDataException(
                    $"Decoded audio correlation {result.Correlation:N5} is below " +
                    $"{testCase.Options.MinCorrelation:N5}.");
            }

            return result;
        }
        finally
        {
            cacheManager.ClearAll();
        }
    }

    private static async Task<AudioDecodeCalibration> CreateCalibrationAsync(AudioReferenceCase testCase,
        AudioAlignmentResult measured)
    {
        return new AudioDecodeCalibration
        {
            SourceHash = await AudioSourceHash.ComputeFileAsync(testCase.InputPath),
            SampleRate = measured.SampleRate,
            OffsetFrames = measured.OffsetFrames,
            DurationDeltaFrames = measured.DurationDeltaFrames,
            Correlation = measured.Correlation,
            Name = testCase.Name
        };
    }

    private static async Task<Pcm16Audio> DecodeWithCalibrationAsync(byte[] fileBytes, int sampleRate,
        AudioDecodeCalibration calibration)
    {
        var cacheManager = new AudioCacheManager(
            NullLogger<AudioCacheManager>.Instance,
            AudioDecodeCalibrationStore.FromCalibrations([calibration]),
            useAutomaticMp3GaplessCorrection: false);

        try
        {
            using var stream = new MemoryStream(fileBytes);
            var cacheResult = await cacheManager.TryGetOrCreateAsync(
                "synthetic-" + Guid.NewGuid(),
                stream,
                new WaveFormat(sampleRate, OutputChannels));

            var cachedAudio = cacheResult.CachedAudio ??
                              throw new InvalidOperationException("Decoder returned no cached audio.");

            byte[] decodedBytes;
            if (!cachedAudio.TryAcquireSpan(out var span))
                throw new InvalidOperationException("Could not acquire decoded PCM span.");

            try
            {
                decodedBytes = span.ToArray();
            }
            finally
            {
                cachedAudio.ReleaseSpan();
            }

            return Pcm16Audio.FromBytes(decodedBytes, cachedAudio.WaveFormat.SampleRate,
                cachedAudio.WaveFormat.Channels);
        }
        finally
        {
            cacheManager.ClearAll();
        }
    }

    private static string DescribeOffset(int offsetFrames)
    {
        if (offsetFrames == 0)
            return "aligned";

        return offsetFrames < 0
            ? $"{-offsetFrames} frames early"
            : $"{offsetFrames} frames late";
    }

    private static float[] CreateSyntheticSignal(int sampleRate, int leadingFrames, int totalFrames)
    {
        var data = new float[totalFrames];
        var state = 0x12345678u;
        for (var i = leadingFrames; i < totalFrames; i++)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            var noise = ((state >> 8) / (double)0x01000000) * 2 - 1;
            var t = (i - leadingFrames) / (double)sampleRate;
            data[i] = (float)(0.55 * noise +
                              0.25 * Math.Sin(2 * Math.PI * 997 * t) +
                              0.20 * Math.Sin(2 * Math.PI * 3211 * t));
        }

        return data;
    }

    private static byte[] CreateStereoPcm16Wave(float[] mono, int sampleRate)
    {
        using var output = new MemoryStream();
        using (var writer = new WaveFileWriter(output, new WaveFormat(sampleRate, 16, OutputChannels)))
        {
            var bytes = CreateStereoPcm16Bytes(mono);
            writer.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static byte[] CreateStereoPcm16Bytes(float[] mono)
    {
        var data = new byte[mono.Length * OutputChannels * sizeof(short)];
        for (var frame = 0; frame < mono.Length; frame++)
        {
            var value = (short)Math.Clamp(
                (int)Math.Round(mono[frame] * short.MaxValue),
                short.MinValue,
                short.MaxValue);

            for (var channel = 0; channel < OutputChannels; channel++)
            {
                var offset = (frame * OutputChannels + channel) * sizeof(short);
                BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset, sizeof(short)), value);
            }
        }

        return data;
    }

    private static byte[] CreateLavcInfoMp3Frame(int encoderDelay, int encoderPadding)
    {
        if ((uint)encoderDelay > 0xFFF)
            throw new ArgumentOutOfRangeException(nameof(encoderDelay));
        if ((uint)encoderPadding > 0xFFF)
            throw new ArgumentOutOfRangeException(nameof(encoderPadding));

        const int frameLength = 576;
        const int xingOffset = 36;
        const int id3PayloadLength = 5;

        var frame = new byte[frameLength];
        frame[0] = 0xFF;
        frame[1] = 0xFB;
        frame[2] = 0xB4;
        frame[3] = 0x01;

        var cursor = xingOffset;
        Encoding.ASCII.GetBytes("Info").CopyTo(frame.AsSpan(cursor));
        cursor += 4;

        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(cursor, sizeof(int)), 0xF);
        cursor += 4;
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(cursor, sizeof(int)), 6_001);
        cursor += 4;
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(cursor, sizeof(int)), frameLength);
        cursor += 4;
        cursor += 100;
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(cursor, sizeof(int)), 0);
        cursor += 4;

        Encoding.ASCII.GetBytes("Lavc57.33").CopyTo(frame.AsSpan(cursor));
        cursor += 9;
        cursor += 1; // Info tag revision + VBR method
        cursor += 1; // lowpass filter
        cursor += 4; // replaygain peak
        cursor += 2; // radio replaygain
        cursor += 2; // audiophile replaygain
        cursor += 1; // encoding flags + ATH type
        cursor += 1; // bitrate

        var delayAndPadding = (encoderDelay << 12) | encoderPadding;
        frame[cursor] = (byte)(delayAndPadding >> 16);
        frame[cursor + 1] = (byte)(delayAndPadding >> 8);
        frame[cursor + 2] = (byte)delayAndPadding;

        var data = new byte[10 + id3PayloadLength + frame.Length];
        Encoding.ASCII.GetBytes("ID3").CopyTo(data.AsSpan(0));
        data[3] = 4;
        data[9] = id3PayloadLength;
        frame.CopyTo(data.AsSpan(10 + id3PayloadLength));

        return data;
    }

    private sealed record AudioReferenceCase(
        string Name,
        string InputPath,
        string ReferencePath,
        AudioReferenceOptions Options)
    {
        public static IEnumerable<AudioReferenceCase> Discover(string root)
        {
            foreach (var directory in EnumerateCaseDirectories(root))
            {
                var referencePath = Path.Combine(directory, "reference.wav");
                if (!File.Exists(referencePath))
                    continue;

                var inputPath = Directory.EnumerateFiles(directory, "input.*", SearchOption.TopDirectoryOnly)
                    .Where(static path => !path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (inputPath == null)
                    continue;

                yield return new AudioReferenceCase(
                    Path.GetRelativePath(root, directory),
                    inputPath,
                    referencePath,
                    AudioReferenceOptions.Load(directory));
            }
        }

        private static IEnumerable<string> EnumerateCaseDirectories(string root)
        {
            yield return root;

            foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
                yield return directory;
        }
    }

    private sealed class AudioReferenceOptions
    {
        public int MaxSearchMilliseconds { get; init; } = 250;
        public int WindowMilliseconds { get; init; } = 100;
        public int AllowedOffsetFrames { get; init; } = 24;
        public int DurationToleranceFrames { get; init; } = 96;
        public double MinCorrelation { get; init; } = 0.985;

        public static AudioReferenceOptions Load(string directory)
        {
            var path = Path.Combine(directory, "case.json");
            if (!File.Exists(path))
                return new AudioReferenceOptions();

            var options = JsonSerializer.Deserialize<AudioReferenceOptions>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return options ?? new AudioReferenceOptions();
        }
    }

    private sealed record AudioAlignmentResult(
        int OffsetFrames,
        double Correlation,
        int DurationDeltaFrames,
        int SampleRate);

    private static class AudioAlignmentEstimator
    {
        private const int CoarseDownsampleFactor = 4;
        private const int RefineRadiusFrames = 24;

        public static AudioAlignmentResult Estimate(float[] reference, float[] candidate, int sampleRate,
            AudioReferenceOptions options)
        {
            if (reference.Length == 0 || candidate.Length == 0)
                throw new InvalidDataException("Cannot estimate alignment for empty PCM data.");

            var maxSearchFrames = Math.Max(1, sampleRate * options.MaxSearchMilliseconds / 1000);
            var windowFrames = Math.Max(256, sampleRate * options.WindowMilliseconds / 1000);
            windowFrames = Math.Min(windowFrames, Math.Min(reference.Length, candidate.Length) / 2);
            if (windowFrames <= 0)
                throw new InvalidDataException("PCM data is too short for alignment estimation.");

            var referenceStart = FindHighEnergyWindow(reference, windowFrames, maxSearchFrames);

            var coarseReference = Downsample(reference, CoarseDownsampleFactor);
            var coarseCandidate = Downsample(candidate, CoarseDownsampleFactor);
            var coarse = Search(
                coarseReference,
                coarseCandidate,
                referenceStart / CoarseDownsampleFactor,
                Math.Max(64, windowFrames / CoarseDownsampleFactor),
                Math.Max(1, maxSearchFrames / CoarseDownsampleFactor),
                int.MinValue,
                int.MaxValue);

            var coarseOffsetFrames = coarse.OffsetFrames * CoarseDownsampleFactor;
            var refined = Search(
                reference,
                candidate,
                referenceStart,
                windowFrames,
                maxSearchFrames,
                coarseOffsetFrames - RefineRadiusFrames,
                coarseOffsetFrames + RefineRadiusFrames);

            return new AudioAlignmentResult(refined.OffsetFrames, refined.Correlation, 0, sampleRate);
        }

        private static AudioAlignmentResult Search(float[] reference, float[] candidate, int referenceStart,
            int windowFrames, int maxSearchFrames, int requestedMinShift, int requestedMaxShift)
        {
            var minShift = Math.Max(-maxSearchFrames, requestedMinShift);
            var maxShift = Math.Min(maxSearchFrames, requestedMaxShift);
            minShift = Math.Max(minShift, -referenceStart);
            maxShift = Math.Min(maxShift, candidate.Length - referenceStart - windowFrames);

            if (minShift > maxShift)
                throw new InvalidDataException("Not enough overlapping PCM data to estimate alignment.");

            var bestShift = 0;
            var bestCorrelation = double.NegativeInfinity;

            for (var shift = minShift; shift <= maxShift; shift++)
            {
                var correlation = Correlation(reference, candidate, referenceStart, referenceStart + shift,
                    windowFrames);
                if (correlation > bestCorrelation)
                {
                    bestCorrelation = correlation;
                    bestShift = shift;
                }
            }

            return new AudioAlignmentResult(bestShift, bestCorrelation, 0, 0);
        }

        private static int FindHighEnergyWindow(float[] data, int windowFrames, int marginFrames)
        {
            var maxStart = Math.Max(0, data.Length - windowFrames - marginFrames);
            var start = Math.Min(Math.Max(0, marginFrames), maxStart);
            var step = Math.Max(1, windowFrames / 8);

            var bestStart = start;
            var bestEnergy = double.NegativeInfinity;
            for (var i = start; i <= maxStart; i += step)
            {
                var energy = 0.0;
                for (var j = 0; j < windowFrames; j++)
                {
                    var sample = data[i + j];
                    energy += sample * sample;
                }

                if (energy > bestEnergy)
                {
                    bestEnergy = energy;
                    bestStart = i;
                }
            }

            return bestStart;
        }

        private static double Correlation(float[] left, float[] right, int leftStart, int rightStart,
            int windowFrames)
        {
            var sumLeft = 0.0;
            var sumRight = 0.0;
            var sumLeftSquared = 0.0;
            var sumRightSquared = 0.0;
            var sumProduct = 0.0;

            for (var i = 0; i < windowFrames; i++)
            {
                var l = left[leftStart + i];
                var r = right[rightStart + i];

                sumLeft += l;
                sumRight += r;
                sumLeftSquared += l * l;
                sumRightSquared += r * r;
                sumProduct += l * r;
            }

            var covariance = sumProduct - sumLeft * sumRight / windowFrames;
            var leftVariance = sumLeftSquared - sumLeft * sumLeft / windowFrames;
            var rightVariance = sumRightSquared - sumRight * sumRight / windowFrames;
            var denominator = Math.Sqrt(leftVariance * rightVariance);

            return denominator <= double.Epsilon ? 0 : covariance / denominator;
        }

        private static float[] Downsample(float[] data, int factor)
        {
            var length = data.Length / factor;
            var result = new float[length];
            for (var i = 0; i < length; i++)
            {
                var sum = 0.0;
                for (var j = 0; j < factor; j++)
                    sum += data[i * factor + j];

                result[i] = (float)(sum / factor);
            }

            return result;
        }
    }

    private sealed record Pcm16Audio(byte[] Data, int SampleRate, int Channels)
    {
        public int FrameCount => Data.Length / (sizeof(short) * Channels);

        public static Pcm16Audio ReadWave(string path)
        {
            using var reader = new WaveFileReader(path);
            if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm ||
                reader.WaveFormat.BitsPerSample != 16)
            {
                throw new InvalidDataException($"Reference WAV must be PCM16: {path}");
            }

            var data = ReadAllBytes(reader);
            return new Pcm16Audio(data, reader.WaveFormat.SampleRate, reader.WaveFormat.Channels);
        }

        public static Pcm16Audio FromBytes(byte[] data, int sampleRate, int channels)
            => new(data, sampleRate, channels);

        public float[] ToMono()
        {
            var result = new float[FrameCount];
            var bytesPerFrame = Channels * sizeof(short);

            for (var frame = 0; frame < result.Length; frame++)
            {
                var frameOffset = frame * bytesPerFrame;
                var sum = 0.0;
                for (var channel = 0; channel < Channels; channel++)
                {
                    var sampleOffset = frameOffset + channel * sizeof(short);
                    sum += BinaryPrimitives.ReadInt16LittleEndian(Data.AsSpan(sampleOffset, sizeof(short)));
                }

                result[frame] = (float)(sum / Channels / short.MaxValue);
            }

            return result;
        }

        private static byte[] ReadAllBytes(WaveStream stream)
        {
            using var output = new MemoryStream((int)Math.Min(stream.Length, int.MaxValue));
            var buffer = new byte[64 * 1024];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, read);

            return output.ToArray();
        }
    }
}
