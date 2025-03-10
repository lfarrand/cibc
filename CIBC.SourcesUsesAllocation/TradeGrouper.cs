using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class TradeGrouper : ITradeGrouper
{
    private readonly ILogger<TradeGrouper> _logger;

    public TradeGrouper(ILogger<TradeGrouper> logger) => _logger = logger;

    public Dictionary<string, List<Trade>> GroupTrades(IEnumerable<Trade> trades)
    {
        _logger.LogDebug("Grouping trades by SecurityId");
        var result = new Dictionary<string, List<Trade>>();
        foreach (var trade in trades)
        {
            if (!result.TryGetValue(trade.SecurityId, out var tradeList))
            {
                tradeList = new List<Trade>(1000); // Pre-allocate capacity
                result[trade.SecurityId] = tradeList;
            }

            tradeList.Add(trade);
        }

        foreach (var kvp in result)
        {
            kvp.Value.Sort((a, b) => a.TradeId.CompareTo(b.TradeId)); // Sort in-place
        }

        _logger.LogInformation("Trades grouped into {GroupCount} security groups", result.Count);
        return result;
    }
}