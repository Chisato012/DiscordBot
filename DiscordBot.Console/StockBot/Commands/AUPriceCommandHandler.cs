using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using NetCord;
using NetCord.Rest;
using StockBot.Models;
using StockBot.Services;

namespace StockBot.Commands;

public sealed partial class AUPriceCommandHandler : IDisposable
{
    private const int DefaultIntervalSeconds = 60;
    private const int MinimumIntervalSeconds = 60;
    private const int MaximumSymbols = 10;
    private readonly ConcurrentDictionary<ulong, PriceSubscription> _subscriptions = new();
    private readonly object _subscriptionsLock = new();
    private readonly StockPriceService _stockPriceService;

    public AUPriceCommandHandler(StockPriceService stockPriceService) => _stockPriceService = stockPriceService;

    public async Task HandleAsync(SlashCommandInteraction interaction, RestClient restClient)
    {
        await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        ApplicationCommandInteractionDataOption? subcommand = interaction.Data.Options.FirstOrDefault();
        string action = subcommand?.Name.ToLowerInvariant() ?? string.Empty;
        try
        {
            string content = action switch
            {
                "start" => Start(interaction, restClient, subcommand),
                "edit" => Edit(interaction, restClient, subcommand),
                "cancel" => Cancel(interaction.User.Id),
                "cancelall" => CancelAll(),
                _ => "Lệnh không hợp lệ. Dùng `/auprice start`, `/auprice edit` hoặc `/auprice cancel`."
            };
            await UpdateResponseAsync(interaction, restClient, content);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Auto price command error: {exception.Message}");
            await UpdateResponseAsync(interaction, restClient, "Không thể cập nhật báo giá tự động ngay lúc này.");
        }
    }

    private string Start(SlashCommandInteraction interaction, RestClient restClient, ApplicationCommandInteractionDataOption? subcommand)
    {
        if (!TryCreateSubscription(interaction, restClient, subcommand, out PriceSubscription? subscription, out string error))
            return error;

        lock (_subscriptionsLock)
        {
            if (_subscriptions.TryGetValue(interaction.User.Id, out PriceSubscription? existingSubscription))
            {
                subscription.Cancellation.Dispose();
                return $"Đăng kí thành công tự báo giá với **{string.Join(", ", existingSubscription.Symbols)}**. Dùng `/auprice edit` để đổi hoặc `/auprice cancel` để dừng.";
            }

            _subscriptions[interaction.User.Id] = subscription;
            subscription.Runner = RunSubscriptionAsync(interaction.User.Id, subscription);
        }
        return $"Đã bật báo giá tự động cho **{string.Join(", ", subscription.Symbols)}** mỗi **{subscription.Interval.TotalSeconds:0} giây**. Dùng `/auprice edit` để thay đổi hoặc `/auprice cancel` để dừng.";
    }

    private string Edit(SlashCommandInteraction interaction, RestClient restClient, ApplicationCommandInteractionDataOption? subcommand)
    {
        PriceSubscription previousSubscription;
        lock (_subscriptionsLock)
        {
            if (!_subscriptions.TryGetValue(interaction.User.Id, out PriceSubscription? foundSubscription) || foundSubscription is null)
                return "Bạn chưa đăng kí báo giá tự động. Dùng `/auprice start` để tạo mới.";
            previousSubscription = foundSubscription;
        }

        if (!TryCreateSubscription(interaction, restClient, subcommand, out PriceSubscription? subscription, out string error))
            return error;

        lock (_subscriptionsLock)
        {
            if (!_subscriptions.TryGetValue(interaction.User.Id, out PriceSubscription? currentSubscription) || currentSubscription != previousSubscription)
            {
                subscription.Cancellation.Dispose();
                return "Không thể thay đổi báo giá tự động vì một lệnh khác vừa được xử lý. Hãy thử lại.";
            }

            _subscriptions[interaction.User.Id] = subscription;
            previousSubscription.Cancellation.Cancel();
            subscription.Runner = RunSubscriptionAsync(interaction.User.Id, subscription);
        }
        return $"Đã cập nhật báo giá tự động: **{string.Join(", ", subscription.Symbols)}**, mỗi **{subscription.Interval.TotalSeconds:0} giây**.";
    }

    private string Cancel(ulong userId)
    {
        lock (_subscriptionsLock)
        {
            if (!_subscriptions.TryRemove(userId, out PriceSubscription? subscription))
                return "Bạn không có báo giá tự động nào đang chạy.";
            subscription.Cancellation.Cancel();
        }
        return $"Đã dừng báo giá tự động của bạn (User Id: {userId}).";
    }

    private string CancelAll()
    {
        int count;
        lock (_subscriptionsLock)
        {
            count = _subscriptions.Count;
            foreach (PriceSubscription subscription in _subscriptions.Values)
                subscription.Cancellation.Cancel();
            _subscriptions.Clear();
        }
        return $"Đã dừng tất cả tiến trình báo giá tự động ({count} đăng kí).";
    }

