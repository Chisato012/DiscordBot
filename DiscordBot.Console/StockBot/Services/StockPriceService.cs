using StockBot.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StockBot.Services;

public sealed class StockPriceService
{
    private const string PriceBoardUrl = "https://kbbuddywts.kbsec.com.vn/iis-server/investment/stock/iss";
    private readonly HttpClient _httpClient;

    public StockPriceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockPriceLookupResult> GetPricesAsync(
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken = default)
    {
        string requestBody = $$"""{"code":"{{string.Join(',', symbols)}}"}""";
        var requestContent = new ByteArrayContent(Encoding.UTF8.GetBytes(requestBody));
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, PriceBoardUrl)
        {
            Content = requestContent
        };

        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
        request.Headers.TryAddWithoutValidation("x-lang", "vi");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        JsonElement data = GetDataArray(document.RootElement);
        var quotesBySymbol = new Dictionary<string, StockQuote>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement item in data.EnumerateArray())
        {
            StockQuote? quote = TryCreateQuote(item);
            if (quote is not null)
            {
                quotesBySymbol[quote.Symbol] = quote;
            }
        }

        var quotes = symbols
            .Where(quotesBySymbol.ContainsKey)
            .Select(symbol => quotesBySymbol[symbol])
            .ToList();

        var invalidSymbols = symbols
            .Where(symbol => !quotesBySymbol.ContainsKey(symbol))
            .ToList();

        return new StockPriceLookupResult(quotes, invalidSymbols);
    }

    private static JsonElement GetDataArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out JsonElement data) &&
            data.ValueKind == JsonValueKind.Array)
        {
            return data;
        }

        throw new InvalidDataException("API tra ve du lieu khong dung dinh dang.");
    }

    private static StockQuote? TryCreateQuote(JsonElement item)
    {
        string? symbol = GetString(item, "SB");
        if (string.IsNullOrWhiteSpace(symbol) || !TryGetDecimal(item, "CP", out decimal currentPrice))
        {
            return null;
        }

        TryGetDecimal(item, "RE", out decimal referencePrice);
        TryGetDecimal(item, "CL", out decimal ceilingPrice);
        TryGetDecimal(item, "FL", out decimal floorPrice);
        TryGetDecimal(item, "CH", out decimal change);
        TryGetDecimal(item, "CHP", out decimal percentChange);
        TryGetDecimal(item, "HI", out decimal highPrice);
        TryGetDecimal(item, "LO", out decimal lowPrice);
        TryGetInt64(item, "TT", out long volume);

        return new StockQuote(
            symbol.ToUpperInvariant(),
            GetString(item, "EX") ?? "N/A",
            currentPrice,
            referencePrice,
            ceilingPrice,
            floorPrice,
            change,
            percentChange,
            highPrice,
            lowPrice,
            volume,
            GetString(item, "TD"),
            GetString(item, "IT"));
    }

    private static string? GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out JsonElement value)
            ? value.ToString()
            : null;
    }

    private static bool TryGetDecimal(JsonElement item, string propertyName, out decimal value)
    {
        value = default;
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.TryGetDecimal(out value)
            : decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetInt64(JsonElement item, string propertyName, out long value)
    {
        value = default;
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.TryGetInt64(out value)
            : long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
