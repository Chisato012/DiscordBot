using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StockBot.Models
{
    public class Discord
    {
        [JsonPropertyName("bot_token")]
        public string? token { get; set; }
    }
}
