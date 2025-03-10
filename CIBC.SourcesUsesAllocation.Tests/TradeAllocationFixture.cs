using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CIBC.SourcesUsesAllocation.Tests;

public class TradeAllocationFixture
{
    private readonly Fixture _fixture;
    public IAllocationProcessor AllocationProcessor { get; }
    public IAllocationRulesProvider AllocationRulesProvider { get; }
    public ITradeProvider TradeProvider { get; }

    public TradeAllocationFixture(int rulesCount, int tradesCount)
    {
        _fixture = new Fixture();
        _fixture.Customize<Trade>(c => c
            .With(t => t.TradeId, () => $"T{_fixture.Create<int>()}")
            .With(t => t.SecurityId, () => $"ISIN_{_fixture.Create<int>() % 5000}")
            .With(t => t.Category, () => _fixture.Create<Generator<string>>().First(g => g == "SOURCE" || g == "USE"))
            .With(t => t.SubCategory,
                () => _fixture.Create<Generator<string>>().First(g =>
                    g == "PLEDGE_IN" || g == "BORROW" || g == "PLEDGE_OUT" || g == "LOAN"))
            .With(t => t.Desk, () => _fixture.Create<bool>() ? $"DESK_{_fixture.Create<int>() % 200}" : null)
            .With(t => t.Account, () => _fixture.Create<bool>() ? $"ACCOUNT_{_fixture.Create<int>() % 200}" : null));

        _fixture.Customize<AllocationRule>(c => c
            .With(r => r.RuleId, () => $"R{_fixture.Create<int>()}")
            .With(r => r.Priority, () => _fixture.Create<int>() % 1000 + 1)
            .With(r => r.SourceCriteria,
                () => new Dictionary<string, string>
                {
                    { "Category", "SOURCE" },
                    {
                        "SubCategory",
                        _fixture.Create<Generator<string>>().First(g => g == "PLEDGE_IN" || g == "BORROW")
                    }
                })
            .With(r => r.UseCriteria,
                () => new Dictionary<string, string>
                {
                    { "Category", "USE" },
                    { "SubCategory", _fixture.Create<Generator<string>>().First(g => g == "PLEDGE_OUT" || g == "LOAN") }
                })
            .With(r => r.AdditionalCriteria,
                () => _fixture.Create<bool>()
                    ? new Dictionary<string, bool>
                        { { "MATCH(Desk)", _fixture.Create<bool>() }, { "MATCH(Account)", _fixture.Create<bool>() } }
                    : new Dictionary<string, bool>()));

        _fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = false });

        _fixture.Inject((ITradeGrouper)new TradeGrouper(Substitute.For<ILogger<TradeGrouper>>()));
        _fixture.Inject((IRuleMatcher)new RuleMatcher());
        _fixture.Inject((IChannelWriter<AllocationResult>)new ChannelWriter<AllocationResult>(Substitute.For<ILogger<ChannelWriter<AllocationResult>>>()));
        _fixture.Inject((IChannelWriter<string>)new ChannelWriter<string>(Substitute.For<ILogger<ChannelWriter<string>>>()));
        
        AllocationRulesProvider = Substitute.For<IAllocationRulesProvider>();
        AllocationRulesProvider.Rules.Returns(GenerateRules(rulesCount));
        _fixture.Inject(AllocationRulesProvider);
        
        TradeProvider = Substitute.For<ITradeProvider>();
        TradeProvider.GetTrades().Returns(GenerateTrades(tradesCount));
        _fixture.Inject(TradeProvider);
        
        ISecurityGroupProcessor securityGroupProcessor = _fixture.Create<SecurityGroupProcessor>();
        _fixture.Inject(securityGroupProcessor);
        
        AllocationProcessor = _fixture.Create<AllocationProcessor>();
    }

    public List<Trade> GenerateTrades(int count) => _fixture.CreateMany<Trade>(count).ToList();
    
    public List<AllocationRule> GenerateRules(int count) => _fixture.CreateMany<AllocationRule>(count).ToList();
}