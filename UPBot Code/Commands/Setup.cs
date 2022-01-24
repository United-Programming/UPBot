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
  public static List<ulong> AdminRoles;
  public static List<Stats.StatChannel> StatsChannels;

  internal static void LoadParams(bool forceCleanBad = false) {
    DiscordGuild guild = Utils.GetGuild();
    Params = Database.GetAll<SetupParam>();
    if (Params == null) Params = new List<SetupParam>();
    trackChannelID = GetIDParam("TrackingChannel"); // 831186370445443104ul
    if (trackChannelID == 0) {
      trackChannelID = guild.SystemChannel.Id;
      SetupParam p = new SetupParam("TrackingChannel", trackChannelID);
      Params.Add(p);
      Database.Add(p);
      Utils.Log("Tracking channel set as default system channel: " + guild.SystemChannel.Name);
    }
    // Admin roles
    AdminRoles = new List<ulong>();
    foreach (var param in Params) {
      if (param.Param == "AdminRole") {
        try {
          DiscordRole r = guild.GetRole(param.IdVal);
          if (r != null) AdminRoles.Add(r.Id);
        } catch (Exception ex) {
          Utils.Log("Error in reading roles from Setup: " + param.IdVal + ": " + ex.Message);
          if (forceCleanBad) {
            Database.Delete(param);
          }
        }
      }
    }
    if (AdminRoles.Count == 0) {
      foreach(DiscordRole role in guild.Roles.Values) {
        if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed || role.CheckPermission(DSharpPlus.Permissions.ManageGuild) == DSharpPlus.PermissionLevel.Allowed) {
          AdminRoles.Add(role.Id);
          SetupParam p = new SetupParam("AdminRole", role.Id);
          Database.Add(p);
          Params.Add(p);
          Utils.Log("Added role " + role.Name + " as default admin role (no admins were found)");
        }
      }
    }
    // Stats channels
    StatsChannels = new List<Stats.StatChannel>();
    foreach (var param in Params) {
      if (param.Param == "StatsChannel") {
        try {
          DiscordChannel c = guild.GetChannel(param.IdVal);
          if (c != null) StatsChannels.Add(new Stats.StatChannel { id = c.Id, name = c.Name });
        } catch (Exception ex) {
          Utils.Log("Error in reading channels from Setup: " + param.IdVal + " " + ex.Message);
          if (forceCleanBad) {
            Database.Delete(param);
          }
        }
      }
    }
    if (StatsChannels.Count == 0) {
      // Check the basic 4 channels of UnitedPrograming, other servers will have nothing
      TryAddDefaultChannel(guild, 830904407540367441ul);
      TryAddDefaultChannel(guild, 830904726375628850ul);
      TryAddDefaultChannel(guild, 830921265648631878ul);
      TryAddDefaultChannel(guild, 830921315657449472ul);
    }
  }

  static void TryAddDefaultChannel(DiscordGuild guild, ulong id) {
    try {
      DiscordChannel c = guild.GetChannel(id);
      if (c != null) {
        SetupParam p = new SetupParam("StatsChannel", id);
        Database.Add(p);
        Params.Add(p);
        StatsChannels.Add(new Stats.StatChannel { id = id, name = c.Name });
        Utils.Log("Adding default Stats channel for United Programming: " + id);
      }
    } catch(Exception ex) {
      Utils.Log("Problems finding a channel with ID " + id + ": " + ex.Message);
    }
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
    Utils.LogUserCommand(ctx);
    command = command.ToLowerInvariant().Trim();
    switch (command) {
      case "trackingchannel": await TrackingChannel(ctx, null); break;
      case "botid": await GetIDs(ctx, true); break;
      case "serverid": await GetIDs(ctx, false); break;
      case "guildid": await GetIDs(ctx, false); break;
      case "listadminroles": await ListAdminRoles(ctx); break;
      case "addadminrole": await Utils.DeleteDelayed(30, ctx.Message, ctx.RespondAsync("Missing role to add parameter").Result); break;
      case "removeadminrole": await Utils.DeleteDelayed(30, ctx.Message, ctx.RespondAsync("Missing role to remove parameter").Result); break;
      case "liststatschannels": await ListStatChannels(ctx); break;
      case "addstatschannel": await Utils.DeleteDelayed(30, ctx.Message, ctx.RespondAsync("Missing channel to add parameter").Result); break;
      case "removestatschannel": await Utils.DeleteDelayed(30, ctx.Message, ctx.RespondAsync("Missing channel to remove parameter").Result); break;

      default:
        DiscordMessage answer = ctx.RespondAsync("Unknown setup command").Result;
        await Utils.DeleteDelayed(30, ctx.Message, answer);
        break;
    }
  }

  [Command("setup")]
  [Description("Configure the bot")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
  public async Task Setup(CommandContext ctx, string command, DiscordRole role) { // Command with role as parameter
    Utils.LogUserCommand(ctx);
    command = command.ToLowerInvariant().Trim();
    switch (command) {
      case "addadminrole": await AddRemoveAdminRoles(ctx, role, true); break;
      case "removeadminrole": await AddRemoveAdminRoles(ctx, role, false); break;

      default:
        DiscordMessage answer = ctx.RespondAsync("Unknown setup command").Result;
        await Utils.DeleteDelayed(30, ctx.Message, answer);
        break;
    }
  }

  [Command("setup")]
  [Description("Configure the bot")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
  public async Task Setup(CommandContext ctx, string command, DiscordChannel channel) { // Command with channel as parameter
    Utils.LogUserCommand(ctx);
    command = command.ToLowerInvariant().Trim();
    switch (command) {
      case "trackingchannel": await TrackingChannel(ctx, channel); break;
      case "addstatschannel": await AddRemoveStatChannel(ctx, channel, true); break;
      case "removestatschannel": await AddRemoveStatChannel(ctx, channel, false); break;

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
        SetupParam p = new SetupParam("TrackingChannel", channel.Id);
        Database.Add(p);
        Params.Add(p);
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

  Task ListAdminRoles(CommandContext ctx) {
    try   {
      string msg = "";
      if (AdminRoles == null || AdminRoles.Count == 0) { // Try to read again the guild
        LoadParams();
      }
      if (AdminRoles == null || AdminRoles.Count == 0) {
        msg = "No admin roles defined";
      } else {
        DiscordGuild guild = Utils.GetGuild();
        foreach (ulong id in AdminRoles) {
          DiscordRole r = guild.GetRole(id);
          if (r != null) msg += r.Mention + ", ";
        }
        msg = msg[0..^2];
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.ListAdminRoles", ex));
    }
  }

  Task AddRemoveAdminRoles(CommandContext ctx, DiscordRole role, bool add) {
    try {
      string msg = null;
      if (add) {
        foreach (var p in AdminRoles) if (p == role.Id) {
            msg = "The role " + role.Name + " is already an Admin role for the bot.";
            break;
          }
        if (msg == null) {
          SetupParam p = new SetupParam("AdminRole", role.Id);
          AdminRoles.Add(role.Id);
          Database.Add(p);
          Params.Add(p);
          Utils.Log("Added role " + role.Name + " as admin role");
          msg = "Role " + role.Name + " added as Admin Role";
        }
      } else {
        foreach (var p in Params) {
          if (p.Param == "AdminRole" && p.IdVal == role.Id) {
            Database.Delete(p);
            Params.Remove(p);
            AdminRoles.Remove(role.Id);
            msg = "Role " + role.Name + " removed from Admin Roles";
            Utils.Log("Removed role " + role.Name + " as admin role");
            break;
          }
        }
        if (msg == null) msg = "Role " + role.Name + " was not an Admin Role";
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.AddRemoveAdminRoles", ex));
    }
  }

  
  Task ListStatChannels(CommandContext ctx) {
    try   {
      string msg = "";
      if (StatsChannels == null || StatsChannels.Count == 0) { // Try to read again the guild
        LoadParams();
      }
      if (StatsChannels == null || StatsChannels.Count == 0) {
        msg = "No stat channels defined";
      } else {
        DiscordGuild guild = Utils.GetGuild();
        foreach (var sc in StatsChannels) {
          DiscordChannel c = guild.GetChannel(sc.id);
          if (c != null) msg += c.Mention + ", ";
        }
        msg = msg[0..^2];
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.ListStatChannels", ex));
    }
  }

  Task AddRemoveStatChannel(CommandContext ctx, DiscordChannel channel, bool add) {
    try {
      string msg = null;
      if (add) {
        foreach (var sc in StatsChannels) if (sc.id == channel.Id) {
            msg = "The channel " + channel.Name + " is already a stat channel.";
            break;
          }
        if (msg == null) {
          SetupParam p = new SetupParam("StatsChannel", channel.Id);
          StatsChannels.Add(new Stats.StatChannel { id = channel.Id, name = channel.Name });
          Database.Add(p);
          Params.Add(p);
          Utils.Log("Added channel " + channel.Name + " as stats channel");
          msg = "Channel " + channel.Name + " added as stats channel";
        }
      } else {
        foreach (var p in Params) {
          if (p.Param == "StatsChannel" && p.IdVal == channel.Id) {
            Database.Delete(p);
            Params.Remove(p);
            foreach (var sc in StatsChannels) 
              if (sc.id == channel.Id) {
                StatsChannels.Remove(sc);
                break;
              }
            msg = "Channel " + channel.Name + " removed from stats channel";
            Utils.Log("Removed channel " + channel.Name + " from stats channel");
            break;
          }
        }
        if (msg == null) msg = "Channel " + channel.Name + " was not a stats channel";
      }
      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.AddRemoveStatChannel", ex));
    }
  }


  //  public async Task Setup(CommandContext ctx, [Description("The user that posted the message to check")] DiscordMember member) { // Refactors the previous post, if it is code

  /*

  roles for commands
  ids for emojis
  ids for admins: 830901562960117780ul 830901743624650783ul 831050318171078718ul
  channels for stats: 
                830904407540367441ul, "Unity",
                830904726375628850ul, "CSharp",
                830921265648631878ul, "Help1",
                830921315657449472ul, "Help2",
  */

  static ulong GetIDParam(string param) {
    if (Params == null) return 0;
    foreach (SetupParam p in Params) {
      if (p.Param == param) return p.IdVal;
    }
    return 0; // not found
  }
}


