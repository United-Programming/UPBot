using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using System.Threading.Tasks;
/// <summary>
/// This command implements a Logs command.
/// It can be used by admins to check the logs and download them
/// author: CPU
/// </summary>
public class Logs : BaseCommandModule {

  [Command("Logs")]
  [Description("Allows to see and download guild logs (only admins)")]
  public async Task LogsCommand(CommandContext ctx) {
    if (ctx.Guild == null) return;
    
    try {
      if (!Setup.HasAdminRole(ctx.Guild.Id, ctx.Member.Roles, false)) return;

      await Utils.DeleteDelayed(60, ctx.Message.RespondAsync(helpMsg));
    } catch (Exception ex) {
      Utils.Log("Error in LogsCommand: " + ex.Message, ctx.Guild.Name);
    }
  }


  [Command("Logs")]
  [Description("Allows to see and download guild logs (only admins)")]
  public async Task LogsCommand(CommandContext ctx, [Description("How many lines to show or 'save'")]string what) {
    if (ctx.Guild == null) return;
    Utils.LogUserCommand(ctx);
    try {
      if (!Setup.HasAdminRole(ctx.Guild.Id, ctx.Member.Roles, false)) return;

      string logs = Utils.GetLogsPath(ctx.Guild.Name);

      if (int.TryParse(what, out int num)) {
        if (num < 1) num = 1;
        if (num > 25) num = 25;
        if (logs == null) {
          await Utils.DeleteDelayed(60, ctx.Message.RespondAsync($"There are no logs today for the guild **{ctx.Guild.Name}**"));
          return;
        }

        List<string> lines = new List<string>();
        using (var fs = new FileStream(logs, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
          using var sr = new StreamReader(fs);
          while (!sr.EndOfStream) {
            lines.Add(sr.ReadLine());
          }
        }

        int start = lines.Count - num;
        if (start < 0) start = 0;
        string res = $"Last {num} lines of logs:\n```";
        while (start < lines.Count) {
          res += lines[start] + "\n";
          start++;
        }
        if (res.Length > 1990) res = res[0..1990] + "...\n";

        res += "```";
        await Utils.DeleteDelayed(60, ctx.Message.RespondAsync(res));
        Utils.LogUserCommand(ctx);
        return;
      }

      else if (what.Equals("save", StringComparison.InvariantCultureIgnoreCase)) {
        if (logs == null) {
          await Utils.DeleteDelayed(60, ctx.Message.RespondAsync($"There are no logs today for the guild **{ctx.Guild.Name}**"));
          Utils.LogUserCommand(ctx);
          return;
        }

        string logsFolder = Utils.GetLastLogsFolder(ctx.Guild.Name, logs);
        string outfile = logsFolder[0..^1] + ".zip";
        ZipFile.CreateFromDirectory(logsFolder, outfile);

        DiscordMessage msg = null;
        using (FileStream fs = new FileStream(outfile, FileMode.Open, FileAccess.Read))
          msg = await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent("Zipped log in attachment").WithFiles(new Dictionary<string, Stream>() { { outfile, fs } }));
        await Utils.DeleteDelayed(60, msg);
        await Utils.DeleteFileDelayed(30, outfile);
        await Utils.DeleteFolderDelayed(30, logsFolder);
        Utils.LogUserCommand(ctx);
        return;
      }

      else if (what.Equals("saveall", StringComparison.InvariantCultureIgnoreCase)) {
        string logsFolder = Utils.GetAllLogsFolder(ctx.Guild.Name);

        string outfile = logsFolder[0..^1] + ".zip";
        ZipFile.CreateFromDirectory(logsFolder, outfile);

        DiscordMessage msg = null;
        using (FileStream fs = new FileStream(outfile, FileMode.Open, FileAccess.Read))
          msg = await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent("Zipped logs in attachment").WithFiles(new Dictionary<string, Stream>() { { outfile, fs } }));
        await Utils.DeleteDelayed(60, msg);
        await Utils.DeleteFileDelayed(30, outfile);
        await Utils.DeleteFolderDelayed(30, logsFolder);
        Utils.LogUserCommand(ctx);
        return;
      }

      else if (what.Equals("delete", StringComparison.InvariantCultureIgnoreCase)) {
        await Utils.DeleteDelayed(60, ctx.Message.RespondAsync("You have to specify the full guild name after 'delete' (_case sensitive_) to confirm the delete of the logs."));
        Utils.LogUserCommand(ctx);
        return;
      }


      await Utils.DeleteDelayed(60, ctx.Message.RespondAsync(helpMsg));

    } catch (Exception ex) {
      Utils.Log("Error in LogsCommand: " + ex.Message, ctx.Guild.Name);
    }
  }

  [Command("LogsDelete")]
  [Description("Allows to see and download guild logs (only admins)")]
  public async Task LogsCommand(CommandContext ctx, [Description("How many lines to show or 'save'")]string what, [Description("The name of the guild, case sensitive, to confirm the delete")]string guildname) {
    if (ctx.Guild == null) return;
    try {
      if (!Setup.HasAdminRole(ctx.Guild.Id, ctx.Member.Roles, false)) return;

      if (!what.Equals("delete", StringComparison.InvariantCultureIgnoreCase)) {
        await Utils.DeleteDelayed(60, ctx.Message.RespondAsync(helpMsg));
        return;
      }

      if (!guildname.Equals(ctx.Guild.Name)) {
        await Utils.DeleteDelayed(60, ctx.Message.RespondAsync("You have to specify the full guild name after 'delete' (_case sensitive_) to confirm the delete of the logs."));
        return;
      }

      int num = Utils.DeleteAllLogs(ctx.Guild.Name);
      if (num == 1)
        await Utils.DeleteDelayed(60, await ctx.Message.RespondAsync($"1 log file for guild **{ctx.Guild.Name}** has been deleted"));
      else
        await Utils.DeleteDelayed(60, await ctx.Message.RespondAsync($"{num} log files for guild **{ctx.Guild.Name}** have been deleted"));

    } catch (Exception ex) {
      Utils.Log("Error in LogsCommand: " + ex.Message, ctx.Guild.Name);
    }
  }

  const string helpMsg = "Specify how many lines (up to 25) or `save` to download the zipped logs of today, or `saveall` for a zip of all logs of the guild.\nUse `delete` _name of guild_ to delete all logs for the guild.";
}