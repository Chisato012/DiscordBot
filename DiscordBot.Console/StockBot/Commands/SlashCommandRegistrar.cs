using NetCord;
using NetCord.Rest;

namespace StockBot.Commands;

public static class SlashCommandRegistrar
{
    public static async Task EnsureCommandsAsync(
        RestClient restClient,
        ulong applicationId,
        IReadOnlySet<ulong> guildIds)
    {
        foreach (ulong guildId in guildIds)
        {
            IReadOnlyList<GuildApplicationCommand> commands =
                await restClient.GetGuildApplicationCommandsAsync(applicationId, guildId);

            foreach (SlashCommandProperties commandToAdd in GetCommands())
            {
                GuildApplicationCommand? existingCommand = commands.FirstOrDefault(command =>
                    command.Name.Equals(commandToAdd.Name, StringComparison.OrdinalIgnoreCase));

                if (existingCommand is not null)
                {
                    await restClient.ModifyGuildApplicationCommandAsync(
                        applicationId,
                        guildId,
                        existingCommand.Id,
                        options =>
                        {
                            options.Description = commandToAdd.Description;
                            options.Options = commandToAdd.Options;
                        });

                    continue;
                }

                await restClient.CreateGuildApplicationCommandAsync(applicationId, guildId, commandToAdd);
                Console.WriteLine($"\nCommand registered in guild {guildId}: /{commandToAdd.Name}.");
            }
        }
    }

    private static IEnumerable<SlashCommandProperties> GetCommands()
    {
        yield return new SlashCommandProperties("price", "Xem giá cổ phiếu")
        {
            Options = new[]
            {
                new ApplicationCommandOptionProperties(
                    ApplicationCommandOptionType.String,
                    "symbols",
                    "Một hoặc nhiều mã, cách nhau bằng dấu phẩy")
                {
                    Required = true
                }
            }
        };

        yield return new SlashCommandProperties("explain", "Giải thích các thuộc tính của một mã")
        {
            Options = new[]
            {
                new ApplicationCommandOptionProperties(
                    ApplicationCommandOptionType.String,
                    "symbol",
                    "Mã cổ phiếu, ví dụ: CMC")
                {
                    Required = true
                }
            }
        };

        yield return new SlashCommandProperties("auprice", "Bật, sửa hoặc dừng báo giá tự động")
        {
            Options = new[]
            {
                new ApplicationCommandOptionProperties(ApplicationCommandOptionType.SubCommand, "start", "Đăng kí báo giá tự động")
                {
                    Options = GetAutoPriceOptions()
                },
                new ApplicationCommandOptionProperties(ApplicationCommandOptionType.SubCommand, "edit", "Thay đổi báo giá tự động của bạn")
                {
                    Options = GetAutoPriceOptions()
                },
                new ApplicationCommandOptionProperties(ApplicationCommandOptionType.SubCommand, "cancel", "Dừng báo giá tự động của bạn")
            }
        };

        yield return new SlashCommandProperties("help", "Hiển thị các lệnh có sẵn");
    }

    private static ApplicationCommandOptionProperties[] GetAutoPriceOptions() =>
    [
        new ApplicationCommandOptionProperties(ApplicationCommandOptionType.String, "symbols", "Một hoặc nhiều mã, cách nhau bằng dấu phẩy")
        {
            Required = true
        },
        new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Integer, "seconds", "Khoảng thời gian, tối thiểu 60 giây (mặc định 60)")
    ];
}
