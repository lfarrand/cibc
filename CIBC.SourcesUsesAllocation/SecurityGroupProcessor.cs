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
    private readonly IChannelWriter<AllocationResult> _allocationResultChannelWriter;
    private readonly IChannelWriter<string> _usedTradesChannelWriter;
    

    public SecurityGroupProcessor(ILogger<SecurityGroupProcessor> logger, IRuleMatcher ruleMatcher, IChannelWriter<AllocationResult> allocationResultChannelWriter, IChannelWriter<string> usedTradesChannelWriter)
    {
        _logger = logger;
        _ruleMatcher = ruleMatcher;
        _allocationResultChannelWriter = allocationResultChannelWriter;
        _usedTradesChannelWriter = usedTradesChannelWriter;
    }

    public async Task ProcessSecurityGroupAsync(string securityId, List<Trade> trades, List<AllocationRule> rules,
        Channel<AllocationResult> resultChannel, Channel<string> usedTradesChannel, HashSet<string> usedTrades)
    {
        var sources = trades.Where(t => t.Category == "SOURCE").ToArray(); // Array for better cache locality
        var uses = trades.Where(t => t.Category == "USE").ToArray();
        _logger.LogDebug("Processing security {SecurityId} with {SourceCount} sources and {UseCount} uses", securityId,
            sources.Length, uses.Length);

        var tasks = new List<Task>(rules.Count * sources.Length / 100); // Pre-allocate task list
        foreach (var rule in rules)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (usedTrades.Contains(source.TradeId)) continue;

                for (int j = 0; j < uses.Length; j++)
                {
                    var use = uses[j];
                    if (usedTrades.Contains(use.TradeId)) continue;

                    if (_ruleMatcher.MatchesRule(source, use, rule))
                    {
                        var result = new AllocationResult(source.TradeId, use.TradeId, rule.RuleId);
                        tasks.Add(Task.WhenAll(
                            _allocationResultChannelWriter.WriteAsync(resultChannel , result, "result"),
                            _usedTradesChannelWriter.WriteAsync(usedTradesChannel, source.TradeId, "used trade"),
                            _usedTradesChannelWriter.WriteAsync(usedTradesChannel, use.TradeId, "used trade")
                        ));
                        _logger.LogDebug("Matched {SourceId} with {UseId} using rule {RuleId}", source.TradeId, use.TradeId, rule.RuleId);
                        break;
                    }
                }
            }

            if (tasks.Count > 100) // Batch tasks to reduce overhead
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks);

        await ProcessLeftoversAsync(sources.Where(s => !usedTrades.Contains(s.TradeId)), resultChannel,
            usedTradesChannel, usedTrades, "BOX", s => s.TradeId);
        await ProcessLeftoversAsync(uses.Where(u => !usedTrades.Contains(u.TradeId)), resultChannel,
            usedTradesChannel, usedTrades, "UNKNOWN", u => u.TradeId, true);

        _logger.LogInformation("Processed security {SecurityId}", securityId);
    }

    private async Task ProcessLeftoversAsync<T>(IEnumerable<T> items, Channel<AllocationResult> resultChannel,
        Channel<string> usedTradesChannel, HashSet<string> usedTrades, string tradeIdPrefix,
        Func<T, string> tradeIdSelector, bool isUse = false) where T : Trade
    {
        var tasks = new List<Task>(1000); // Pre-allocate
        foreach (var item in items)
        {
            var tradeId = tradeIdSelector(item);
            if (!usedTrades.Contains(tradeId))
            {
                var result = isUse
                    ? new AllocationResult("UNKNOWN", tradeId, null)
                    : new AllocationResult(tradeId, "BOX", null);
                tasks.Add(Task.WhenAll(
                    _allocationResultChannelWriter.WriteAsync(resultChannel, result, "result"),
                    _usedTradesChannelWriter.WriteAsync(usedTradesChannel, tradeId, "used trade")
                ));
                _logger.LogDebug("Added leftover {TradeType} {TradeId} as {Prefix}", isUse ? "use" : "source",
                    tradeId, tradeIdPrefix);
                if (tasks.Count >= 1000)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks);
    }
}