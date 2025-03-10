using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CIBC.SourcesUsesAllocation;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, loggingBuilder) =>
            {
                var loggerConfig = new LoggerConfiguration()
                    .ReadFrom.Configuration(ctx.Configuration)
                    .Enrich.FromLogContext();

                var logger = loggerConfig.CreateLogger();
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(logger, true);
            })
            .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<ITradeGrouper, TradeGrouper>();
                    services.AddSingleton<IRuleMatcher, RuleMatcher>();
                    services.AddSingleton<IChannelWriter<AllocationResult>, ChannelWriter<AllocationResult>>();
                    services.AddSingleton<IChannelWriter<string>, ChannelWriter<string>>();
                    services.AddSingleton<ISecurityGroupProcessor, SecurityGroupProcessor>();
                    services.AddSingleton<IAllocationRulesProvider, AllocationRulesProvider>();
                    services.AddTransient<IAllocationProcessor, AllocationProcessor>();
                    services.AddTransient<ITradeProvider, DummyTradeProvider>();
                    services.AddHostedService<AllocationsProcessorWorker>();
                }
            ).ConfigureAppConfiguration((ctx, config) =>
            {
                var env = ctx.HostingEnvironment;
                config.AddCommandLine(args);
                config.AddEnvironmentVariables();
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            });
        
        var host = builder.Build();
        host.Run();
    }
}