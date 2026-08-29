using NetCord;
using NetCord.Rest;

namespace StockBot.Commands;

public sealed class HelpCommandHandler
{
    private const string HelpText =
        "**Các lệnh có sẵn**\n" +
        "`/price symbols:<mã>` - Xem giá một hoặc nhiều mã, ngăn cách bằng dấu phẩy.\n" +
        "`/explain symbol:<mã>` - Giải thích các thuộc tính giá của một mã.\n" +
        "`/auprice start symbols:<mã> [seconds:<giây>]` - Đăng kí báo giá tự động (mặc định và tối thiểu là 60 giây).\n" +
        "`/auprice edit symbols:<mã> [seconds:<giây>]` - Thay đổi báo giá tự động của bạn.\n" +
        "`/auprice cancel` - Dừng báo giá tự động của bạn.\n" +
        "`/auprice cancelall` - Dừng tất cả tiến trình báo giá tự động.\n" +
        "`/help` - Hiển thị hướng dẫn này.";

    public Task HandleAsync(SlashCommandInteraction interaction)
    {
        return interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties { Content = HelpText }));
    }
}
