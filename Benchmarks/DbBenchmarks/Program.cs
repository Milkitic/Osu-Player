// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using OsuPlayer.Data;

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

    [Benchmark]
    public async Task<object?> Auto()
    {
        await using var appDbContext = new ApplicationDbContext();
        return await appDbContext.SearchPlayItemsAsync("yf_bmp",
            BeatmapOrderOptions.Artist, 0, 300);
    }
}