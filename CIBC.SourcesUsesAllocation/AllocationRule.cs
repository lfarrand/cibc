using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public record AllocationRule(
    string RuleId,
    int Priority,
    Dictionary<string, string> SourceCriteria,
    Dictionary<string, string> UseCriteria,
    Dictionary<string, bool> AdditionalCriteria);