using System.Collections.Generic;

namespace CIBC.SourcesUsesAllocation;

public class AllocationRulesProvider : IAllocationRulesProvider
{
    public List<AllocationRule> Rules =>
    [
        ..new[]
        {
            new AllocationRule("R1", 100,
                new() { { "Category", "SOURCE" }, { "SubCategory", "PLEDGE_IN" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new() { { "MATCH(Desk)", true }, { "MATCH(Account)", true } }
            ),
            new AllocationRule("R2", 90,
                new() { { "Category", "SOURCE" }, { "SubCategory", "PLEDGE_IN" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new() { { "MATCH(Desk)", true } }
            ),
            new AllocationRule("R3", 80,
                new() { { "Category", "SOURCE" }, { "SubCategory", "PLEDGE_IN" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new()
            ),
            new AllocationRule("R4", 70,
                new() { { "Category", "SOURCE" }, { "SubCategory", "BORROW" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new() { { "MATCH(Desk)", true }, { "MATCH(Account)", true } }
            ),
            new AllocationRule("R5", 60,
                new() { { "Category", "SOURCE" }, { "SubCategory", "BORROW" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new() { { "MATCH(Desk)", true } }
            ),
            new AllocationRule("R6", 50,
                new() { { "Category", "SOURCE" }, { "SubCategory", "BORROW" } },
                new() { { "Category", "USE" }, { "SubCategory", "PLEDGE_OUT" } },
                new()
            ),
            new AllocationRule("R7", 40,
                new() { { "Category", "SOURCE" }, { "SubCategory", "BORROW" } },
                new() { { "Category", "USE" }, { "SubCategory", "LOAN" } },
                new()
            )
        }
    ];
}