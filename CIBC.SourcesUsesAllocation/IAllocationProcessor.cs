using System.Collections.Generic;
using System.Threading.Tasks;

namespace CIBC.SourcesUsesAllocation;

public interface IAllocationProcessor
{
    Task<List<AllocationResult>> ProcessAllocationsAsync(IList<Trade> trades);
}