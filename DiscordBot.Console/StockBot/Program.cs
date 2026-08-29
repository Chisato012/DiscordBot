using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using StockBot.Aphrodite;
using StockBot.Commands;
using StockBot.Services;

namespace StockBot;

class Program
{
    public static async Task Main(string[] args)
    {
        string botToken = await CreatePath.GetBotTokenAsync();
        IReadOnlySet<ulong> allowedChannelIds = await ChannelSettingsStore.GetAllowedChannelIdsAsync();

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            var stockPriceService = new StockPriceService(httpClient);
            var priceCommandHandler = new PriceCommandHandler(stockPriceService);
            var explainCommandHandler = new ExplainCommandHandler(stockPriceService);
            var helpCommandHandler = new HelpCommandHandler();
            using var autoPriceCommandHandler = new AUPriceCommandHandler(stockPriceService);
            using var client = new GatewayClient(new BotToken(botToken));
            client.Ready += async readyEventArgs =>
            {
                Console.Write("Login Successful: Bot name - ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{readyEventArgs.User.Username}");
                Console.ResetColor();

                try
                {
                    IReadOnlySet<ulong> guildIds = await GetGuildIdsAsync(client.Rest, allowedChannelIds);
                    await SlashCommandRegistrar.EnsureCommandsAsync(
                        client.Rest,
                        readyEventArgs.ApplicationId,
                        guildIds);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"\nCommand registration error: {exception.Message}");
                }
            };

            client.InteractionCreate += async interaction =>
            {
                if (interaction is not SlashCommandInteraction slashCommand ||
                    !allowedChannelIds.Contains(slashCommand.Channel.Id))
                {
                    return;
                }

                switch (slashCommand.Data.Name.ToLowerInvariant())
                {
                    case "price":
                        await priceCommandHandler.HandleAsync(slashCommand, client.Rest);
                        break;
                    case "explain":
                        await explainCommandHandler.HandleAsync(slashCommand, client.Rest);
                        break;
                    case "auprice":
                        await autoPriceCommandHandler.HandleAsync(slashCommand, client.Rest);
                        break;
                    case "help":
                        await helpCommandHandler.HandleAsync(slashCommand);
                        break;
                }
            };

            await client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception exception)
        {
            Console.WriteLine("Login Err: " + exception.Message);
        }

    }

    private static async Task<IReadOnlySet<ulong>> GetGuildIdsAsync(
        RestClient restClient,
        IReadOnlySet<ulong> channelIds)
    {
        var guildIds = new HashSet<ulong>();

        foreach (ulong channelId in channelIds)
        {
            Channel channel = await restClient.GetChannelAsync(channelId);
            if (channel is not IGuildChannel guildChannel)
            {
                throw new InvalidOperationException($"Channel ID {channelId} không thuộc một Discord server.");
            }

            guildIds.Add(guildChannel.GuildId);
        }

        return guildIds;
    }
}
