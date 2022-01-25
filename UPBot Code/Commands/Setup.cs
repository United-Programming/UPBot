using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class SetupModule : BaseCommandModule {
  private static Dictionary<ulong, DiscordGuild> Guilds = new Dictionary<ulong, DiscordGuild>();
  private static Dictionary<ulong, List<Config>> Configs = new Dictionary<ulong, List<Config>>();
  public static Dictionary<ulong, DiscordChannel> TrackChannels = new Dictionary<ulong, DiscordChannel>();
  public static Dictionary<ulong, List<ulong>> AdminRoles = new Dictionary<ulong, List<ulong>>();

  public static List<Stats.StatChannel> StatsChannels; // FIXME
  public static HashSet<string> RepSEmojis; // FIXME
  public static HashSet<ulong> RepIEmojis; // FIXME
  public static HashSet<string> FunSEmojis; // FIXME
  public static HashSet<ulong> FunIEmojis; // FIXME
  private readonly static Regex emjSnowflakeER = new Regex(@"(<:[a-z0-9_]+:[0-9]+>)", RegexOptions.IgnoreCase);

  public static DiscordGuild TryGetGuild(ulong id) {
    if (Guilds.ContainsKey(id)) return Guilds[id];

    while (Utils.GetClient() == null) Task.Delay(1000);
    while (Utils.GetClient().Guilds == null) Task.Delay(1000);
    while (Utils.GetClient().Guilds.Count == 0) Task.Delay(1000);
    IReadOnlyDictionary<ulong, DiscordGuild> cguilds = Utils.GetClient().Guilds;
    foreach (var guildId in cguilds.Keys) {
      if (!Guilds.ContainsKey(guildId)) Guilds[guildId] = cguilds[guildId];
    }
    if (Guilds.ContainsKey(id)) return Guilds[id];

    return null;
  }

  internal static void LoadParams(bool forceCleanBad = false) { // FIXME this ahs to be server specific
    List<Config> dbconfig = Database.GetAll<Config>();
    foreach (var c in dbconfig) {
      if (!Configs.ContainsKey(c.Guild)) Configs[c.Guild] = new List<Config>();
      Configs[c.Guild].Add(c);

      // Guilds
      if (!Guilds.ContainsKey(c.Guild)) {
        if (TryGetGuild(c.Guild)==null) continue; // Guild is missing
      }

      // Admin roles
      if (c.IsParam(Config.ParamType.AdminRole)) {
        if (!AdminRoles.ContainsKey(c.Guild)) AdminRoles[c.Guild] = new List<ulong>();
        AdminRoles[c.Guild].Add(c.IdVal);
      }

      // Tracking channels
      if (c.IsParam(Config.ParamType.TrackingChannel)) {
        if (!TrackChannels.ContainsKey(c.Guild)) {
          DiscordChannel ch =  Guilds[c.Guild].GetChannel(c.IdVal);
          if (ch != null) TrackChannels[c.Guild] = ch;
        }
      }
    }


    Utils.Log("Params fully loaded. " + Configs.Count + " Discord servers found");
  }

  internal static DiscordChannel GetTrackChannel(ulong id) {
    if (TrackChannels.ContainsKey(id)) return TrackChannels[id];
    return null;
  }

  internal static bool IsAdminRole(ulong guild, ulong role) {
    if(!AdminRoles.ContainsKey(guild)) return false;
    return AdminRoles[guild].Contains(role);
  }

  /* RECYCLE **************************************************
static void old() { }
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
 // Rep and Fun Emojis
 RepSEmojis = new HashSet<string>();
 RepIEmojis = new HashSet<ulong>();
 FunSEmojis = new HashSet<string>();
 FunIEmojis = new HashSet<ulong>();
 foreach (var param in Params) {
   if (param.Param == "RepEmoji") {
     if (param.IdVal == 0) RepSEmojis.Add(param.StrVal);
     else RepIEmojis.Add(param.IdVal);
   }
   if (param.Param == "FunEmoji") {
     if (param.IdVal == 0) FunSEmojis.Add(param.StrVal);
     else FunIEmojis.Add(param.IdVal);
   }
 }
 if (RepIEmojis.Count == 0 && RepSEmojis.Count == 0) { // Add defaults
   RepIEmojis.Add(830907665869570088ul); // :OK:
   RepIEmojis.Add(840702597216337990ul); // :whatthisguysaid:
   RepIEmojis.Add(552147917876625419ul); // :thoose:
   RepSEmojis.Add("👍"); // :thumbsup:
   RepSEmojis.Add("❤️"); // :hearth:
   RepSEmojis.Add("🥰"); // :hearth:
   RepSEmojis.Add("😍"); // :hearth:
   RepSEmojis.Add("🤩"); // :hearth:
   RepSEmojis.Add("😘"); // :hearth:
   RepSEmojis.Add("💯"); // :100:
 }

 if (FunIEmojis.Count == 0 && FunSEmojis.Count == 0) { // Add defaults
   FunIEmojis.Add(830907626928996454ul); // :StrongSmile: 
   FunSEmojis.Add("😀");
   FunSEmojis.Add("😃");
   FunSEmojis.Add("😄");
   FunSEmojis.Add("😁");
   FunSEmojis.Add("😆");
   FunSEmojis.Add("😅");
   FunSEmojis.Add("🤣");
   FunSEmojis.Add("😂");
   FunSEmojis.Add("🙂");
   FunSEmojis.Add("🙃");
   FunSEmojis.Add("😉");
   FunSEmojis.Add("😊");
   FunSEmojis.Add("😇");
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
   "**ListEmojiReputation** - to list all emojis for Reputation tracking.\n" +
   "**ListEmojiFun** - to list all emojis for Fun tracking.\n" +
   "**AddEmojiReputation** _emoji_ - to add an emoji for Reputation tracking.\n" +
   "**AddEmojiFun** - _emoji_ - to add an emoji for Fun tracking.\n" +
   "**RemoveEmojiReputation** _emoji_ - to remove an emoji for Reputation tracking.\n" +
   "**RemoveEmojiFun** - _emoji_ - to remove an emoji for Fun tracking.\n" +
   "**ListStatsChannels** - to list all channels used for stats.\n" +
   "**AddStatsChannel** _<#channel>_ - adds a channel to the channels used for stats.\n" +
   "**RemoveStatsChannel** _<#channel>_ - removes the channel from the channels used for stats.";

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
   case "listemojireputation": await ListEmojiAppreciation(ctx, true); break;
   case "listemojifun": await ListEmojiAppreciation(ctx, false); break;

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

[Command("setup")]
[Description("Configure the bot")]
[RequireRoles(RoleCheckMode.Any, "Mod", "helper", "Owner", "Admin", "Moderator")] // Restrict access to users with a high level role
public async Task Setup(CommandContext ctx, string command, string msg) { // Command with string as parameter
 Utils.LogUserCommand(ctx);
 command = command.ToLowerInvariant().Trim();
 switch (command) {
   case "addemojireputation": await AddRemoveEmojiAppreciation(ctx, true, true); break;
   case "removeemojireputation": await AddRemoveEmojiAppreciation(ctx, true, false); break;
   case "addemojifun": await AddRemoveEmojiAppreciation(ctx, false, true); break;
   case "removeemojifun": await AddRemoveEmojiAppreciation(ctx, false, false); break;

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

Task ListEmojiAppreciation(CommandContext ctx, bool rep) {
 try {
   string msg = "";
   if (rep) {
     if (RepIEmojis.Count == 0 && RepSEmojis.Count == 0) msg = "No emojis for reputation are defined";
     else {
       msg = "Emojis for reputation: ";
       foreach (string emj in RepSEmojis) msg += emj;
       foreach (ulong emj in RepIEmojis) msg += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj));
     }
   }
   else {
     if (FunIEmojis.Count == 0 && FunSEmojis.Count == 0) msg = "No emojis for fun are defined";
     else {
       msg = "Emojis for fun: ";
       foreach (string emj in FunSEmojis) msg += emj;
       foreach (ulong emj in FunIEmojis) msg += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj));
     }
   }
   if (StatsChannels == null || StatsChannels.Count == 0) { // Try to read again the guild
     LoadParams();
   }
   DiscordMessage answer = ctx.RespondAsync(msg).Result;
   return Utils.DeleteDelayed(30, ctx.Message, answer);
 } catch (Exception ex) {
   return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.ListEmojiAppreciation", ex));
 }
}

Task AddRemoveEmojiAppreciation(CommandContext ctx, bool rep, bool add) {
 try {
   string[] contentParts = ctx.Message.Content.Split(' ');
   // Get the 3rd that is not empty
   string content = null;
   int num = 0;
   foreach (string part in contentParts) {
     if (!string.IsNullOrEmpty(part)) {
       num++;
       if (num == 3) { content = part; break; }
     }
   }
   string msg = null;
   // Do we have an emoji snoflake id?
   Match match = emjSnowflakeER.Match(content);
   if (match.Success) {
     content = match.Groups[1].Value;
     if (add) {
       if (rep) {
         if (RepSEmojis.Contains(content)) msg = "Emoji " + content + " already in the reputation list";
         else {

//              WeakReference should get the ulong for the emoji here!!!! Not the string!!!!!

           SetupParam p = new SetupParam("RepEmoji", content);
           Params.Add(p);
           Database.Add(p);
           RepSEmojis.Add(content);
           msg = "Emoji " + content + " added to the reputation list";
         }

       } else {
         if (FunSEmojis.Contains(content)) msg = "Emoji " + content + " already in the fun list";
         else {
           SetupParam p = new SetupParam("FunEmoji", content);
           Params.Add(p);
           Database.Add(p);
           FunSEmojis.Add(content);
           msg = "Emoji " + content + " added to the fun list";
         }
       }
     } else { // Remove
       string t = (rep ? "RepEmoji" : "FunEmoji");
         foreach (var p in Params) {
         if (p.Param == t && p.StrVal == content) {
           Params.Remove(p);
           Database.Delete(p);
           if (rep) {
             RepSEmojis.Remove(content);
             msg = "Emoji " + content + " removed to the reputation list";
           } else {
             FunSEmojis.Remove(content);
             msg = "Emoji " + content + " removed to the fun list";
           }
           break;
         }
       }
     }
   }
   else { // Grab the very first unicode emoji we can find
     for (int i = 0; i < content.Length - 1; i++) {
       if (char.IsSurrogate(content[i]) && char.IsSurrogatePair(content[i], content[i + 1])) {
         int codePoint = char.ConvertToUtf32(content[i], content[i + 1]);
         content = "" + content[i] + content[i + 1];
         break;
       }
     }
   }


   DiscordMessage answer = ctx.RespondAsync(msg).Result;
   return Utils.DeleteDelayed(30, ctx.Message, answer);

 } catch (Exception ex) {
   return ctx.RespondAsync(Utils.GenerateErrorAnswer("Setup.AddRemoveStatChannel", ex));
 }
}


//  public async Task Setup(CommandContext ctx, [Description("The user that posted the message to check")] DiscordMember member) { // Refactors the previous post, if it is code


roles for commands
ids for emojis
ids for admins: 830901562960117780ul 830901743624650783ul 831050318171078718ul
channels for stats: 
             830904407540367441ul, "Unity",
             830904726375628850ul, "CSharp",
             830921265648631878ul, "Help1",
             830921315657449472ul, "Help2",

static ulong GetIDParam(string param) {
 if (Params == null) return 0;
 foreach (SetupParam p in Params) {
   if (p.Param == param) return p.IdVal;
 }
 return 0; // not found
}
*/
}


