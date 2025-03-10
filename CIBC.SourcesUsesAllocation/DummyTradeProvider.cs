using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public class DummyTradeProvider : ITradeProvider
{
    public IList<Trade> GetTrades()
    {
        return new List<Trade>
        {
            new("T1", "ISIN_1", "SOURCE", "PLEDGE_IN", "DESK_1", "ACCOUNT_1"),
            new("T2", "ISIN_1", "SOURCE", "BORROW", "DESK_2", "ACCOUNT_2"),
            new("T3", "ISIN_1", "USE", "PLEDGE_OUT", "", "ACCOUNT_1"),
            new("T4", "ISIN_1", "USE", "PLEDGE_OUT", "DESK_1", "ACCOUNT_3"),
            new("T5", "ISIN_2", "SOURCE", "BORROW", "", ""),
            new("T6", "ISIN_2", "USE", "LOAN", "", ""),
            new("T7", "ISIN_1", "SOURCE", "PLEDGE_IN", "", ""),
            new("T8", "ISIN_3", "USE", "LOAN", "", "")
        };
    }
}