using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public interface ITradeGrouper
{
    Dictionary<string, List<Trade>> GroupTrades(IEnumerable<Trade> trades);
}