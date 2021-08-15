using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

public class CustomCommandsService
{
    private static List<CustomCommand> commands = new List<CustomCommand>();
    internal static DiscordClient DiscordClient { get; set; }

    [Command("newcc")]
    [Aliases("newCC", "createcc", "createCC", "addcc", "addCC", "ccadd", "ccAdd", "cccreate", "ccCreate")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task CreateCommand(CommandContext ctx, params string[] names)
    {
        foreach (var name in names)
        {
            if (DiscordClient.GetCommandsNext().RegisteredCommands.ContainsKey(name))
            {
                await ErrorCallback(ctx, name);
                return;
            }
            
            foreach (var cmd in commands)
            {
                if (cmd.Names.Contains(name))
                {
                    await ErrorCallback(ctx, name);
                    return;
                }
            }
        }
        
        await ctx.RespondAsync($"Please input the content of the CC {names[0]} in one single message. Your next message will count as the content.");
        string content = string.Empty;
        await ctx.Message.GetNextMessageAsync(m =>
        {
            content = m.Content;
            return true;
        });
        CustomCommand command = new CustomCommand(names, content);
        await WriteToFile(command);
    }

    [Command("delcc")]
    [Aliases("delCC", "deletecc", "deleteCC", "removecc", "removeCC")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task DeleteCommand(CommandContext ctx, string name)
    {
        if (File.Exists(name))
        {
            File.Delete(name);
            await ctx.RespondAsync($"CC {name} successfully deleted!");
        }
    }

    internal static async Task LoadCustomCommands()
    {
        foreach (string fileName in Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory))
        {
            using (StreamReader sr = File.OpenText(fileName))
            {
                string names = await sr.ReadLineAsync();
                if (string.IsNullOrEmpty(names))
                    continue;

                string content = await sr.ReadLineAsync();

                CustomCommand cmd = new CustomCommand(names.Split(','), content);
                commands.Add(cmd);
            }
        }
    }

    private async Task WriteToFile(CustomCommand command)
    {
        if (!File.Exists(command.FilePath))
        {
            await using (StreamWriter sw = File.AppendText(command.FilePath))
            {
                await sw.WriteLineAsync(string.Join(',', command.Names));
            }
        }
    }

    private async Task ErrorCallback(CommandContext ctx, string name)
    {
        await ctx.RespondAsync($"There is already a command containing the alias {name}");
    }
}