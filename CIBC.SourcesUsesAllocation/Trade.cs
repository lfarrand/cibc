namespace CIBC.SourcesUsesAllocation;

public record Trade(
    string TradeId,
    string SecurityId,
    string Category,
    string? SubCategory,
    string? Desk = null,
    string? Account = null);