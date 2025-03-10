using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using CIBC.SourcesUsesAllocation.Tests;

namespace CIBC.SourcesUsesAllocation.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class AllocationProcessorBenchmarks
{
    [Benchmark]
    public async Task ProcessAllocationsAsync_SmallDataset()
    {
        var fixture = new TradeAllocationFixture(100, 10);
        
        await fixture.AllocationProcessor.ProcessAllocationsAsync(fixture.TradeProvider.GetTrades());
    }

    [Benchmark]
    public async Task ProcessAllocationsAsync_LargeDataset()
    {
        var fixture = new TradeAllocationFixture(300, 5_000_000);
        
        await fixture.AllocationProcessor.ProcessAllocationsAsync(fixture.TradeProvider.GetTrades());
    }
}