using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class SetupModule : BaseCommandModule {
  private static List<SetupParam> Params = null;
  public static ulong trackChannelID = 0;

  internal static void LoadParams() {
    Params = Database.GetAll<SetupParam>();
    trackChannelID = GetIDParam("TrackingChannel"); // 831186370445443104ul
  }

  [Command("setup")]
  [Description("Configure the bot")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
  public async Task Setup(CommandContext ctx) { // Show the possible options
    string msg =
      "**TrackingChannel** _<#channel>_  - to set what channel to use for tracking purposes.\n" +
      "**ListAdminRoles** - to list all admin roles.\n" +
      "**AddAdminRole** _<@Role>_ - adds a role to the admins.\n" +
      "**RemoveAdminRole** _<@Role>_ - removes the role to the admins.\n" +
      "**ServerId** - prints the current server guild id.\n" +
      "**GuildId** - prints the current server guild id.\n" +
      "**BotID** - prints the bot id in the current server guild.\n" +
      "**ListEmojiAppreciation** - to list all emojis for appreciation tracking.\n" +
      "**ListEmojiReputation** - to list all emojis for reputation tracking.\n" +
      "**ListEmojiFun** - to list all emojis for fun tracking.\n" +
      "**AddEmojiAppreciation** _emoji_ - to add an emoji for appreciation tracking.\n" +
      "**AddEmojiReputation** _emoji_ - to add an emoji for reputation tracking.\n" +
      "**AddEmojiFun** - _emoji_ - to add an emoji for fun tracking.\n" +
      "**RemoveEmojiAppreciation** _emoji_ - to remove an emoji for appreciation tracking.\n" +
      "**RemoveEmojiReputation** _emoji_ - to remove an emoji for reputation tracking.\n" +
      "**RemoveEmojiFun** - _emoji_ - to remove an emoji for fun tracking.\n" +
      "**ListStatsChannels** - to list all channels used for stats.\n" +
      "**AddStatsChannel** _<#channel>_ - adds a channel to the channels used for stats.\n" +
      "**removeStatsChannel** _<#channel>_ - removes the channel from the channels used for stats.";

    DiscordMessage answer = ctx.RespondAsync(msg).Result;
    await Utils.DeleteDelayed(30, ctx.Message, answer);
  }

  [Command("setup")]
  [Description("Configure the bot")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
  public async Task Setup(CommandContext ctx, string command) { // Command with no parameters
    command = command.ToLowerInvariant().Trim();
    switch (command) {
      case "trackingchannel": await TrackingChannel(ctx, null); break;
      case "botid": await GetIDs(ctx, true); break;
      case "serverid": await GetIDs(ctx, false); break;
      case "guildid": await GetIDs(ctx, false); break;

      default:
        DiscordMessage answer = ctx.RespondAsync("Unknown setup command").Result;
        await Utils.DeleteDelayed(30, ctx.Message, answer);
        break;
    }
  }

  [Command("setup")]
  [Description("Configure the bot")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
  public async Task Setup(CommandContext ctx, string command, DiscordChannel channel) { // Command with no parameters
    command = command.ToLowerInvariant().Trim();
    switch (command) {
      case "trackingchannel": await TrackingChannel(ctx, channel); break;

      default:
        DiscordMessage answer = ctx.RespondAsync("Unknown setup command").Result;
        await Utils.DeleteDelayed(30, ctx.Message, answer);
        break;
    }
  }

  Task TrackingChannel(CommandContext ctx, DiscordChannel channel) {
    try {
      string msg;
      if (channel == null) { // Read current value
        DiscordGuild guild = Utils.GetGuild();
        ulong channelid = GetIDParam("TrackingChannel");
        if (channelid == 0) {
          msg = "No channel set as Tracking Channel";
        } else {
          DiscordChannel tc = guild.GetChannel(channelid);
          msg = "Current tracking channel for this server is: " + tc.Mention + " (" + tc.Id + ")";
        }
      }
      else { // set the channel
        SetupParam p = new SetupParam(Utils.GetGuild().Id, "TrackingChannel", channel.Id);
        Database.Add(p);
        msg = "TrackingChannel set to " + channel.Mention;
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.TrackingChannel", ex));
    }
  }

  Task GetIDs(CommandContext ctx, bool forBot) {
    try {
      string msg;
      if (forBot) { // Read current value
        DiscordMember bot = Utils.GetMyself();
        msg = "Bot ID is: " + bot.Mention + " (" + bot.Id + ")";
      } else {
        DiscordGuild guild = Utils.GetGuild();
        msg = "Server/Guild ID is: " + guild.Name + " (" + guild.Id + ")";
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.GetIDs", ex));
    }
  }

  //  public async Task Setup(CommandContext ctx, [Description("The user that posted the message to check")] DiscordMember member) { // Refactors the previous post, if it is code

  /*
  bot self id: 875701548301299743ul
  roles for commands
  server guild: 830900174553481236ul
  ids for emojis
  ids for admins: 830901562960117780ul 830901743624650783ul 831050318171078718ul
  channels for stats: 
                830904407540367441ul, "Unity",
                830904726375628850ul, "CSharp",
                830921265648631878ul, "Help1",
                830921315657449472ul, "Help2",
  */

  static ulong GetIDParam(string param) {
    foreach (SetupParam p in Params) {
      if (p.Param == param) return p.IdVal;
    }
    return 0; // not found
  }
}


