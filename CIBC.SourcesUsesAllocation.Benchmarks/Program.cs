using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace CIBC.SourcesUsesAllocation.Benchmarks;

class Program
{
    static void Main()
    {
        var summary = BenchmarkRunner.Run<AllocationProcessorBenchmarks>(
            ManualConfig
                .Create(DefaultConfig.Instance)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator));
        
        Console.WriteLine(summary);
    }
}