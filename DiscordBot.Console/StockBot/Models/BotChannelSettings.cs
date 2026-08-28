using System.Text.Json.Serialization;

namespace StockBot.Models;

public sealed class BotChannelSettings
{
    [JsonPropertyName("channel_ids")]
    public List<ulong> ChannelIds { get; set; } = [];
}
