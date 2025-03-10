using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class AllocationsProcessorWorker : BackgroundService
{
    private readonly ILogger<AllocationsProcessorWorker> _logger;
    private readonly ITradeProvider _tradeProvider;
    private readonly IAllocationProcessor _allocationProcessor;

    public AllocationsProcessorWorker(ILogger<AllocationsProcessorWorker> logger, ITradeProvider tradeProvider, IAllocationProcessor allocationProcessor)
    {
        _logger = logger;
        _tradeProvider = tradeProvider;
        _allocationProcessor = allocationProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            
            var trades = _tradeProvider.GetTrades();

            if (trades.Any())
            {
                await _allocationProcessor.ProcessAllocationsAsync(trades);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}