    private bool TryCreateSubscription(SlashCommandInteraction interaction, RestClient restClient, ApplicationCommandInteractionDataOption? subcommand, [NotNullWhen(true)] out PriceSubscription? subscription, out string error)
    {
        subscription = null;
        error = string.Empty;
        IReadOnlyList<string> symbols = ParseSymbols(GetOptionValue(subcommand, "symbols"));
        if (symbols.Count == 0)
        {
            error = "Nhập ít nhất một mã hợp lệ.";
            return false;
        }
        if (symbols.Count > MaximumSymbols)
        {
            error = $"Mỗi báo giá tự động chỉ hỗ trợ tối đa {MaximumSymbols} mã.";
            return false;
        }
        string? secondsText = GetOptionValue(subcommand, "seconds");
        int intervalSeconds = DefaultIntervalSeconds;
        if (!string.IsNullOrWhiteSpace(secondsText) && (!int.TryParse(secondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out intervalSeconds) || intervalSeconds < MinimumIntervalSeconds))
        {
            error = $"Khoảng thời gian phải là số nguyên và không nhỏ hơn {MinimumIntervalSeconds} giây.";
            return false;
        }
        subscription = new PriceSubscription(interaction.Channel.Id, symbols, TimeSpan.FromSeconds(intervalSeconds), restClient, new CancellationTokenSource());
        return true;
    }

    private async Task RunSubscriptionAsync(ulong userId, PriceSubscription subscription)
    {
        try
        {
            while (true)
            {
                await Task.Delay(subscription.Interval, subscription.Cancellation.Token);
                try
                {
                    StockPriceLookupResult result = await _stockPriceService.GetPricesAsync(subscription.Symbols, subscription.Cancellation.Token);
                    await subscription.RestClient.SendMessageAsync(subscription.ChannelId, new MessageProperties { Content = FormatPriceReport(result) }, cancellationToken: subscription.Cancellation.Token);
                }
                catch (OperationCanceledException) when (subscription.Cancellation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Auto price update error for user {userId}: {exception.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (subscription.Cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Console.WriteLine($"Auto price subscription error for user {userId}: {exception.Message}");
        }
        finally
        {
            lock (_subscriptionsLock)
                ((ICollection<KeyValuePair<ulong, PriceSubscription>>)_subscriptions).Remove(new(userId, subscription));
            subscription.Cancellation.Dispose();
        }
    }

    private static string? GetOptionValue(ApplicationCommandInteractionDataOption? subcommand, string name) => subcommand?.Options?.FirstOrDefault(option => option.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static IReadOnlyList<string> ParseSymbols(string? symbolsText)
    {
        if (string.IsNullOrWhiteSpace(symbolsText)) return Array.Empty<string>();
        return symbolsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(symbol => symbol.ToUpperInvariant()).Where(symbol => SymbolPattern().IsMatch(symbol)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string FormatPriceReport(StockPriceLookupResult result)
    {
        var lines = new List<string> { "**Báo giá tự động**" };
        foreach (StockQuote quote in result.Quotes)
        {
            string timestamp = string.Join(' ', new[] { quote.TradingDate, quote.TradingTime });
            string sign = quote.Change > 0 ? "+" : string.Empty;
            lines.Add($"**{quote.Symbol}** ({quote.Exchange}) — **{FormatNumber(quote.CurrentPrice)} VND**\n" +
                $"- Thay đổi: {sign}{FormatNumber(quote.Change)} ({sign}{quote.PercentChange:0.##}%)\n" +
                $"- Cao/Thấp: {FormatNumber(quote.HighPrice)}/{FormatNumber(quote.LowPrice)}\n" +
                $"- KL: {FormatNumber(quote.Volume)}\n" +
                $"*Thời gian cập nhật: {timestamp}*.\n");
        }
        if (result.InvalidSymbols.Count > 0) lines.Add($"Không tìm thấy dữ liệu: **{string.Join(", ", result.InvalidSymbols)}**");
        return lines.Count > 1 ? string.Join("\n", lines) : "**Báo giá tự động**\nKhông tìm thấy dữ liệu cho các mã đã nhập.";
    }

    private static Task UpdateResponseAsync(SlashCommandInteraction interaction, RestClient restClient, string content) => restClient.ModifyInteractionResponseAsync(interaction.ApplicationId, interaction.Token, options => options.Content = content);
    private static string FormatNumber(decimal number) => number.ToString("N0", CultureInfo.InvariantCulture);
    private static string FormatNumber(long number) => number.ToString("N0", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        lock (_subscriptionsLock)
        {
            foreach (PriceSubscription subscription in _subscriptions.Values) subscription.Cancellation.Cancel();
            _subscriptions.Clear();
        }
    }

    [GeneratedRegex("^[A-Z0-9]{1,10}$")]
    private static partial Regex SymbolPattern();

    private sealed class PriceSubscription(ulong channelId, IReadOnlyList<string> symbols, TimeSpan interval, RestClient restClient, CancellationTokenSource cancellation)
    {
        public ulong ChannelId { get; } = channelId;
        public IReadOnlyList<string> Symbols { get; } = symbols;
        public TimeSpan Interval { get; } = interval;
        public RestClient RestClient { get; } = restClient;
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Task? Runner { get; set; }
    }
}
