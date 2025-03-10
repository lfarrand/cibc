using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public interface ITradeProvider
{
    public IList<Trade> GetTrades();
}