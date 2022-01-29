using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class SetupModule : BaseCommandModule {
  private static Dictionary<ulong, DiscordGuild> Guilds = new Dictionary<ulong, DiscordGuild>();
  private static Dictionary<ulong, List<Config>> Configs = new Dictionary<ulong, List<Config>>();
  public static Dictionary<ulong, TrackChannel> TrackChannels = new Dictionary<ulong, TrackChannel>();
  public static Dictionary<ulong, List<ulong>> AdminRoles = new Dictionary<ulong, List<ulong>>();
  public static Dictionary<ulong, ulong> SpamProtection = new Dictionary<ulong, ulong>();
  public static Dictionary<ulong, List<string>> BannedWords = new Dictionary<ulong, List<string>>();

  public static HashSet<string> RepSEmojis; // FIXME
  public static HashSet<ulong> RepIEmojis; // FIXME
  public static HashSet<string> FunSEmojis; // FIXME
  public static HashSet<ulong> FunIEmojis; // FIXME
  private readonly static Regex emjSnowflakeER = new Regex(@"(<:[a-z0-9_]+:[0-9]+>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
  private readonly Regex roleParser = new Regex("<@[^0-9]+([0-9]*)>", RegexOptions.Compiled);

  public static DiscordGuild TryGetGuild(ulong id) {
    if (Guilds.ContainsKey(id)) return Guilds[id];

    Task.Delay(1000);
    int t = 0;
    while (Utils.GetClient() == null) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (no client)"); }
    t = 0;
    while (Utils.GetClient().Guilds == null) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (no guilds)"); }

    while (Utils.GetClient().Guilds.Count == 0) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (guilds count is zero"); }

    IReadOnlyDictionary<ulong, DiscordGuild> cguilds = Utils.GetClient().Guilds;
    foreach (var guildId in cguilds.Keys) {
      if (!Guilds.ContainsKey(guildId)) Guilds[guildId] = cguilds[guildId];
    }
    if (Guilds.ContainsKey(id)) return Guilds[id];

    return null;
  }

  internal static bool Permitted(ulong guild, Config.ParamType t, IEnumerable<DiscordRole> roles) {
    if (!Configs.ContainsKey(guild)) return t == Config.ParamType.Ping; // Only ping is available by default
    List<Config> cfgs = Configs[guild];
    Config.ConfVal cv = GetConfigValue(guild, t);
    switch (cv) {
      case Config.ConfVal.NotAllowed: return false;
      case Config.ConfVal.Everybody: return true;
      case Config.ConfVal.OnlyAdmins:
        foreach (var role in roles) {
          if (IsAdminRole(guild, role)) return true;
        }
        break;
    }
    return t == Config.ParamType.Ping; // Only ping is available by default
  }


  internal static void LoadParams(bool forceCleanBad = false) {
    List<Config> dbconfig = Database.GetAll<Config>();
    foreach (var c in dbconfig) {
      ulong gid = c.Guild;

      if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config>();
      Configs[gid].Add(c);

      // Guilds
      if (!Guilds.ContainsKey(gid)) {
        if (TryGetGuild(gid) ==null) continue; // Guild is missing
      }

      // Admin roles
      if (c.IsParam(Config.ParamType.AdminRole)) {
        if (!AdminRoles.ContainsKey(gid)) AdminRoles[gid] = new List<ulong>();
        AdminRoles[gid].Add(c.IdVal);
      }

      // Tracking channels
      if (c.IsParam(Config.ParamType.TrackingChannel)) {
        if (!TrackChannels.ContainsKey(gid)) {
          DiscordChannel ch =  Guilds[gid].GetChannel(c.IdVal);
          if (ch != null) {
            if (!TrackChannels.ContainsKey(gid) || TrackChannels[gid] == null) TrackChannels[gid] = new TrackChannel();
            TrackChannels[gid].channel = ch;
            TrackChannels[gid].trackJoin = c.StrVal != null && c.StrVal.Length > 0 && c.StrVal[0] != '0';
            TrackChannels[gid].trackLeave = c.StrVal != null && c.StrVal.Length > 1 && c.StrVal[1] != '0';
            TrackChannels[gid].trackRoles = c.StrVal != null && c.StrVal.Length > 2 && c.StrVal[2] != '0';
            TrackChannels[gid].config = c;
          }
        }
      }

      // Spam Protection
      if (c.IsParam(Config.ParamType.SpamProtection)) {
        SpamProtection[gid] = c.IdVal;
      }
    }

    // Banned Words
    List<BannedWord> words = Database.GetAll<BannedWord>();
    Utils.Log("Found " + words.Count + " banned words from all servers");
    foreach (BannedWord word in words) {
      ulong gid = word.Guild;
      if (!BannedWords.ContainsKey(gid)) BannedWords[gid] = new List<string>();
      BannedWords[gid].Add(word.Word);
    }
    foreach (var bwords in BannedWords.Values)
      bwords.Sort((a, b) => { return a.CompareTo(b); });

    Utils.Log("Params fully loaded. " + Configs.Count + " Discord servers found");
  }

  internal static TrackChannel GetTrackChannel(ulong id) {
    if (TrackChannels.ContainsKey(id)) return TrackChannels[id];
    return null;
  }

  internal static bool IsAdminRole(ulong guild, DiscordRole role) {
    if (role.Position == 0 || role.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)) return true;
    if(!AdminRoles.ContainsKey(guild)) return false;
    return AdminRoles[guild].Contains(role.Id);
  }

  private void TryAddRole(ulong gid, ulong rid) {
    if (!AdminRoles.ContainsKey(gid)) AdminRoles[gid] = new List<ulong>();
    if (AdminRoles[gid].Contains(rid)) return;
    Config c = new Config(gid, Config.ParamType.AdminRole, rid);
    AdminRoles[gid].Add(rid);
    if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config>();
    Configs[gid].Add(c);
    Database.Add(c);
  }

  private void TryRemoveRole(ulong gid, ulong rid) {
    if (!AdminRoles.ContainsKey(gid)) return;
    if (!AdminRoles[gid].Contains(rid)) return;
    AdminRoles[gid].Remove(rid);
    if (!Configs.ContainsKey(gid)) return;

    long key = Config.TheKey(gid, Config.ParamType.AdminRole, rid);
    foreach (Config c in Configs[gid]) 
      if (c.ConfigKey == key) {
        Configs[gid].Remove(c);
        break;
      }
    Database.DeleteByKey<Config>(key);
  }

  readonly DiscordComponentEmoji ey = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✅"));
  readonly DiscordComponentEmoji en = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❎"));
  readonly DiscordComponentEmoji el = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↖️"));
  readonly DiscordComponentEmoji er = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↘️"));
  readonly DiscordComponentEmoji ec = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❌"));
  DiscordComponentEmoji ok = null;
  DiscordComponentEmoji ko = null;

  /* RECYCLE **************************************************

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


  /**************************** Interaction *********************************/
  [Command("Setup")]
  [Description("Configration of the bot (interactive if without parameters)")]
  public async Task SetupCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    ulong gid = ctx.Guild.Id;
    var interact = ctx.Client.GetInteractivity();
    if (ok == null) {
      ok = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.OK));
      ko = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.KO));
    }

    // Basic intro message
    var msg = CreateMainConfigPage(ctx, null);
    var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
    var ir = result.Result;

    while (ir != null && ir.Id != "idexitconfig") {
      ir.Handled = true;
      
      if (ir.Id == "idback") { // ******************************************************************** Back *************************************************************************
        msg = CreateMainConfigPage(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "iddefineadmins") { // ***************************************************** DefAdmins ***********************************************************************************
        msg = CreateAdminsInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idaddrole") { // *********************************************************** AddRole *******************************************************************************
        await ctx.Channel.DeleteMessageAsync(msg);
        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", please mention the role to add (_type anything else to close_)");
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(2));
        if (answer.Result != null && answer.Result.MentionedRoles.Count > 0) {
          foreach (DiscordRole r in answer.Result.MentionedRoles) {
            TryAddRole(gid, r.Id);
          }
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateAdminsInteraction(ctx, null);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idremrole") { // *********************************************************** RemRole *******************************************************************************
        await ctx.Channel.DeleteMessageAsync(msg);
        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", please mention the role to remove (_type anything else to close_)");
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(2));
        if (answer.Result != null && answer.Result.MentionedRoles.Count > 0) {
          foreach (DiscordRole r in answer.Result.MentionedRoles) {
            TryRemoveRole(gid, r.Id);
          }
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateAdminsInteraction(ctx, null);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "iddefinetracking") { // ************************************************************ DefTracking **************************************************************************
        msg = CreateTrackingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idchangetrackch") { // ************************************************************ Change Tracking ************************************************************************
        await ctx.Channel.DeleteMessageAsync(msg);
        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", please mention the channel (_use: **#**_) as tracking channel\nType _remove_ to remove the tracking channel");
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id && (dm.MentionedChannels.Count > 0 || dm.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase)));
        }, TimeSpan.FromMinutes(2));
        if (answer.Result == null || (answer.Result.MentionedChannels.Count == 0 && !answer.Result.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase))) {
          await ir.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Config timed out"));
          return;
        }

        if (answer.Result.MentionedChannels.Count > 0) {
          TryAddChannel(answer.Result.MentionedChannels[0]);
        } else if (answer.Result.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase)) {
          TryRemoveChannel(gid);
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateTrackingInteraction(ctx, null);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idremtrackch") { // ************************************************************ Remove Tracking ************************************************************************
        TryRemoveChannel(gid);

        msg = CreateTrackingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idaltertrackjoin") { // ************************************************************ Alter Tracking Join ************************************************************************
        AlterTracking(gid, true, false, false);

        msg = CreateTrackingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idaltertrackleave") { // ************************************************************ Alter Tracking Leave ************************************************************************
        AlterTracking(gid, false, true, false);

        msg = CreateTrackingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idaltertrackroles") { // ************************************************************ Alter Tracking Roles ************************************************************************
        AlterTracking(gid, false, false, true);

        msg = CreateTrackingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idconfigfeats") { // *************************************************************** ConfigFeats ***********************************************************************
        msg = CreateFeaturesInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;


      } else if (ir.Id == "idfeatping" || ir.Id == "idfeatping0" || ir.Id == "idfeatping1" || ir.Id == "idfeatping2") { // *********** Config Ping ***********************************************************************
        if (ir.Id == "idfeatping0") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatping1") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatping2") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.Everybody);
        msg = CreatePingInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatwhois" || ir.Id == "idfeatwhois0" || ir.Id == "idfeatwhois1" || ir.Id == "idfeatwhois2") { // ********* Config WhoIs ***********************************************************************
        if (ir.Id == "idfeatwhois0") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatwhois1") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatwhois2") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.Everybody);
        msg = CreateWhoIsInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatmassdel" || ir.Id == "idfeatmassdel0" || ir.Id == "idfeatmassdel1" || ir.Id == "idfeatmassdel2") { // ********* Config MassDel ***********************************************************************
        if (ir.Id == "idfeatmassdel0") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatmassdel1") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatmassdel2") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.Everybody);
        msg = CreateMassDelInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatgames" || ir.Id == "idfeatgames0" || ir.Id == "idfeatgames1" || ir.Id == "idfeatgames2") { // ********* Config Games ***********************************************************************
        if (ir.Id == "idfeatgames0") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatgames1") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatgames2") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.Everybody);
        msg = CreateGamesInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatrefactor" || ir.Id == "idfeatrefactor0" || ir.Id == "idfeatrefactor1" || ir.Id == "idfeatrefactor2") { // ********* Config Refactor ***********************************************************************
        if (ir.Id == "idfeatrefactor0") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatrefactor1") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatrefactor2") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.Everybody);
        msg = CreateRefactorInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatreunitydocs" || ir.Id == "idfeatreunitydocs0" || ir.Id == "idfeatreunitydocs1" || ir.Id == "idfeatreunitydocs2") { // ********* Config Unity Docs ***********************************************************************
        if (ir.Id == "idfeatreunitydocs0") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatreunitydocs1") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatreunitydocs2") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.Everybody);
        msg = CreateUnityDocsInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatrespamprotect" || // ********* Config Spam Protection ***********************************************************************
        ir.Id == "idfeatrespamprotect0" || ir.Id == "idfeatrespamprotect1" || ir.Id == "idfeatrespamprotect2") {
        Config c = GetConfig(gid, Config.ParamType.SpamProtection);
        ulong val = 0;
        if (c != null) val = c.IdVal;
        ulong old = val;
        if (ir.Id == "idfeatrespamprotect0") val ^= 1ul;
        if (ir.Id == "idfeatrespamprotect1") val ^= 2ul;
        if (ir.Id == "idfeatrespamprotect2") val ^= 4ul;
        if (val != old) {
          if (c == null) {
            c = new Config(gid, Config.ParamType.SpamProtection, val);
            if (!Configs.ContainsKey(gid)) Configs[gid]=new List<Config>();
            Configs[gid].Add(c);
          } else c.IdVal = val;
          Database.Add(c);
          SpamProtection[gid] = val;
        }
        msg = CreateSpamProtectInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeattz" || ir.Id == "idfeattzs0" || ir.Id == "idfeattzs1" || ir.Id == "idfeattzs2" || ir.Id == "idfeattzg0" || ir.Id == "idfeattzg1" || ir.Id == "idfeattzg2") { 
        // ********* Config Timezones ***********************************************************************
        if (ir.Id == "idfeattzs0") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeattzs1") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeattzs2") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.Everybody);
        if (ir.Id == "idfeattzg0") { SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.NotAllowed); SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.NotAllowed); }
        if (ir.Id == "idfeattzg1") SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeattzg2") SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.Everybody);
        msg = CreateTimezoneInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id == "idfeatstats" || ir.Id == "idfeatstats0" || ir.Id == "idfeatstats1" || ir.Id == "idfeatstats2") { // ********* Config Stats ***********************************************************************
        if (ir.Id == "idfeatstats0") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.NotAllowed);
        if (ir.Id == "idfeatstats1") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.OnlyAdmins);
        if (ir.Id == "idfeatstats2") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.Everybody);
        msg = CreateStatsInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else if (ir.Id.Length > 12 && ir.Id[0..13] == "idfeatbannedw") { // ********* Config Banned Words ***********************************************************************
        if (ir.Id == "idfeatbannedwed") {
          if (GetConfigValue(gid, Config.ParamType.BannedWords) == Config.ConfVal.NotAllowed) SetConfigValue(gid, Config.ParamType.BannedWords, Config.ConfVal.Everybody);
          else SetConfigValue(gid, Config.ParamType.BannedWords, Config.ConfVal.NotAllowed);
        } else if (ir.Id == "idfeatbannedwadd") {
          await ctx.Channel.DeleteMessageAsync(msg);
          msg = null;
          DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", type the word to be banned (at least 4 letters), you have 1 minute before timeout");
          var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
          }, TimeSpan.FromMinutes(1));
          if (answer.Result == null || answer.Result.Content.Length < 4) {
            _ = Utils.DeleteDelayed(10, ctx.Channel.SendMessageAsync("Config timed out"));
          }
          else { // Is it already here? (avoid duplicates)
            string bw = answer.Result.Content.ToLowerInvariant().Trim();
            if (!BannedWords.ContainsKey(gid)) BannedWords[gid] = new List<string>();
            if (BannedWords[gid].Contains(bw)) {
              _ = Utils.DeleteDelayed(10, ctx.Channel.SendMessageAsync("The word is already there"));
            }
            else {
              BannedWords[gid].Add(bw);
              Database.Add(new BannedWord(gid, bw));
            }
          }
          await ctx.Channel.DeleteMessageAsync(prompt);

        } else if (ir.Id.Length > 13 && ir.Id[0..14] == "idfeatbannedwr") {
          // Get the word by number, remove it. In case there are no more disable the feature
          int.TryParse(ir.Id[13..], out int num);
          Database.DeleteByKey<BannedWord>(BannedWord.GetTheKey(gid, BannedWords[gid][num]));
          BannedWords[gid].RemoveAt(num);
        }
        msg = CreateBannedWordsInteraction(ctx, msg);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;

      } else {
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
        ir = result.Result;
      }
    }
    if (ir == null) await ctx.Channel.DeleteMessageAsync(msg); // Expired
    else await ir.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Config completed"));
  }


  [Command("Setup")]
  [Description("Configration of the bot (interactive if without parameters)")]
  public async Task SetupCommand(CommandContext ctx, [RemainingText] [Description("The setup command to execute")]string command) {
    Utils.LogUserCommand(ctx);
    DiscordGuild g = ctx.Guild;
    ulong gid = g.Id;
    string[] cmds = command.Trim().ToLowerInvariant().Split(' ');

    // ****************** LIST *********************************************************************************************************************************************
    if (cmds[0].Equals("list") || cmds[0].Equals("dump")) {
      // list

      string msg = "Setup list for Discord Server " + ctx.Guild.Name + "\n";
      string part = "";
      // Admins ******************************************************
      if(!AdminRoles.ContainsKey(gid)) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
      else {
        foreach (var rid in AdminRoles[gid]) {
          DiscordRole r = g.GetRole(rid);
          if (r != null) part += r.Name + ", ";
        }
        if (part.Length == 0) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
        else msg += "**AdminRoles**: " + part[0..^2] + "\n";
      }

      // TrackingChannel ******************************************************
      if(!TrackChannels.ContainsKey(gid)) msg += "**TrackingChannel**: _no tracking channel defined_\n";
      else {
        msg += "**TrackingChannel**: " + TrackChannels[gid].channel.Mention + " for ";
        if (TrackChannels[gid].trackJoin || TrackChannels[gid].trackLeave || TrackChannels[gid].trackRoles) {
          if (TrackChannels[gid].trackJoin) msg += "_Join_ ";
          if (TrackChannels[gid].trackLeave) msg += "_Leave_ ";
          if (TrackChannels[gid].trackRoles) msg += "_Roles_ ";
        } else msg += "nothing";
        msg += "\n";
      }

      // Ping ******************************************************
      Config cfg = GetConfig(gid, Config.ParamType.Ping);
      if (cfg == null) msg += "**Ping**: _not defined (allowed to all by default)_\n";
      else msg += "**Ping**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // WhoIs ******************************************************
      cfg = GetConfig(gid, Config.ParamType.WhoIs);
      if (cfg == null) msg += "**WhoIs**: _not defined (disabled by default)_\n";
      else msg += "**WhoIs**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // MassDel ******************************************************
      cfg = GetConfig(gid, Config.ParamType.MassDel);
      if (cfg == null) msg += "**Mass Delete**: _not defined (disabled by default)_\n";
      else msg += "**Mass Delete**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // Games ******************************************************
      cfg = GetConfig(gid, Config.ParamType.Games);
      if (cfg == null) msg += "**Games**: _not defined (disabled by default)_\n";
      else msg += "**Games**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // Refactor ******************************************************
      cfg = GetConfig(gid, Config.ParamType.Refactor);
      if (cfg == null) msg += "**Code Refactor**: _not defined (disabled by default)_\n";
      else msg += "**Code Refactor**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // Timezones ******************************************************
      cfg = GetConfig(gid, Config.ParamType.TimezoneS);
      Config cfg2 = GetConfig(gid, Config.ParamType.TimezoneG);
      if (cfg == null || cfg2 == null) msg += "**Timezones**: _not defined (disabled by default)_\n";
      else msg += "**Timezones**: Set = " + (Config.ConfVal)cfg.IdVal + " Read = " + (Config.ConfVal)cfg2.IdVal + "\n";

      // UnityDocs ******************************************************
      cfg = GetConfig(gid, Config.ParamType.UnityDocs);
      if (cfg == null) msg += "**Unity Docs**: _not defined (disabled by default)_\n";
      else msg += "**Unity Docs**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // SpamProtection ******************************************************
      cfg = GetConfig(gid, Config.ParamType.SpamProtection);
      if (cfg == null) msg += "**Spam Protection**: _not defined (disabled by default)_\n";
      else if (cfg.IdVal == 0) msg += "**Spam Protection**: _disabled_\n";
      else msg += "**Spam Protection**: enabled for" +
          ((cfg.IdVal & 1ul) == 1 ? " _Discord_" : "") +
          ((cfg.IdVal & 2ul) == 2 ? ((cfg.IdVal & 1ul) == 1 ? ", _Steam_" : " _Steam_") : "") +
          ((cfg.IdVal & 4ul) == 4 ? ((cfg.IdVal & 1ul) != 0 ? ",  _Epic Game Store_" : " _Epic Game Store_") : "") +
          "\n";

      // Stats ******************************************************
      cfg = GetConfig(gid, Config.ParamType.Stats);
      if (cfg == null) msg += "**Stats**: _not defined (disabled by default)_\n";
      else msg += "**Stats**: " + (Config.ConfVal)cfg.IdVal + "\n";

      // Banned Words ******************************************************
      cfg = GetConfig(gid, Config.ParamType.BannedWords);
      if (cfg == null) msg += "**BannedWords**: _not defined (disabled by default)_\n";
      else {
        if (!BannedWords.ContainsKey(gid) || BannedWords[gid].Count==0) msg += "**BannedWords**: _disabled_ (no words defined)\n";
        else {
          string bws = "";
          foreach (var w in BannedWords[gid]) bws += w + ", ";
          msg += "**BannedWords**: " + bws[0..^2] + ".\n";
        }
      }


      await Utils.DeleteDelayed(60, ctx.RespondAsync(msg));
    }

    // ****************** PING *********************************************************************************************************************************************
    if (cmds[0].Equals("ping") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.Ping);
      if (c == null) {
        c = new Config(gid, Config.ParamType.Ping, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("Ping command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** WHOIS *********************************************************************************************************************************************
    if (cmds[0].Equals("whois") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.WhoIs);
      if (c == null) {
        c = new Config(gid, Config.ParamType.WhoIs, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("WhoIs command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** MASSDEL *********************************************************************************************************************************************
    if (cmds[0].Equals("massdel") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.MassDel);
      if (c == null) {
        c = new Config(gid, Config.ParamType.MassDel, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("MassDel command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** GAMES *********************************************************************************************************************************************
    if (cmds[0].Equals("games") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.Games);
      if (c == null) {
        c = new Config(gid, Config.ParamType.Games, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("Games command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** REFACTOR *********************************************************************************************************************************************
    if (cmds[0].Equals("refactor") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.Refactor);
      if (c == null) {
        c = new Config(gid, Config.ParamType.Refactor, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("Code Refactor command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** UNITYDOCS *********************************************************************************************************************************************
    if (cmds[0].Equals("unitydocs") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.UnityDocs);
      if (c == null) {
        c = new Config(gid, Config.ParamType.UnityDocs, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> {c};
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("UnityDocs command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** TIMEZONES *********************************************************************************************************************************************
    if (cmds[0].Equals("timezones") && cmds.Length > 2) {
      // get|set who
      // who who
      char who = cmds[2][0];
      Config cg = GetConfig(gid, Config.ParamType.TimezoneG);
      Config cs = GetConfig(gid, Config.ParamType.TimezoneS);
      if (cmds[1].Trim().Equals("get", StringComparison.InvariantCultureIgnoreCase)) {
        if (cg == null) {
          cg = new Config(gid, Config.ParamType.TimezoneG, 1);
          if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { cg };
        }
        if (who == 'n' || who == 'd') cg.IdVal = (int)Config.ConfVal.NotAllowed;
        if (who == 'a' || who == 'r' || who == 'o') cg.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (who == 'e' || who == 'y') cg.IdVal = (int)Config.ConfVal.Everybody;

      } else if (cmds[1].Trim().Equals("set", StringComparison.InvariantCultureIgnoreCase)) {
        if (cs == null) {
          cs = new Config(gid, Config.ParamType.TimezoneS, 1);
          if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { cs };
        }
        if (who == 'n' || who == 'd') cs.IdVal = (int)Config.ConfVal.NotAllowed;
        if (who == 'a' || who == 'r' || who == 'o') cs.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (who == 'e' || who == 'y') cs.IdVal = (int)Config.ConfVal.Everybody;

      } else {
        char whos = cmds[1][0];
        if (cg == null) {
          cg = new Config(gid, Config.ParamType.TimezoneG, 1);
          if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { cg };
        }
        if (who == 'n' || who == 'd') cg.IdVal = (int)Config.ConfVal.NotAllowed;
        if (who == 'a' || who == 'r' || who == 'o') cg.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (who == 'e' || who == 'y') cg.IdVal = (int)Config.ConfVal.Everybody;

        if (cs == null) {
          cs = new Config(gid, Config.ParamType.TimezoneS, 1);
          if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { cs };
        }
        if (whos == 'n' || whos == 'd') cs.IdVal = (int)Config.ConfVal.NotAllowed;
        if (whos == 'a' || whos == 'r' || whos == 'o') cs.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (whos == 'e' || whos == 'y') cs.IdVal = (int)Config.ConfVal.Everybody;
      }

      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("Timezones command changed to  SET = " + (Config.ConfVal)cs.IdVal + "  GET = " + (Config.ConfVal)cg.IdVal));
    }

    // ****************** TRACKINGCHANNEL *********************************************************************************************************************************************
    if (cmds[0].Equals("trackingchannel") && cmds.Length > 1) {
      Config c = GetConfig(gid, Config.ParamType.TrackingChannel);
      if (c == null) {
        c = new Config(gid, Config.ParamType.TrackingChannel, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { c };
      }

      // [channel]|remove j l r
      bool j = false;
      bool l = false;
      bool r = false;
      DiscordChannel forLater = null;
      for (int i = 1; i < cmds.Length; i++) {
        string part = cmds[i].ToLowerInvariant();
        if (part[0] == '#') part = part[1..];
        // Is it a channel?
        foreach (DiscordChannel ch in g.Channels.Values) {
          if (ch.Name.Equals(part, StringComparison.InvariantCultureIgnoreCase)) {
            c.IdVal = ch.Id;
            forLater = ch;
            break;
          }
        }
        if (part.Length > 2 && part[0..3] == "rem") { // remove
          c.IdVal = 0;
          c.StrVal = null;
          forLater = null;
          break;
        }
        if (part[0] == 'j' && c.IdVal != 0) j = true; // join
        if (part[0] == 'l' && c.IdVal != 0) l = true; // leave
        if (part[0] == 'r' && c.IdVal != 0) r = true; // roles
      }
      if (c.IdVal != 0) {
        c.StrVal = (j ? "1" : "0") + (l ? "1" : "0") + (r ? "1" : "0");
      }
      // Update the cache
      if (!TrackChannels.ContainsKey(gid)) {
        TrackChannels[gid] = new TrackChannel() { channel = forLater, config = c, trackJoin = j, trackLeave = l, trackRoles = r };
      } else {
        if (c.IdVal != 0 && forLater != null) TrackChannels[gid].channel = forLater;
        else if (c.IdVal == 0) TrackChannels[gid].channel = null;
        TrackChannels[gid].config = c;
        TrackChannels[gid].trackJoin = j;
        TrackChannels[gid].trackLeave = l;
        TrackChannels[gid].trackRoles = r;
      }

      _ = Utils.DeleteDelayed(15, ctx.Message);
      string msg;
      if (c.IdVal == 0) msg = "TrackingChannel command changed to _tracking channel removed_";
      else {
        msg = "TrackingChannel command changed to " + g.GetChannel(c.IdVal).Mention + " for ";
        if (!j && !l && !r) {
          msg += "_nothing_";
        }
        if (j) msg += "_Join_ ";
        if (l) msg += "_Leave_ ";
        if (r) msg += "_Roles_ ";
      }
      await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
    }

    // ****************** ADMINROLES *********************************************************************************************************************************************
    if (cmds[0].Equals("adminroles") && cmds.Length > 1) {
      var errs = "";
      // set them, or remove
      if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase)) {
        Config c = GetConfig(gid, Config.ParamType.AdminRole);
        while (c != null) {
          Configs[gid].Remove(c);
          Database.Delete(c);
          AdminRoles[gid].Remove(c.IdVal);
          c = GetConfig(gid, Config.ParamType.AdminRole);
        }
      } else {
        var guildRoles = g.Roles.Values;
        List<Config> confRoles = new List<Config>();
        foreach (var c in Configs[gid])
          if (c.IsParam(Config.ParamType.AdminRole)) {
            confRoles.Add(c);
            c.StrVal = "*";
          }
        for (int i = 1; i < cmds.Length; i++) {
          // Find if we have it (and the id)
          ulong id = 0;
          Match rm = roleParser.Match(cmds[i]);
          if (rm.Success)
            ulong.TryParse(rm.Groups[1].Value, out id);
          else {
            foreach (var r in guildRoles)
              if (r.Name.Equals(cmds[i], StringComparison.InvariantCultureIgnoreCase)) {
                id = r.Id;
                break;
              }
          }
          if (id == 0) {
            errs += cmds[i] + " ";
            continue;
          }
          // Do we have it in the Configs?
          bool notfound = true;
          foreach (var c in confRoles) {
            if (c.IdVal == id) {
              c.StrVal = null;
              notfound = false;
              break;
            }
          }
          if (notfound) { // Add it
            Config c = new Config(gid, Config.ParamType.AdminRole, id);
            confRoles.Add(c);
            Configs[gid].Add(c);
            Database.Add(c);
            AdminRoles[gid].Add(id);
          }
        }
        // Remove all configs that are not required
        for (int i = confRoles.Count - 1; i >= 0; i--) {
          if (confRoles[i].StrVal != null) {
            Config c = confRoles[i];
            confRoles.RemoveAt(i);
            Configs[gid].Remove(c);
            AdminRoles[gid].Remove(c.IdVal);
            Database.Delete(c);
          }
        }
      }
      // And show the result
      string msg;

      if (!AdminRoles.ContainsKey(gid) || AdminRoles[gid].Count == 0) msg = "**AdminRoles** removed";
      else {
        msg = "**AdminRoles** are: ";
        foreach (var rid in AdminRoles[gid]) {
          DiscordRole r = g.GetRole(rid);
          if (r != null) msg += r.Mention + ", ";
        }
        msg = msg[0..^2];
      }
      if (errs.Length > 0) msg += " _Errors:_ " + errs;

      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
    }

    // ****************** SPAMPROTECTION *********************************************************************************************************************************************
    if ((cmds[0].Equals("spam") || cmds[0].Equals("spamprotection")) && cmds.Length > 1) {
      bool edisc = false, esteam = false, eepic = false;
      for (int i = 1; i < cmds.Length; i++) {
        char mode = cmds[1][0];
        if (mode == 'd') edisc = true;
        if (mode == 's') esteam = true;
        if (mode == 'e') eepic = true;
      }
      ulong val = (edisc ? 1ul : 0) | (esteam ? 2ul : 0) | (eepic ? 4ul : 0);
      Config c = GetConfig(gid, Config.ParamType.SpamProtection);
      if (c == null) {
        c = new Config(gid, Config.ParamType.SpamProtection, val);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config>();
        Configs[gid].Add(c);
      } else c.IdVal = val;
      Database.Add(c);
      SpamProtection[gid] = val;

      string msg = "SpamProtection command changed to ";
      if (val == 0) msg += "_disabled_\n";
      else msg += "enabled for" +
          ((val & 1ul) == 1 ? " _Discord_" : "") +
          ((val & 2ul) == 2 ? ((val & 1ul) == 1 ? ", _Steam_" : " _Steam_") : "") +
          ((val & 4ul) == 4 ? ((val & 1ul) != 0 ? ",  _Epic Game Store_" : " _Epic Game Store_") : "");

      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
    }

    // ****************** STATS *********************************************************************************************************************************************
    if (cmds[0].Equals("stats") && cmds.Length > 1) {
      char mode = cmds[1][0];
      Config c = GetConfig(gid, Config.ParamType.Stats);
      if (c == null) {
        c = new Config(gid, Config.ParamType.Stats, 1);
        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config> { c };
      }
      if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
      if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
      if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
      _ = Utils.DeleteDelayed(15, ctx.Message);
      await Utils.DeleteDelayed(15, ctx.RespondAsync("Stats command changed to " + (Config.ConfVal)c.IdVal));
    }

    // ****************** BANNEDWORDS *********************************************************************************************************************************************
    if (cmds[0].Equals("bannedwords")) {
      if (cmds.Length > 1) {
        if (cmds[1].Equals("list", StringComparison.InvariantCultureIgnoreCase)) { // LIST ********************************************************************************************************************
          if (!BannedWords.ContainsKey(gid) || BannedWords[gid].Count == 0) await Utils.DeleteDelayed(15, ctx.RespondAsync("No banned words are defined"));
          string bws = "Banned Words: ";
          foreach (var w in BannedWords[gid]) bws += w + ", ";
          bws = bws[0..^2];
          if (GetConfigValue(gid, Config.ParamType.BannedWords) == Config.ConfVal.NotAllowed) bws += " (_disabled_)";
          else bws += " (_enabled_)";
          await Utils.DeleteDelayed(15, ctx.RespondAsync(bws));

        } else if (cmds[1].Equals("enable", StringComparison.InvariantCultureIgnoreCase)) { // ENABLE ******************************************************************************************
          SetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords, Config.ConfVal.Everybody);
          if (!BannedWords.ContainsKey(gid) || BannedWords[gid].Count == 0)
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Stats command changed to _enabled_ (but no banned words are deifned)"));
          else
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Stats command changed to _enabled_"));

        } else if (cmds[1].Equals("disable", StringComparison.InvariantCultureIgnoreCase)) { // DISABLE ******************************************************************************************
          SetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords, Config.ConfVal.NotAllowed);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Stats command changed to _disabled_"));

        } else if (cmds[1].Equals("add", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // ADD ******************************************************************************************
          string bw = cmds[2].ToLowerInvariant().Trim();
          if (!BannedWords.ContainsKey(gid)) BannedWords[gid] = new List<string>();
          if (BannedWords[gid].Contains(bw)) {
            await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is already there"));
          } else {
            BannedWords[gid].Add(bw);
            Database.Add(new BannedWord(gid, bw));
          }
          await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is added the the list of banned words"));

        } else if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // REMOVE *************************************************************************************************
          if (!BannedWords.ContainsKey(gid)) return;
          string bw = cmds[2].ToLowerInvariant().Trim();
          if (BannedWords[gid].Contains(bw)) {
            Database.DeleteByKey<BannedWord>(BannedWord.GetTheKey(gid, bw));
            BannedWords[gid].Remove(bw);
            await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is removed from the list of banned words"));
          } else {
            await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is not in the list of banned words"));
          }

        } else if (cmds[1].Equals("clear", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("clean", StringComparison.InvariantCultureIgnoreCase)) { // CLEAR *********************************************
          if (!BannedWords.ContainsKey(gid)) return;
          foreach (var bw in BannedWords[gid]) {
            Database.DeleteByKey<BannedWord>(BannedWord.GetTheKey(gid, bw));
          }
          BannedWords[gid].Clear();
        }
      }
      if (cmds.Length == 1 || cmds[1].Equals("?", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) { // HELP *******************************************
        await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `enable` or `disable` to change the status\n`add` _word_ to add a new word to ban\n`remove` _word_ to remove a word from the list\n`clear` to remove all words\n`list` to show all banned words."));
      }

    }

  }

  private void AlterTracking(ulong gid, bool j, bool l, bool r) {
    if (!TrackChannels.ContainsKey(gid)) return;
    TrackChannel tc = TrackChannels[gid];
    if (j) tc.trackJoin = !tc.trackJoin;
    if (l) tc.trackLeave = !tc.trackLeave;
    if (r) tc.trackRoles = !tc.trackRoles;
    tc.config.StrVal = (tc.trackJoin ? "1" : "0") + (tc.trackLeave ? "1" : "0") + (tc.trackRoles ? "1" : "0");
    Database.Update(tc.config);
  }

  private void TryRemoveChannel(ulong id) {
    if (TrackChannels.ContainsKey(id)) {
      Database.DeleteByKey<Config>(Config.TheKey(id, Config.ParamType.TrackingChannel, TrackChannels[id].channel.Id));
      TrackChannels.Remove(id);
    }
  }

  private void TryAddChannel(DiscordChannel ch) {
    TrackChannel tc;
    if (!TrackChannels.ContainsKey(ch.Guild.Id)) {
      tc = new TrackChannel();
      TrackChannels[ch.Guild.Id] = tc;
      tc.trackJoin = true;
      tc.trackLeave = true;
      tc.trackRoles = true;
    } else {
      Database.DeleteByKey<Config>(Config.TheKey(ch.Guild.Id, Config.ParamType.TrackingChannel, TrackChannels[ch.Guild.Id].channel.Id));
    }
    tc = TrackChannels[ch.Guild.Id];
    tc.channel = ch;
    Config c = new Config(ch.Guild.Id, Config.ParamType.TrackingChannel, ch.Id);
    c.StrVal = (tc.trackJoin ? "1" : "0") + (tc.trackLeave ? "1" : "0") + (tc.trackRoles ? "1" : "0");
    Database.Add(c);
  }


  private DiscordMessage CreateMainConfigPage(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    //- Set tracking
    //- Set Admins
    //- Enable features:
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefineadmins", "Define Admins", false, er));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefinetracking", "Define Tracking channel", false, er));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idconfigfeats", "Configure features", false, er));
    builder.AddComponents(actions);

    //-Exit
    builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateAdminsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Admin roles"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    string desc = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n" +
      "Current server roles that are considered bot administrators:\n";

    // List admin roles
    if (!AdminRoles.ContainsKey(ctx.Guild.Id)) desc += "_**No admin roles defined.** Owner and server Admins will be used_";
    else {
      List<ulong> roles = AdminRoles[ctx.Guild.Id];
      bool one = false;
      foreach (ulong role in roles) {
        DiscordRole dr = ctx.Guild.GetRole(role);
        if (dr != null) {
          desc += dr.Mention + ", ";
          one = true;
        }
      }
      if (one) desc = desc[0..^2];
      else desc += "_**No admin roles defined.** Owner and server Admins will be used_";
    }
    eb.Description = desc;
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());
    

    // - Add role
    // - Remove role
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaddrole", "Add role", false, ok));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idremrole", "Remove role", false, ko));
    builder.AddComponents(actions);
    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateTrackingInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    TrackChannel tc = TrackChannels.ContainsKey(ctx.Guild.Id) ? TrackChannels[ctx.Guild.Id] : null;

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Tracking channel"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    string desc = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n";
    if (tc == null) desc += "_**No tracking channel defined.**_";
    else {
      if (tc.channel == null) desc += "_**No tracking channel defined.**_";
      else desc += "_**Tracking channel:** " + tc.channel.Mention + "_";
    }
    eb.Description = desc;
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());


    // - Change channel
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idchangetrackch", "Change channel", false, ok));
    if (TrackChannels.ContainsKey(ctx.Guild.Id))
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idremtrackch", "Remove channel", false, ko));
    builder.AddComponents(actions);

    // - Actions to track
    if (tc != null) {
      actions = new List<DiscordButtonComponent>();
      if (tc.trackJoin) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackjoin", "Track Joint", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackjoin", "Track Joint", false, en));
      if (tc.trackLeave) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackleave", "Track Leave", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackleave", "Track Leave", false, en));
      if (tc.trackRoles) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackroles", "Track Roles", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackroles", "Track Roles", false, en));
      builder.AddComponents(actions);
    }

    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateFeaturesInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Features"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n" +
      "Select the feature to configure _(red ones are disabled, blue ones are enabled)_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());


    // ping
    // whois
    // mass delete
    // games
    // refactor code
    actions = new List<DiscordButtonComponent>();
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Ping);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatping", "Ping", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.WhoIs);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatwhois", "WhoIs", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.MassDel);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatmassdel", "Mass Delete", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Games);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatgames", "Games", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatrefactor", "Refactor Code", false, er));

    builder.AddComponents(actions);

    // timezones
    // unitydocs
    // Spam protection
    // Stats
    // Banned words
    actions = new List<DiscordButtonComponent>();
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneG);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeattz", "Timezone", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.UnityDocs);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatreunitydocs", "Unity Docs", false, er));
    Config sc = GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    actions.Add(new DiscordButtonComponent((sc == null || sc.IdVal == 0) ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Stats);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatstats0", "Stats", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatbannedw", "Banned Words", false, er));
    builder.AddComponents(actions);


    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }


  private DiscordMessage CreatePingInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Ping"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Ping);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **ping** command will just make the bot to asnwer when it is alive.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Ping** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Ping** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Ping** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatping0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatping1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatping2", "Everybody", false,   GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateWhoIsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - WhoIs"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.WhoIs);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **whois** command allows to see who an user is with some statistics.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**WhoIs** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**WhoIs** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**WhoIs** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatwhois0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatwhois1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatwhois2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateMassDelInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Mass Delete"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.MassDel);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **delete** command can mass remove a set of messages from a channel. It is recommended to limit it to admins.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Mass Delete** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Mass Delete** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Mass Delete** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatmassdel0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatmassdel1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatmassdel2", "Everybody (not recommended)", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateGamesInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Games"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Games);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot has some simple games that can be played. Games are _RPS_ (Rock,Paper, Scissors) and _bool_ that will just return randomly true or false.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Games** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Games** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Games** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatgames0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatgames1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatgames2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateStatsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Stats"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Stats);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot can produce some stats for the server.\n Stats can be gloabl or even checkign specific things in a channel, like used emojis, mentioned people, mentioned roles.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Stats** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Stats** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Stats** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatstats0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatstats1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatstats2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateUnityDocsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - UnityDocs"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **unitydocs** command allows to find the **Unity** online documentation (last LTS version.)\n" +
      "The best 3 links are proposed.\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**UnityDocs** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**UnityDocs** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**UnityDocs** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatreunitydocs0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatreunitydocs1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatreunitydocs2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateRefactorInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Refactor"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **refactor** command allows to convert some random code posted in a compact code block.\nIt works with C++, C#, Java, Javascript, and Python\n" +
      "You can reformat last posted code or you can mention the post to reformat. You can also _analyze_ the code to find the probable language.\n" +
      "And you can delete the original code post, in case was done by you. (_Admins_ can force the delete of the reformatted code)\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Refactor** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Refactor** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Refactor** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatrefactor0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatrefactor1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatrefactor2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateTimezoneInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Timezone"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cvs = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneS);
    Config.ConfVal cvg = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneG);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **timezone** command allows to specify timezones for the users and check the local time.\n" +
      "You can use `list` to have a list to all known timezones.\n" +
      "You can mention a user to see its time zone or mention a user with a timezone to define the timezone for the users (_recommended_ only for admins)\n" +
      "You can also just specify the timezone and it will be applied to yourself\n\n";
    if (cvs == Config.ConfVal.NotAllowed) eb.Description += "**Set Timezone** is _Disabled_";
    if (cvs == Config.ConfVal.OnlyAdmins) eb.Description += "**Set Timezone** is _Enabled_ for Admins";
    if (cvs == Config.ConfVal.Everybody) eb.Description += "**Set Timezone** is _Enabled_ for Everybody";
    if (cvg == Config.ConfVal.NotAllowed) eb.Description += "**Get Timezone** is _Disabled_";
    if (cvg == Config.ConfVal.OnlyAdmins) eb.Description += "**Get Timezone** is _Enabled_ for Admins";
    if (cvg == Config.ConfVal.Everybody) eb.Description += "**Get Timezone** is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattzlabs", "Set values", true));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.NotAllowed), "idfeattzs0", "Not allowed", false, GetYN(cvs, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.OnlyAdmins), "idfeattzs1", "Only Admins (recommended)", false, GetYN(cvs, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.Everybody), "idfeattzs2", "Everybody", false, GetYN(cvs, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattzlabg", "Read values", true));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.NotAllowed), "idfeattzg0", "Not allowed", false, GetYN(cvg, Config.ConfVal.NotAllowed)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.OnlyAdmins), "idfeattzg1", "Only Admins", false, GetYN(cvg, Config.ConfVal.OnlyAdmins)));
    actions.Add(new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.Everybody), "idfeattzg2", "Everybody", false, GetYN(cvg, Config.ConfVal.Everybody)));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateSpamProtectInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Spam Protection"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config c = GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    bool edisc = c != null && (c.IdVal & 1) == 1;
    bool esteam = c != null && (c.IdVal & 2) == 2;
    bool eepic = c != null && (c.IdVal & 4) == 4;
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **Spam Protection** is a feature of the bot used to watch all posts contain links.\n" +
      "If the link is a counterfait Discord (or Steam, or Epic) link (usually a false free nitro,\n" +
      "then the link will be immediately removed.\n\n**Spam Protection** for\n";
    eb.Description += "**Discord Nitro** feature is " + (edisc ? "_Enabled_" : "_Disabled_") + " _recommended!_\n";
    eb.Description += "**Steam** feature is " + (esteam ? "_Enabled_" : "_Disabled_") + "\n";
    eb.Description += "**Epic Game Store** feature is " + (eepic ? "_Enabled_" : "_Disabled_") + "\n";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(edisc ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect0", "Discord Nitro", false, edisc ? ey : en));
    actions.Add(new DiscordButtonComponent(esteam ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect1", "Steam", false, esteam ? ey : en));
    actions.Add(new DiscordButtonComponent(eepic ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect2", "Epic", false, eepic ? ey : en));
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }




  private DiscordMessage CreateBannedWordsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Banned Words"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot can automatically remove messages containing banned words.\n\n";

    if (!BannedWords.ContainsKey(ctx.Guild.Id) || BannedWords[ctx.Guild.Id].Count == 0) {
      eb.Description += "No banned words defined.\n\n";
    } else {
      eb.Description += "Banned words: ";
      foreach (var w in BannedWords[ctx.Guild.Id])
        eb.Description += w + ", ";
      eb.Description = eb.Description[0..^2] + "\n\n";
    }
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Banned Words** feature is _Disabled_";
    else eb.Description += "**Banned Words** feature is _Enabled_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent>();
    actions.Add(
      cv==Config.ConfVal.NotAllowed ?
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatbannedwed", "Enable", false, ey) :
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatbannedwed", "Disable", false, en));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatbannedwadd", "Add", false, er));

    if (BannedWords.ContainsKey(ctx.Guild.Id)) {
      // Goups of 5, but we start at 1 for the global enable/disable
      int num = 1;
      int count = 0;
      foreach (var w in BannedWords[ctx.Guild.Id]) {
        actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idfeatbannedwr" + count, w, false, en));
        num++;
        count++;
        if (num == 5) {
          builder.AddComponents(actions);
          actions = new List<DiscordButtonComponent>();
        }
      }
    }
    if (actions.Count > 0) builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }






  private static Config GetConfig(ulong gid, Config.ParamType t) {
    if (!Configs.ContainsKey(gid)) return null;
    List<Config> cs = Configs[gid];
    foreach (var c in cs) {
      if (c.IsParam(t)) return c;
    }
    return null;
  }
  private static Config.ConfVal GetConfigValue(ulong gid, Config.ParamType t) {
    if (!Configs.ContainsKey(gid)) return Config.ConfVal.NotAllowed;
    List<Config> cs = Configs[gid];
    foreach (var c in cs) {
      if (c.IsParam(t)) return (Config.ConfVal)c.IdVal;
    }
    return Config.ConfVal.NotAllowed;
  }
  private void SetConfigValue(ulong gid, Config.ParamType t, Config.ConfVal v) {
    if (!Configs.ContainsKey(gid)) {
      Configs[gid] = new List<Config>();
    }
    List<Config> cs = Configs[gid];
    Config tc = null;
    foreach (var c in cs) {
      if (c.IsParam(t)) {
        tc = c;
        break;
      }
    }
    if (tc == null) {
      tc = new Config(gid, t, (ulong)v);
      Configs[gid].Add(tc);
    } else {
      Database.Delete(tc);
      tc.SetVal(v); 
    }
    Database.Add(tc);
  }

  private DiscordComponentEmoji GetYN(Config.ConfVal cv) {
    if (cv == Config.ConfVal.NotAllowed) return en;
    return ey;
  }

  private DiscordComponentEmoji GetYN(Config.ConfVal cv, Config.ConfVal what) {
    if (cv == what) return ey;
    return en;
  }

  private DSharpPlus.ButtonStyle GetStyle(Config.ConfVal cv) {
    switch (cv) {
      case Config.ConfVal.NotAllowed: return DSharpPlus.ButtonStyle.Secondary;
      case Config.ConfVal.OnlyAdmins: return DSharpPlus.ButtonStyle.Danger;
      default: return DSharpPlus.ButtonStyle.Primary;
    }
  }

  private DSharpPlus.ButtonStyle GetIsStyle(Config.ConfVal cv, Config.ConfVal what) {
    if (cv == what) return DSharpPlus.ButtonStyle.Secondary;
    return DSharpPlus.ButtonStyle.Primary;
  }



}

public class TrackChannel {
  public DiscordChannel channel;
  public bool trackJoin;
  public bool trackLeave;
  public bool trackRoles;
  public Config config;
}