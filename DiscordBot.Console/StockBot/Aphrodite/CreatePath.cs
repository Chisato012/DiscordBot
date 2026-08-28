using NetCord;
using StockBot.generate_class;
using StockBot.Models;
using StockBot.path;
using System.Text.Json;

namespace StockBot.Aphrodite;

internal static class CreatePath
{
    private static readonly string TokenPath = Path.Combine(folder_name.data, file_name.token);

    public static async Task<string> GetBotTokenAsync()
    {
        Directory.CreateDirectory(folder_name.data);

        string? savedToken = await ReadTokenAsync();
        if (!string.IsNullOrWhiteSpace(savedToken))
        {
            return savedToken;
        }

        if (!File.Exists(TokenPath))
        {
            await SaveTokenAsync(string.Empty);
            Console.WriteLine($"Token path: {TokenPath}");
        }

        string newToken;
        do
        {
            Console.Write("Bot token: ");
            newToken = (await Console.In.ReadLineAsync() ?? string.Empty).Trim();
        }
        while (string.IsNullOrWhiteSpace(newToken));

        await SaveTokenAsync(newToken);
        return newToken;
    }

    private static async Task<string?> ReadTokenAsync()
    {
        if (!File.Exists(TokenPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(TokenPath);
            Models.Discord? data = JsonSerializer.Deserialize(json, AppJsonContext.Default.Discord);
            return data?.token?.Trim();
        }
        catch (JsonException e)
        {
            Console.WriteLine($"Read token: {e.Message}");
            return null;
        }
    }

    private static async Task SaveTokenAsync(string token)
    {
        var data = new Models.Discord { token = token };
        string json = JsonSerializer.Serialize(data, AppJsonContext.Default.Discord);
        await File.WriteAllTextAsync(TokenPath, json);
    }
}
