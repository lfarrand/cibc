using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public interface IAllocationRulesProvider
{
    List<AllocationRule> Rules { get; }
}