using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CIBC.SourcesUsesAllocation.Tests;

public static class TestDependencyInjectionConfig
{
    public static IServiceProvider ConfigureTestServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(loggingBuilder =>
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestCorrelator();

            var logger = loggerConfig.CreateLogger();
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger, true);
        });
        services.AddSingleton<ITradeGrouper, TradeGrouper>();
        services.AddSingleton<IRuleMatcher, RuleMatcher>();
        services.AddSingleton<IChannelWriter<AllocationResult>, ChannelWriter<AllocationResult>>();
        services.AddSingleton<IChannelWriter<string>, ChannelWriter<string>>();
        services.AddSingleton<ISecurityGroupProcessor, SecurityGroupProcessor>();
        services.AddSingleton<IAllocationRulesProvider, AllocationRulesProvider>();
        services.AddTransient<AllocationProcessor>();
        services.AddTransient<ITradeProvider, DummyTradeProvider>();
        services.AddHostedService<AllocationsProcessorWorker>();

        return services.BuildServiceProvider();
    }
}