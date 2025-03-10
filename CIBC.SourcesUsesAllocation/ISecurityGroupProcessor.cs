using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CIBC.SourcesUsesAllocation;

public interface ISecurityGroupProcessor
{
    Task ProcessSecurityGroupAsync(string securityId, List<Trade> trades, List<AllocationRule> rules,
        Channel<AllocationResult> resultChannel, Channel<string> usedTradesChannel, HashSet<string> usedTrades);
}