using StockBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StockBot.generate_class
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Discord))]
    [JsonSerializable(typeof(BotChannelSettings))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
