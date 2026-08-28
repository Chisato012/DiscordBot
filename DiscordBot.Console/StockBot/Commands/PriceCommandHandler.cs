using NetCord;
using NetCord.Rest;
using StockBot.Models;
using StockBot.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StockBot.Commands;

public sealed partial class PriceCommandHandler
{
    private const int MaximumSymbols = 10;
    private readonly StockPriceService _stockPriceService;

    public PriceCommandHandler(StockPriceService stockPriceService)
    {
        _stockPriceService = stockPriceService;
    }

    public async Task HandleAsync(SlashCommandInteraction interaction, RestClient restClient)
    {
        await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        try
        {
            string? symbolsText = interaction.Data.Options
                .FirstOrDefault(option => option.Name.Equals("symbols", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            ParsedSymbols parsedSymbols = ParseSymbols(symbolsText);
            string content;

            if (parsedSymbols.ValidSymbols.Count == 0 && parsedSymbols.InvalidSymbols.Count == 0)
            {
                content = "Nhập ít nhất một mã, ví dụ: `/price FPT,CMC`.";
            }
            else if (parsedSymbols.ValidSymbols.Count == 0)
            {
                content = FormatResult(new StockPriceLookupResult(
                    Array.Empty<StockQuote>(),
                    parsedSymbols.InvalidSymbols));
            }
            else if (parsedSymbols.ValidSymbols.Count > MaximumSymbols)
            {
                content = $"Vì giới hạn nên mỗi lần chỉ tối đa {MaximumSymbols} mã.";
            }
            else
            {
                StockPriceLookupResult result = await _stockPriceService.GetPricesAsync(parsedSymbols.ValidSymbols);
                content = FormatResult(new StockPriceLookupResult(
                    result.Quotes,
                    result.InvalidSymbols.Concat(parsedSymbols.InvalidSymbols).ToList()));
            }

            await UpdateResponseAsync(interaction, restClient, content);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Price command error: {exception.Message}");
            await UpdateResponseAsync(interaction, restClient, "Không thể lấy giá ngay lúc này.");
        }
    }

    private static ParsedSymbols ParseSymbols(string? symbolsText)
    {
        if (string.IsNullOrWhiteSpace(symbolsText))
        {
            return new ParsedSymbols(Array.Empty<string>(), Array.Empty<string>());
        }

        string[] inputSymbols = symbolsText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(symbol => symbol.ToUpperInvariant())
            .ToArray();

        var validSymbols = inputSymbols
            .Where(symbol => SymbolPattern().IsMatch(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invalidSymbols = inputSymbols
            .Where(symbol => !SymbolPattern().IsMatch(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ParsedSymbols(validSymbols, invalidSymbols);
    }

    private static string FormatResult(StockPriceLookupResult result)
    {
        var lines = new List<string>();

        foreach (StockQuote quote in result.Quotes)
        {
            string sign = quote.Change > 0 ? "+" : string.Empty;
            lines.Add(
                $"**{quote.Symbol}** ({quote.Exchange}) - Giá hiện tại: **{FormatNumber(quote.CurrentPrice)} VND**\n" +
                $"Tham chiếu: {FormatNumber(quote.ReferencePrice)} | Trần: {FormatNumber(quote.CeilingPrice)} | Sàn: {FormatNumber(quote.FloorPrice)}\n" +
                $"Thay đổi: {sign}{FormatNumber(quote.Change)} ({sign}{quote.PercentChange:0.##}%) | " +
                $"Cao/Thấp: {FormatNumber(quote.HighPrice)}/{FormatNumber(quote.LowPrice)}\n" +
                $"Khối lượng: {FormatNumber(quote.Volume)}");
        }

        if (result.InvalidSymbols.Count > 0)
        {
            lines.Add($"Mã không hợp lệ hoặc chưa có dữ liệu: **{string.Join(", ", result.InvalidSymbols)}**");
        }

        return lines.Count > 0 ? string.Join("\n\n", lines): "Không tìm thấy dữ liệu cho các mã đã nhập.";
    }

    private static Task UpdateResponseAsync(
        SlashCommandInteraction interaction,
        RestClient restClient,
        string content)
    {
        return restClient.ModifyInteractionResponseAsync(
            interaction.ApplicationId,
            interaction.Token,
            options => options.Content = content);
    }

    private static string FormatNumber(decimal number) => number.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatNumber(long number) => number.ToString("N0", CultureInfo.InvariantCulture);

    [GeneratedRegex("^[A-Z0-9]{1,10}$")]
    private static partial Regex SymbolPattern();

    private sealed record ParsedSymbols(IReadOnlyList<string> ValidSymbols, IReadOnlyList<string> InvalidSymbols);
}
