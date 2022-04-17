// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using OsuPlayer.Data;
using Perfolizer.Horology;

BenchmarkRunner.Run<SearchComparison>();

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class SearchComparison
{

    [GlobalSetup]
    public void Setup()
    {
    }


    [Benchmark(Baseline = true)]
    public async Task<object?> New()
    {
        await using var appDbContext = new ApplicationDbContext();
        return await appDbContext.SearchBeatmapAsync("Camellia", BeatmapOrderOptions.ArtistUnicode, 0, 3000);
    }

    [Benchmark]
    public async Task<object?> Old()
    {
        await using var appDbContext = new ApplicationDbContext();
        return await appDbContext.SearchBeatmapAsyncOld("Camellia", BeatmapOrderOptions.ArtistUnicode, 0, 3000);
    }
}