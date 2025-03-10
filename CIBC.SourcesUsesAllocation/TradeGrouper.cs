using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class TradeGrouper(ILogger<TradeGrouper> logger) : ITradeGrouper
{
    public Dictionary<string, List<Trade>> GroupTrades(IEnumerable<Trade> trades)
    {
        logger.LogDebug("Grouping trades by SecurityId");
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
            kvp.Value.Sort((a, b) => string.Compare(a.TradeId, b.TradeId, StringComparison.Ordinal)); // Sort in-place
        }

        logger.LogInformation("Trades grouped into {GroupCount} security groups", result.Count);
        return result;
    }
}