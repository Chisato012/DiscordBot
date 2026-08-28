namespace StockBot.Models;

public sealed record StockQuote(
    string Symbol,
    string Exchange,
    decimal CurrentPrice,
    decimal ReferencePrice,
    decimal CeilingPrice,
    decimal FloorPrice,
    decimal Change,
    decimal PercentChange,
    decimal HighPrice,
    decimal LowPrice,
    long Volume,
    string? TradingDate,
    string? TradingTime);

public sealed record StockPriceLookupResult(
    IReadOnlyList<StockQuote> Quotes,
    IReadOnlyList<string> InvalidSymbols);
