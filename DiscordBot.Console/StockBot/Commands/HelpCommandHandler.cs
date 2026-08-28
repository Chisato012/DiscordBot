using NetCord;
using NetCord.Rest;

namespace StockBot.Commands;

public sealed class HelpCommandHandler
{
    private const string HelpText =
        "**Các lệnh có sẵn**\n" +
        "`/price symbols:<mã>` - Xem giá một hoặc nhiều mã, ngăn cách bằng dấu phẩy. Ví dụ: `/price CMC`.\n" +
        "`/explain symbol:<mã>` - Giải thích các thuộc tính giá của một mã. Ví dụ: `/explain CMC`.\n" +
        "`/help` - Hiển thị hướng dẫn này.";

    public Task HandleAsync(SlashCommandInteraction interaction)
    {
        return interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties { Content = HelpText }));
    }
}
