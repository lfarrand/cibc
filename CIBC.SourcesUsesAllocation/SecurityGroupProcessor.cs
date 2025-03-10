using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class SecurityGroupProcessor : ISecurityGroupProcessor
{
    private readonly ILogger<SecurityGroupProcessor> _logger;
    private readonly IRuleMatcher _ruleMatcher;
    

    public SecurityGroupProcessor(ILogger<SecurityGroupProcessor> logger, IRuleMatcher ruleMatcher)
    {
        _logger = logger;
        _ruleMatcher = ruleMatcher;
    }
    
    public async IAsyncEnumerable<AllocationResult> ProcessSecurityGroupAsync(string securityId, List<Trade> trades, List<AllocationRule> rules)
    {
        var sourceTrades = trades.Where(t => t.Category == "SOURCE").ToArray(); // Array for better cache locality
        var useTrades = trades.Where(t => t.Category == "USE").ToArray();
        _logger.LogDebug("Processing security {SecurityId} with {SourceCount} SOURCE trades and {UseCount} USE trades", securityId,
            sourceTrades.Length, useTrades.Length);
        
        // TODO: Iterate over rules, querying trades that match filter criteria
        // TODO: For each rule, iterate over trades backwards, removing on the fly when match is found
        // TODO: Group by: security, 

        HashSet<string> usedTrades = new HashSet<string>();
        foreach (var rule in rules)
        {
            for (int i = 0; i < sourceTrades.Length; i++)
            {
                var source = sourceTrades[i];
                if (usedTrades.Contains(source.TradeId))
                {
                    continue;
                }

                for (int j = 0; j < useTrades.Length; j++)
                {
                    var use = useTrades[j];
                    if (usedTrades.Contains(use.TradeId))
                    {
                        continue;
                    }

                    if (_ruleMatcher.MatchesRule(source, use, rule))
                    {
                        _logger.LogDebug("Matched {SourceId} with {UseId} using rule {RuleId}", source.TradeId, use.TradeId, rule.RuleId);
                        yield return new AllocationResult(source.TradeId, use.TradeId, rule.RuleId);
                        break;
                    }
                }
            }
        }

        await ProcessLeftoversAsync(sources.Where(s => !usedTrades.Contains(s.TradeId)), resultChannel,
            usedTradesChannel, usedTrades, "BOX", s => s.TradeId);
        await ProcessLeftoversAsync(uses.Where(u => !usedTrades.Contains(u.TradeId)), resultChannel,
            usedTradesChannel, usedTrades, "UNKNOWN", u => u.TradeId, true);

        _logger.LogInformation("Processed security {SecurityId}", securityId);
    }

    private async Task ProcessLeftoversAsync<T>(IEnumerable<T> items, HashSet<string> usedTrades, string tradeIdPrefix,
        Func<T, string> tradeIdSelector, bool isUse = false) where T : Trade
    {
        var tasks = new List<Task>(1000); // Pre-allocate
        foreach (var item in items)
        {
            var tradeId = tradeIdSelector(item);
            if (usedTrades.Contains(tradeId))
            {
                continue;
            }
            
            var result = isUse
                ? new AllocationResult("UNKNOWN", tradeId, null)
                : new AllocationResult(tradeId, "BOX", null);
                
            tasks.Add(Task.WhenAll(
                _allocationResultChannelWriter.WriteAsync(resultChannel, result, "result"),
                _usedTradesChannelWriter.WriteAsync(usedTradesChannel, tradeId, "used trade")
            ));
                
            _logger.LogDebug("Added leftover {TradeType} {TradeId} as {Prefix}", isUse ? "use" : "source",
                tradeId, tradeIdPrefix);
                
            if (tasks.Count < 1000)
            {
                continue;
            }
                
            await Task.WhenAll(tasks);
            tasks.Clear();
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}