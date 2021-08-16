using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// Deals with the functionality of loading, adding and removing "Custom Commands"
/// "Custom Commands" (short: CC) are a way of adding new commands without touching a single line of code
/// Moderators can add a new Custom Command using a Discord command
/// These "Custom Commands" will only display a specified text as a callback when someone calls them
/// </summary>
public class CustomCommandsService : BaseCommandModule
{
    private static readonly List<CustomCommand> Commands = new List<CustomCommand>();
    internal static DiscordClient DiscordClient { get; set; }
    internal const string DirectoryNameCC = "CustomCommands";

    [Command("newcc")]
    [Aliases("createcc", "addcc", "ccadd", "cccreate")]
    [Description("**Create** a new Custom Command (so-called 'CC') with a specified name and all aliases if desired " +
                 "(no duplicate alias allowed).\nAfter doing this, the bot will ask you to input the content, which will " +
                 "be displayed once someone invokes this CC. Your entire next message will be used for the content, so " +
                 "be careful what you type!\n\n**Usage:**\n\n- `newcc name` (without alias)\n- `newcc name alias1 alias2`" +
                 " (with 2 aliases)\n\nThis command can only be invoked by a Mod.")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task CreateCommand(CommandContext ctx, [Description("A 'list' of all aliases. The first term is the **main name**, the other ones, separated by a space, are aliases")] params string[] names)
    {
        foreach (var name in names)
        {
            if (DiscordClient.GetCommandsNext().RegisteredCommands.ContainsKey(name)) // Check if there is a command with one of the names already
            {
                await UtilityFunctions.ErrorCallback(CommandErrors.CommandExists, ctx, name);
                return;
            }
            
            foreach (var cmd in Commands)
            {
                if (cmd.Names.Contains(name)) // Check if there is already a CC with one of the names
                {
                    await UtilityFunctions.ErrorCallback(CommandErrors.CommandExists, ctx, name);
                    return;
                }
            }
        }

        string content = await WaitForContent(ctx, names[0]);
        CustomCommand command = new CustomCommand(names, content);
        Commands.Add(command);
        await WriteToFile(command);
        
        string embedMessage = $"CC {names[0]} successfully created and saved!";
        await UtilityFunctions.BuildEmbedAndExecute("Success", embedMessage, UtilityFunctions.Green, ctx, false);
    }

    [Command("ccdel")]
    [Aliases("ccdelete", "ccremove", "delcc", "deletecc", "removecc")]
    [Description("**Delete** a Custom Command (so-called 'CC').\n**Attention!** Use the main name of the CC " +
                 "you entered first when you created it, **not an alias!**\nThe CC will be irrevocably deleted." +
                 "\n\nThis command can only be invoked by a Mod.")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task DeleteCommand(CommandContext ctx, [Description("Main name of the CC you want to delete")] string name)
    {
        string filePath = UtilityFunctions.ConstructPath(DirectoryNameCC, name, ".txt");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            if (TryGetCommand(name, out CustomCommand cmd))
                Commands.Remove(cmd);
            
            string embedMessage = $"CC {name} successfully deleted!";
            await UtilityFunctions.BuildEmbedAndExecute("Success", embedMessage, UtilityFunctions.Green, ctx, true);
        }
    }

    [Command("ccedit")]
    [Aliases("editcc")]
    [Description("**Edit** the **content** of a Custom Command (so-called 'CC')." +
                 "\n**Attention!** Use the main name of the CC you entered first when you created it, **not an alias!**" +
                 "\n\nThis command can only be invoked by a Mod.")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task EditCommand(CommandContext ctx, [Description("Main name of the CC you want to edit")] string name)
    {
        string filePath = UtilityFunctions.ConstructPath(DirectoryNameCC, name, ".txt");
        if (File.Exists(filePath))
        {
            string content = await WaitForContent(ctx, name);
            string firstLine;
            using (StreamReader sr = File.OpenText(filePath))
                firstLine = await sr.ReadLineAsync();
            
            
            await using (StreamWriter sw = File.CreateText(filePath))
            {
                await sw.WriteLineAsync(firstLine);
                await sw.WriteLineAsync(content);
            }

            if (TryGetCommand(name, out CustomCommand command))
                command.EditCommand(content);

            string embedMessage = $"CC **{name}** successfully edited!";
            await UtilityFunctions.BuildEmbedAndExecute("Success", embedMessage, UtilityFunctions.Green, ctx, false);
        }
        else
            await UtilityFunctions.ErrorCallback(CommandErrors.MissingCommand, ctx);
    }

    [Command("cceditname")]
    [Aliases("ccnameedit", "editnamecc")]
    [Description("**Edit** the **names** (including aliases) of an existing CC." +
                 "\n**Attention!** Use the main name of the CC you entered first when you created it, **not an alias!**" +
                 "\n\nThis command can only be invoked by a Mod.")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task EditCommandName(CommandContext ctx, [Description("A list of new names and aliases, " +
                                                                       "__**BUT**__ the **FIRST** term is the current **main name** " +
                                                                       "of the CC whose name you want to edit, the **SECOND** term " +
                                                                       "is the new **main name** and all the other terms are new aliases")] params string[] names)
    {
        if (names.Length < 2)
        {
            await UtilityFunctions.ErrorCallback(CommandErrors.InvalidParams, ctx);
            return;
        }
        
        string filePath = UtilityFunctions.ConstructPath(DirectoryNameCC, names[0], ".txt");
        if (File.Exists(filePath))
        {
            if (TryGetCommand(names[0], out CustomCommand command))
                command.EditCommand(names.Skip(1).ToArray());
            
            string content = string.Empty;
            using (StreamReader sr = File.OpenText(filePath))
            {
                string c;
                await sr.ReadLineAsync();
                while ((c = await sr.ReadLineAsync()) != null)
                    content += c + System.Environment.NewLine;
            }

            string newPath = UtilityFunctions.ConstructPath(DirectoryNameCC, names[1], ".txt");
            File.Move(filePath, newPath);
            using (StreamWriter sw = File.CreateText(newPath))
            {
                await sw.WriteLineAsync(string.Join(',', names.Skip(1)));
                await sw.WriteLineAsync(content);
            }

            string embedDescription = "The CC names have been successfully edited.";
            await UtilityFunctions.BuildEmbedAndExecute("Success", embedDescription, UtilityFunctions.Green, ctx, false);
        }
        else
            await UtilityFunctions.ErrorCallback(CommandErrors.MissingCommand, ctx);
    }

    [Command("cclist")]
    [Aliases("listcc")]
    [Description("Get a list of all Custom Commands (CC's).")]
    public async Task ListCC(CommandContext ctx)
    {
        if (Commands.Count <= 0)
        {
            await UtilityFunctions.ErrorCallback(CommandErrors.NoCustomCommands, ctx);
            return;
        }

        string allCommands = string.Empty;
        foreach (var cmd in Commands)
        {
            if(cmd.Names.Length > 0)
                allCommands += $"- {cmd.Names[0]} ({(cmd.Names.Length > 1 ? string.Join(", ", cmd.Names.Skip(1)) : string.Empty)}){System.Environment.NewLine}";
        }

        await UtilityFunctions.BuildEmbedAndExecute("CC List", allCommands, UtilityFunctions.Yellow, ctx, true);
    }

    internal static async Task LoadCustomCommands()
    {
        string path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "CustomCommands");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        foreach (string fileName in Directory.GetFiles(path))
        {
            using (StreamReader sr = File.OpenText(fileName))
            {
                string names = await sr.ReadLineAsync();
                if (string.IsNullOrEmpty(names))
                    continue;

                string content = string.Empty;
                string c;
                while ((c = await sr.ReadLineAsync()) != null)
                    content += c + System.Environment.NewLine;

                CustomCommand cmd = new CustomCommand(names.Split(','), content);
                Commands.Add(cmd);
            }
        }
    }

    internal static async Task CommandError(CommandsNextExtension extension, CommandErrorEventArgs args)
    {
        if (args.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
        {
            string commandName = args.Context.Message.Content.Split(' ')[0].Substring(1);
            if (TryGetCommand(commandName, out CustomCommand command))
                await command.ExecuteCommand(args.Context);
        }
    }

    private async Task WriteToFile(CustomCommand command)
    {
        if (!File.Exists(command.FilePath))
        {
            await using (StreamWriter sw = File.AppendText(command.FilePath))
            {
                await sw.WriteLineAsync(string.Join(',', command.Names));
                await sw.WriteLineAsync(command.Content);
            }
        }
    }

    private async Task<string> WaitForContent(CommandContext ctx, string name)
    {
        string embedMessage = $"Please input the content of the CC **{name}** in one single message. Your next message will count as the content.";
        await UtilityFunctions.BuildEmbedAndExecute("Waiting for interaction", embedMessage, UtilityFunctions.LightBlue, ctx, true);

        string content = string.Empty;
        await ctx.Message.GetNextMessageAsync(m =>
        {
            content = m.Content;
            return true;
        });
        
        return content;
    }

    private static bool TryGetCommand(string name, out CustomCommand command)
    {
        command = GetCommandByName(name);
        return command != null;
    }

    private static CustomCommand GetCommandByName(string name)
    {
        return Commands.FirstOrDefault(cc => cc.Names.Contains(name));
    }
}