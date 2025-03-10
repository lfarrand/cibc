namespace CIBC.SourcesUsesAllocation;

public class RuleMatcher : IRuleMatcher
{
    public bool MatchesRule(Trade source, Trade use, AllocationRule rule)
    {
        foreach (var criterion in rule.SourceCriteria)
        {
            if (criterion.Key == "Category" && source.Category != criterion.Value) return false;
            if (criterion.Key == "SubCategory" && source.SubCategory != criterion.Value) return false;
        }

        foreach (var criterion in rule.UseCriteria)
        {
            if (criterion.Key == "Category" && use.Category != criterion.Value) return false;
            if (criterion.Key == "SubCategory" && use.SubCategory != criterion.Value) return false;
        }

        foreach (var criterion in rule.AdditionalCriteria)
        {
            if (criterion.Key == "MATCH(Desk)" && criterion.Value && source.Desk != use.Desk) return false;
            if (criterion.Key == "MATCH(Account)" && criterion.Value && source.Account != use.Account) return false;
        }

        return true;
    }
}