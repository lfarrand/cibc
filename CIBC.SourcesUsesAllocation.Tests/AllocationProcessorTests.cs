using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog.Sinks.TestCorrelator;
using Xunit;

namespace CIBC.SourcesUsesAllocation.Tests;

public class AllocationProcessorTests : IClassFixture<TradeAllocationFixture>
{
    private readonly TradeAllocationFixture _fixture;
    private readonly ILogger _loggerSubstitute;
    private readonly ITradeGrouper _tradeGrouperSubstitute;
    private readonly IRuleMatcher _ruleMatcherSubstitute;
    private readonly IChannelWriter<object> _channelWriterSubstitute;
    private readonly ISecurityGroupProcessor _securityGroupProcessorSubstitute;

    public AllocationProcessorTests(TradeAllocationFixture fixture)
    {
        _fixture = fixture;
        _loggerSubstitute = Substitute.For<ILogger>();
        _tradeGrouperSubstitute = Substitute.For<ITradeGrouper>();
        _ruleMatcherSubstitute = Substitute.For<IRuleMatcher>();
        _channelWriterSubstitute = Substitute.For<IChannelWriter<object>>();
        _securityGroupProcessorSubstitute = Substitute.For<ISecurityGroupProcessor>();
    }
    
    /*

    [Theory, AutoFixture.Xunit2.AutoData]
    public async Task AllocationProcessor_MatchesSinglePair(Trade source, Trade use, AllocationRule rule)
    {
        using (TestCorrelator.CreateContext())
        {
            source = source with { Category = "SOURCE", SubCategory = "PLEDGE_IN", Desk = "DESK_1", Account = "ACCOUNT_1" };
            use = use with { Category = "USE", SubCategory = "PLEDGE_OUT", Desk = "DESK_1", Account = "ACCOUNT_1" };
            rule = rule with { SourceCriteria = new() { {"Category", "SOURCE"}, {"SubCategory", "PLEDGE_IN"} }, UseCriteria = new() { {"Category", "USE"}, {"SubCategory", "PLEDGE_OUT"} }, AdditionalCriteria = new() { {"MATCH(Desk)", true}, {"MATCH(Account)", true} } };

            var trades = new List<Trade> { source, use };
            var rules = new List<AllocationRule> { rule };
            var processor = new AllocationProcessor(trades, rules, _fixture.ServiceProvider.GetRequiredService<ILogger>(), _fixture.ServiceProvider.GetRequiredService<ITradeGrouper>(), _fixture.ServiceProvider.GetRequiredService<ISecurityGroupProcessor>());

            var results = await processor.ProcessAllocationsAsync();

            Assert.Single(results);
            Assert.Equal(source.TradeId, results[0].SourceTradeId);
            Assert.Equal(use.TradeId, results[0].UseTradeId);
            Assert.Equal(rule.RuleId, results[0].RuleId);
        }
    }

    [Fact]
    public async Task AllocationProcessor_NoMatchesLeavesLeftovers()
    {
        using (TestCorrelator.CreateContext())
        {
            var trades = _fixture.GenerateTrades(10);
            var rules = _fixture.GenerateRules(5);
            var processor = new AllocationProcessor(trades, rules, _fixture.ServiceProvider.GetRequiredService<ILogger>(), _fixture.ServiceProvider.GetRequiredService<ITradeGrouper>(), _fixture.ServiceProvider.GetRequiredService<ISecurityGroupProcessor>());

            var results = await processor.ProcessAllocationsAsync();

            Assert.All(results, r => Assert.True(r.UseTradeId == "BOX" || r.SourceTradeId == "UNKNOWN"));
            Assert.Equal(trades.Count, results.Count);
        }
    }

    [Fact]
    public async Task AllocationProcessor_EmptyTradesReturnsEmptyResults()
    {
        using (TestCorrelator.CreateContext())
        {
            var processor = new AllocationProcessor(new List<Trade>(), _fixture.GenerateRules(5), _fixture.ServiceProvider.GetRequiredService<ILogger>(), _fixture.ServiceProvider.GetRequiredService<ITradeGrouper>(), _fixture.ServiceProvider.GetRequiredService<ISecurityGroupProcessor>());

            var results = await processor.ProcessAllocationsAsync();

            Assert.Empty(results);
            Assert.Contains(TestCorrelator.GetLogEvents(), e => e.MessageTemplate.Text.Contains("Allocation process completed successfully"));
        }
    }

    [Fact]
    public async Task AllocationProcessor_EmptyRulesLeavesAllTradesAsLeftovers()
    {
        using (TestCorrelator.CreateContext())
        {
            var trades = _fixture.GenerateTrades(10);
            var processor = new AllocationProcessor(trades, new List<AllocationRule>(), _fixture.ServiceProvider.GetRequiredService<ILogger>(), _fixture.ServiceProvider.GetRequiredService<ITradeGrouper>(), _fixture.ServiceProvider.GetRequiredService<ISecurityGroupProcessor>());

            var results = await processor.ProcessAllocationsAsync();

            Assert.Equal(trades.Count, results.Count);
            Assert.All(results, r => Assert.Null(r.RuleId));
            Assert.All(results, r => Assert.True(r.UseTradeId == "BOX" || r.SourceTradeId == "UNKNOWN"));
        }
    }

    [Fact]
    public async Task AllocationProcessor_HandlesSecurityGroupError()
    {
        using (TestCorrelator.CreateContext())
        {
            var trades = _fixture.GenerateTrades(10);
            _tradeGrouperSubstitute.GroupTrades(Arg.Any<IEnumerable<Trade>>())
                .Returns(new Dictionary<string, List<Trade>> { { "ISIN_1", trades.Take(2).ToList() } });
            _securityGroupProcessorSubstitute
                .When(x => x.ProcessSecurityGroupAsync(Arg.Any<string>(), Arg.Any<List<Trade>>(), Arg.Any<List<AllocationRule>>(), Arg.Any<Channel<AllocationResult>>(), Arg.Any<Channel<string>>(), Arg.Any<HashSet<string>>()))
                .Do(x => throw new Exception("Test exception"));

            var processor = new AllocationProcessor(
                trades,
                _fixture.GenerateRules(5),
                _fixture.ServiceProvider.GetRequiredService<ILogger>(),
                _tradeGrouperSubstitute,
                _securityGroupProcessorSubstitute
            );

            var exception = await Assert.ThrowsAsync<AggregateException>(() => processor.ProcessAllocationsAsync());
            Assert.Single((IEnumerable)exception.InnerExceptions);
            Assert.Contains(TestCorrelator.GetLogEvents(), e => e.MessageTemplate.Text.Contains("Error processing security group"));
        }
    }

    [Fact(Skip = "Large scale test, run manually due to high resource usage")]
    public async Task AllocationProcessor_HandlesLargeScaleScenario()
    {
        using (TestCorrelator.CreateContext())
        {
            const int tradeCount = 5_000_000;
            const int ruleCount = 300;
            var largeTrades = _fixture.GenerateTrades(tradeCount);
            var largeRules = _fixture.GenerateRules(ruleCount);

            var processor = new AllocationProcessor(
                largeTrades,
                largeRules,
                _fixture.ServiceProvider.GetRequiredService<ILogger>(),
                _fixture.ServiceProvider.GetRequiredService<ITradeGrouper>(),
                _fixture.ServiceProvider.GetRequiredService<ISecurityGroupProcessor>()
            );

            var results = await processor.ProcessAllocationsAsync();

            var sourceCount = largeTrades.Count(t => t.Category == "SOURCE");
            var useCount = largeTrades.Count(t => t.Category == "USE");
            var matchedResults = results.Count(r => r.RuleId != null);
            var leftoverSources = results.Count(r => r.UseTradeId == "BOX");
            var leftoverUses = results.Count(r => r.SourceTradeId == "UNKNOWN");

            Assert.True(results.Count > 0, "Expected some matches in a large dataset");
            Assert.Equal(results.Count, matchedResults + leftoverSources + leftoverUses);
            Assert.True(matchedResults > 0, "Expected some rule-based matches");
            Assert.True(leftoverSources <= sourceCount, "Leftover sources should not exceed total sources");
            Assert.True(leftoverUses <= useCount, "Leftover uses should not exceed total uses");
            Assert.All(results.Where(r => r.RuleId != null), r => Assert.Contains(largeRules, rule => rule.RuleId == r.RuleId));
            Assert.All(results, r => Assert.True(r.SourceTradeId != "UNKNOWN" || r.UseTradeId != "BOX", "Invalid result combination"));
            Assert.Contains(TestCorrelator.GetLogEvents(), e => e.MessageTemplate.Text.Contains("Allocation process completed successfully"));
        }
    }
    */
}