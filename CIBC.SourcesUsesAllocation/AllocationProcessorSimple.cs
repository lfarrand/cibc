using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class AllocationProcessorSimple : IAllocationProcessor
{
    private readonly ILogger<AllocationProcessorSimple> _logger;
    private readonly ITradeGrouper _tradeGrouper;
    private readonly ISecurityGroupProcessor _securityGroupProcessor;
    private readonly IAllocationRulesProvider _rulesProvider;
    private const int ChannelCapacity = 10;

    public AllocationProcessorSimple(ILogger<AllocationProcessorSimple> logger, ITradeGrouper tradeGrouper, ISecurityGroupProcessor securityGroupProcessor, IAllocationRulesProvider rulesProvider )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tradeGrouper = tradeGrouper;
        _securityGroupProcessor = securityGroupProcessor;
        _rulesProvider = rulesProvider;
    }
    
    /*
       Sources & Uses Allocation process can be broken down into following steps:
       a. Group the raw data based on SecurityId.
       b. Within each group, order the trades by TradeId and apply the SNU Allocation Rules (in Priority
       descending order) to each trade. If a matching pair of Source and Use can be found, save it in
       the result table.
       c. If there are leftover trades that cannot be captured by any of the given rules,
       i. If leftover trades are SOURCE trades, put it in the result table with UseTradeId = BOX.
       Leave RuleId blank.
       ii. If leftover trades are USE trades, put it in the result table with SourceTradeId =
       UNKNOWN. Leave RuleId blank.
       
       Notes
       a. One trade should only be matched once.
       b. Only sources and uses of the same SecurityId should be linked together.
       c. While this is a test scenario, the solution should be developed with due consideration given to
       performance. In a real-world scenario this can scale to millions of raw data rows and hundreds
       of rules.
     */

    public async Task<List<AllocationResult>> ProcessAllocationsAsync(IList<Trade> trades)
    {
        var rules = _rulesProvider.Rules;
        
        _logger.LogInformation("Starting allocation process with {TradeCount} trades and {RuleCount} rules",
            trades.Count, rules.Count);

        var usedTrades = new HashSet<string>(trades.Count / 2); // Pre-allocate capacity
        var exceptions = new List<Exception>();

        var tradesBySecurity = _tradeGrouper.GroupTrades(trades);
        var processTasks = new List<Task>(tradesBySecurity.Count);
        
        foreach (var group in tradesBySecurity)
        {
            processTasks.Add(_securityGroupProcessor.ProcessSecurityGroupAsync(group.Key, group.Value, rules));
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
                _logger.LogInformation("SourceTradeId: {SourceTradeId}, UseTradeId: {UseTradeId}, RuleId: {RuleId}",
                    result.SourceTradeId, result.UseTradeId, result.RuleId);
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