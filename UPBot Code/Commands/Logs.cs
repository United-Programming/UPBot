using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

/// <summary>
/// This command implements a Logs command.
/// It can be used by admins to check the logs and download them
/// author: CPU
/// </summary>
[SlashCommandGroup("logs", "Commands to show the logs")]
public class SlashLogs : ApplicationCommandModule {

  [SlashCommand("show", "Allows to see and download guild logs")]
  public async Task LogsCommand(InteractionContext ctx, [Option("NumerOflines", "How many lines of logs to get")][Minimum(5)][Maximum(25)] long numLines) {
    if (ctx.Guild == null) return;
    Utils.LogUserCommand(ctx);

    string logs = Utils.GetLogsPath(ctx.Guild.Name);
    if (logs == null) {
      await ctx.CreateResponseAsync($"There are no logs today for the guild **{ctx.Guild.Name}**", true);
      return;
    }

    List<string> lines = new List<string>();
    using (var fs = new FileStream(logs, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
      using var sr = new StreamReader(fs);
      while (!sr.EndOfStream) {
        lines.Add(sr.ReadLine());
      }
    }

    int start = lines.Count - (int)numLines;
    if (start < 0) start = 0;
    string res = "";
    while (start < lines.Count) {
      res += lines[start].Replace("```", "\\`\\`\\`") + "\n";
      start++;
    }
    if (res.Length > 1990) res = res[-1990..] + "...\n";
    res = $"Last {numLines} lines of logs:\n```\n" + res + "```";
    await ctx.CreateResponseAsync(res);
  }


  [SlashCommand("save", "Creates a zip file of the last logs of the server")]
  public async Task LogsSaveCommand(InteractionContext ctx) {
    if (ctx.Guild == null) return;
    Utils.LogUserCommand(ctx);

    string logs = Utils.GetLogsPath(ctx.Guild.Name);
    if (logs == null) {
      await ctx.CreateResponseAsync($"There are no logs today for the guild **{ctx.Guild.Name}**", true);
      return;
    }
    string logsFolder = Utils.GetLastLogsFolder(ctx.Guild.Name, logs);
    string outfile = logsFolder[0..^1] + ".zip";
    ZipFile.CreateFromDirectory(logsFolder, outfile);


    using (FileStream fs = new FileStream(outfile, FileMode.Open, FileAccess.Read))
      await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Zipped log in attachment").AddFile(fs));
    await Utils.DeleteFileDelayed(30, outfile);
    await Utils.DeleteFolderDelayed(30, logsFolder);
  }

  [SlashCommand("saveall", "Creates a zip file of the all the server logs")]
  public async Task LogsSaveAllCommand(InteractionContext ctx) {
    if (ctx.Guild == null) return;
    Utils.LogUserCommand(ctx);

    string logsFolder = Utils.GetAllLogsFolder(ctx.Guild.Name);

    string outfile = logsFolder[0..^1] + ".zip";
    ZipFile.CreateFromDirectory(logsFolder, outfile);

    using (FileStream fs = new FileStream(outfile, FileMode.Open, FileAccess.Read))
      await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Zipped logs in attachment").AddFile(fs));
    await Utils.DeleteFileDelayed(30, outfile);
    await Utils.DeleteFolderDelayed(30, logsFolder);
  }

  [SlashCommand("delete", "Removes the server logs")]
  public async Task LogsDeleteCommand(InteractionContext ctx, [Option("GuildName", "The name of the guild, case sensitive, to confirm the delete")] string guildname) {
    if (ctx.Guild == null) return;
    Utils.LogUserCommand(ctx);

    string logs = Utils.GetLogsPath(ctx.Guild.Name);
    if (logs == null) {
      await ctx.CreateResponseAsync($"There are no logs today for the guild **{ctx.Guild.Name}**", true);
      return;
    }

    if (!guildname.Equals(ctx.Guild.Name)) {
      await ctx.CreateResponseAsync("You have to specify the full guild name after 'delete' (_case sensitive_) to confirm the delete of the logs.", true);
      return;
    }

    int num = Utils.DeleteAllLogs(ctx.Guild.Name);
    if (num == 1)
      await ctx.CreateResponseAsync($"1 log file for guild **{ctx.Guild.Name}** has been deleted");
    else
      await ctx.CreateResponseAsync($"{num} log files for guild **{ctx.Guild.Name}** have been deleted");
  }

}