namespace CIBC.SourcesUsesAllocation.Rules;

public interface IAllocationRule
{
     string RuleId { get; set; }
     int Priority { get; set; }
     string SourceCriteria { get; set; }
     string UseCriteria { get; set; }
     string AdditionalCriteria { get; set; }
}

public class AllocationRule
{
    
}