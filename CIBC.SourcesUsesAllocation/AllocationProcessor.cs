using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class AllocationProcessor : IAllocationProcessor
{
    private readonly ILogger<AllocationProcessor> _logger;
    private readonly ITradeGrouper _tradeGrouper;
    private readonly ISecurityGroupProcessor _securityGroupProcessor;
    private readonly IAllocationRulesProvider _rulesProvider;
    private const int ChannelCapacity = 100000;

    public AllocationProcessor(ILogger<AllocationProcessor> logger, ITradeGrouper tradeGrouper, ISecurityGroupProcessor securityGroupProcessor, IAllocationRulesProvider rulesProvider )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tradeGrouper = tradeGrouper;
        _securityGroupProcessor = securityGroupProcessor;
        _rulesProvider = rulesProvider;
    }

    public async Task<List<AllocationResult>> ProcessAllocationsAsync(IList<Trade> trades)
    {
        var rules = _rulesProvider.Rules;
        
        _logger.LogInformation("Starting allocation process with {TradeCount} trades and {RuleCount} rules",
            trades.Count, rules.Count);

        var resultChannel = Channel.CreateBounded<AllocationResult>(new BoundedChannelOptions(ChannelCapacity)
            { FullMode = BoundedChannelFullMode.Wait, SingleWriter = false, SingleReader = true });
        var usedTradesChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(ChannelCapacity)
            { FullMode = BoundedChannelFullMode.Wait, SingleWriter = false, SingleReader = true });

        var usedTrades = new HashSet<string>(trades.Count / 2); // Pre-allocate capacity
        var exceptions = new List<Exception>();

        var usedTradesTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var tradeId in usedTradesChannel.Reader.ReadAllAsync())
                {
                    usedTrades.Add(tradeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting used trades");
                exceptions.Add(new InvalidOperationException("Error collecting used trades", ex));
            }
        });

        var tradesBySecurity = _tradeGrouper.GroupTrades(trades);
        var processTasks = new List<Task>(tradesBySecurity.Count);
        foreach (var group in tradesBySecurity)
        {
            processTasks.Add(_securityGroupProcessor.ProcessSecurityGroupAsync(group.Key, group.Value, rules,
                resultChannel, usedTradesChannel, usedTrades));
        }

        try
        {
            await Task.WhenAll(processTasks);
            _logger.LogInformation("Parallel processing completed");

            resultChannel.Writer.Complete();
            usedTradesChannel.Writer.Complete();
            await usedTradesTask;

            if (exceptions.Any())
            {
                _logger.LogCritical("Processing completed with {ErrorCount} errors", exceptions.Count);
                throw new AggregateException("Errors occurred during processing", exceptions);
            }

            var results = new List<AllocationResult>(trades.Count / 2); // Pre-allocate capacity
            await foreach (var result in resultChannel.Reader.ReadAllAsync())
            {
                results.Add(result);
            }

            _logger.LogInformation("Collected {ResultCount} results", results.Count);
            
            foreach (var result in results)
            {
                _logger.LogInformation("RuleId: {RuleId}, SourceTradeId: {SourceTradeId}, UseTradeId: {UseTradeId}",result.RuleId,  result.SourceTradeId, result.UseTradeId );
            }

            _logger.LogInformation("Allocation process completed successfully");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during allocation processing");
            resultChannel.Writer.TryComplete(ex);
            usedTradesChannel.Writer.TryComplete(ex);
            throw;
        }
    }
}