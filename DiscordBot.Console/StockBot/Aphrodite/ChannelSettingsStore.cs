using StockBot.generate_class;
using StockBot.Models;
using StockBot.path;
using System.Text.Json;

namespace StockBot.Aphrodite;

internal static class ChannelSettingsStore
{
    private static readonly string SettingsPath = Path.Combine(folder_name.data, file_name.channels);

    public static async Task<IReadOnlySet<ulong>> GetAllowedChannelIdsAsync()
    {
        Directory.CreateDirectory(folder_name.data);

        IReadOnlySet<ulong>? savedIds = await ReadAsync();
        if (savedIds is { Count: > 0 })
        {
            return savedIds;
        }

        Console.WriteLine("ID Channel (Can split by ',' ex: 123456789,987654321)): ");
        while (true)
        {
            Console.Write("ID Channel: ");
            HashSet<ulong> channelIds = ParseChannelIds(await Console.In.ReadLineAsync());
            if (channelIds.Count == 0)
            {
                Console.WriteLine("Invalid ID Channel");
                continue;
            }

            await SaveAsync(channelIds);
            Console.WriteLine($"Save {channelIds.Count} in: {SettingsPath}");
            return channelIds;
        }
    }

    private static async Task<IReadOnlySet<ulong>?> ReadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(SettingsPath);
            BotChannelSettings? settings = JsonSerializer.Deserialize(json, AppJsonContext.Default.BotChannelSettings);

            return settings?.ChannelIds
                .Where(id => id != 0)
                .ToHashSet();
        }
        catch (JsonException e)
        {
            Console.WriteLine($"Error reading channels.json: {e.Message}");
            return null;
        }
    }

    private static async Task SaveAsync(IReadOnlyCollection<ulong> channelIds)
    {
        var settings = new BotChannelSettings
        {
            ChannelIds = channelIds.Order().ToList()
        };

        string json = JsonSerializer.Serialize(settings, AppJsonContext.Default.BotChannelSettings);
        await File.WriteAllTextAsync(SettingsPath, json);
    }

    private static HashSet<ulong> ParseChannelIds(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => ulong.TryParse(value, out ulong id) ? id : 0)
            .Where(id => id != 0)
            .ToHashSet();
    }
}
