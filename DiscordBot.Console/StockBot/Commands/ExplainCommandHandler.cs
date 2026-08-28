using NetCord;
using NetCord.Rest;
using StockBot.Models;
using StockBot.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StockBot.Commands;

public sealed partial class ExplainCommandHandler
{
    private readonly StockPriceService _stockPriceService;

    public ExplainCommandHandler(StockPriceService stockPriceService)
    {
        _stockPriceService = stockPriceService;
    }

    public async Task HandleAsync(SlashCommandInteraction interaction, RestClient restClient)
    {
        await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        try
        {
            string? input = interaction.Data.Options
                .FirstOrDefault(option => option.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase))
                ?.Value;
            string symbol = input?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!SymbolPattern().IsMatch(symbol))
            {
                await UpdateResponseAsync(interaction, restClient, "Mã cổ phiếu không hợp lệ.");
                return;
            }

            StockPriceLookupResult result = await _stockPriceService.GetPricesAsync([symbol]);
            StockQuote? quote = result.Quotes.FirstOrDefault();

            string content = quote is null
                ? $"Mã **{symbol}** không hợp lệ hoặc chưa có dữ liệu."
                : FormatExplanation(quote);

            await UpdateResponseAsync(interaction, restClient, content);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Explain command error: {exception.Message}");
            await UpdateResponseAsync(interaction, restClient, "Không thể lấy dữ liệu ngay lúc này.");
        }
    }

    private static string FormatExplanation(StockQuote quote)
    {
        string sign = quote.Change > 0 ? "+" : string.Empty;
        string timestamp = string.Join(' ', new[] { quote.TradingDate, quote.TradingTime }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return $"**{quote.Symbol} ({quote.Exchange}) - Giải thích thuộc tính**\n" +
               $"- **Giá hiện tại** ({FormatNumber(quote.CurrentPrice)} VND): giá khớp gần nhất.\n" +
               $"- **Tham chiếu** ({FormatNumber(quote.ReferencePrice)} VND): mốc để tính mức thay đổi trong phiên.\n" +
               $"- **Trần / Sàn** ({FormatNumber(quote.CeilingPrice)} / {FormatNumber(quote.FloorPrice)} VND): mức giá cao nhất / thấp nhất được phép giao dịch trong phiên.\n" +
               $"- **Thay đổi** ({sign}{FormatNumber(quote.Change)} VND, {sign}{quote.PercentChange:0.##}%): chênh lệch so với giá tham chiếu.\n" +
               $"- **Cao nhất / Thấp nhất** ({FormatNumber(quote.HighPrice)} / {FormatNumber(quote.LowPrice)} VND): giá cao / thấp đã khớp trong phiên.\n" +
               $"- **Khối lượng** ({FormatNumber(quote.Volume)}): tổng số cổ phiếu đã khớp trong phiên." +
               (string.IsNullOrWhiteSpace(timestamp) ? string.Empty : $"\nDữ liệu cập nhật: {timestamp}.");
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
}
