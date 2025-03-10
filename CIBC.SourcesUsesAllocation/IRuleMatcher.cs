namespace CIBC.SourcesUsesAllocation;

public interface IRuleMatcher
{
    bool MatchesRule(Trade source, Trade use, AllocationRule rule);
}