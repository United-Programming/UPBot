using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
/// <summary>
/// This command implements a table for the timezones of the users
/// Users can define their own timezone and can see the local time of other users that defined the timezone
/// author: CPU
/// </summary>


[SlashCommandGroup("tz", "Commands to check timezones")]
public class SlashTimezone : ApplicationCommandModule {
  [SlashCommand("whattimeis", "Checks the current local time in a timezone")]
  public async Task TZTimeCommand(InteractionContext ctx, [Option("timezone", "Timezone to check the local time")] string tz) {
    Utils.LogUserCommand(ctx);

    int best = -1;
    int dist = 0;
    string[] parts = tz.Split(' ');
    for (int i = 0; i < values.Length; i++) {
      int d = 0;
      foreach (var part in parts) {
        string withspaces = " " + part + " ";
        if (values[i][2].Contains(part, StringComparison.InvariantCulture)) d+=3;
        if (values[i][2].Contains(part, StringComparison.InvariantCultureIgnoreCase)) d++;
        if (values[i][2].Contains(withspaces, StringComparison.InvariantCultureIgnoreCase)) d+=2;
        if (values[i][2].Contains(withspaces, StringComparison.InvariantCulture)) d+=4;
      }
      if (d > dist) {
        dist = d;
        best = i;
      }
    }

    if (best == -1) {
      await ctx.CreateResponseAsync($"I cannot find a timezone similar to {tz}.", true);
      return;
    }

    var tzi = TimeZoneInfo.FindSystemTimeZoneById(values[best][1]);
    DateTime dest = TimeZoneInfo.ConvertTime(DateTime.Now, tzi);
    await ctx.CreateResponseAsync($"Current time in the timezone {values[best][0]} is {dest:HH:mm:ss}");
  }

  [SlashCommand("whattimeisfor", "Checks the current local time for an user")]
  public async Task TZTimeGetCommand(InteractionContext ctx, [Option("user", "The user for the time")] DiscordUser user) {
    Utils.LogUserCommand(ctx);

    DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
    Timezone utz = Database.GetByKey<Timezone>(user.Id);
    if (utz == null) {
      await ctx.CreateResponseAsync($"Timezone for user {member.DisplayName} is not defined", true);
      return;
    }

    string tzid = null;
    for (int i = 0; i < values.Length; i++) {
      if (values[i][0].Equals(utz.TimeZoneName, StringComparison.InvariantCultureIgnoreCase)) {
        tzid = values[i][1];
        break;
      }
    }

    if (tzid == null) {
      await ctx.CreateResponseAsync($"Timezone for user {member.DisplayName} is not clear: {utz.TimeZoneName}", true);
      return;
    }

    var tzi = TimeZoneInfo.FindSystemTimeZoneById(tzid);
    DateTime dest = TimeZoneInfo.ConvertTime(DateTime.Now, tzi);
    await ctx.CreateResponseAsync($"Current time for user {member.DisplayName} is {dest:HH:mm:ss} ({tzid})");
  }

  [SlashCommand("set", "Set the timezone for a user")]
  public async Task TZSetCommand(InteractionContext ctx, [Option("user", "The user to set the timezone")]DiscordUser user, [Option("timezone", "Timezone to check the local time")] string tz) {
    DiscordMember member = await ctx.Guild.GetMemberAsync(user.Id);
    if (!Configs.HasAdminRole(ctx.Guild.Id, member.Roles, false) && user.Id != ctx.User.Id) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

    int best = -1;
    int dist = 0;
    string[] parts = tz.Split(' ');
    for (int i = 0; i < values.Length; i++) {
      int d = 0;
      foreach (var part in parts) {
        string withspaces = " " + part + " ";
        if (values[i][2].Contains(part, StringComparison.InvariantCulture)) d+=3;
        if (values[i][2].Contains(part, StringComparison.InvariantCultureIgnoreCase)) d++;
        if (values[i][2].Contains(withspaces, StringComparison.InvariantCultureIgnoreCase)) d+=2;
        if (values[i][2].Contains(withspaces, StringComparison.InvariantCulture)) d+=4;
      }
      if (d > dist) {
        dist = d;
        best = i;
      }
    }

    if (best == -1) {
      await ctx.CreateResponseAsync($"I cannot find a timezone similar to {tz}.", true);
      return;
    }

    Timezone tdata = new Timezone(user.Id, values[best][0]);
    Database.Add(tdata);

    var tzi = TimeZoneInfo.FindSystemTimeZoneById(values[best][1]);
    DateTime dest = TimeZoneInfo.ConvertTime(DateTime.Now, tzi);
    await ctx.CreateResponseAsync($"Timezone for user {member.DisplayName} is set to {values[best][0]}. Current time for they is {dest:HH:mm:ss}");
  }


  [SlashCommand("list", "List all timezones with how meny users are in each one")]
  public async Task TZListCommand(InteractionContext ctx) {
    var list = Database.GetAll<Timezone>();
    Dictionary<string, int> count = new Dictionary<string, int>();
    foreach (Timezone t in list) {
      if (!count.ContainsKey(t.TimeZoneName)) count[t.TimeZoneName] = 1;
      else count[t.TimeZoneName]++;
    }
    string res = "```\n";
    foreach (var tzcode in count.Keys) {
      string tzid = null;
      for (int i = 0; i < values.Length; i++) {
        if (values[i][0].Equals(tzcode, StringComparison.InvariantCultureIgnoreCase)) {
          tzid = values[i][1];
          break;
        }
      }
      string code = tzcode;
      while (code.Length < 8) code += " ";
      if (tzid == null) {
        string offset = "??:??       ";
        res += code + offset + count[tzcode] + (count[tzcode] == 1 ? " user\n" : " users\n");
      }
      else {
        var tzi = TimeZoneInfo.FindSystemTimeZoneById(tzid);
        TimeSpan off = tzi.GetUtcOffset(DateTime.UtcNow);
        string offset;
        if (off.TotalMinutes < 0) offset = "-";
        else offset = "+";
        int h = Math.Abs(off.Hours);
        int m = Math.Abs(off.Minutes);
        if (h < 9) offset += "0";
        offset += h.ToString() + ":";
        if (m < 9) offset += "0";
        offset += m.ToString();
        offset += " UTC  ";
        res += code + offset + count[tzcode] + (count[tzcode] == 1 ? " user\n" : " users\n");
      }
    }
    res += "```";
    await ctx.CreateResponseAsync(res);
  }



  readonly string[][] values = {
    new[] { "CET",  "Central European Standard Time",   " CET Central Europe Germany berlin France Paris Belgium Brussels Amsterdam Nederland Spain Madrid Italy Rome Czekia Praha Switzerland Sweden Stockholm Romania Bucarest Austria Vienna Croatia Hungary Budapest Norway Oslo Poland Warsaw 	Albania Algeria Andorra Austria Belgium Bosnia and Herzegovina Bouvet Island Croatia Czechia Denmark France Germany Gibraltar Hungary Italy Kosovo Liechtenstein Luxembourg Malta Montenegro Netherlands North Macedonia Norway Poland Principality of Monaco San Marino Serbia Slovakia Slovenia Spain Svalbard Sweden Switzerland Tunisia Vatican "},
    new[] { "GMT",  "GMT Standard Time",                " GMT Greenwich Mean Time British UK United Kingdom Ireland London Dublin Island Dublin Edinburgh "},
    new[] { "WET",  "W. Europe Standard Time",          " WET Western European Time Portugal Lisbon "},
    new[] { "AKST", "Alaskan Standard Time",            " AKDT AKST Alaska "},
    new[] { "HST",  "Hawaiian Standard Time",           " HST Hawaii Hawaiian Honolulu "},
    new[] { "PST",  "Pacific Standard Time",            " PDT PST Pacific Vancouver San Francisco Seattle "},
    new[] { "MST",  "Mountain Standard Time",           " MST Mountain Whitehorse Edmonton Colorado Albuquerque Calgary Denver La Paz Phoenix Arizona Salt Lake "},
    new[] { "CST",  "Central Standard Time",            " CST	CDT Central Mexico City Regina Winnipeg Cicago Austin "},
    new[] { "EST",  "Eastern Standard Time",            " EST	EDT Eastern Toronto Montreal Quebec Boston Indianapolis Miami New York Philadelphia Orlando Ottawa "},
    new[] { "MSK",  "Russian Standard Time",            " MSK Moscow Standard Time Belarus Russia Moskow Petersburg "},
    new[] { "ART",  "Argentina Standard Time",          " ART Argentina Buenos Aires "},
    new[] { "KST",  "Korea Standard Time",              " KST Korea Seoul "},
    new[] { "EET",  "FLE Standard Time",                " EET Eastern European Time Bulgaria Cyprus Egypt Estonia Finland Greece Jordan Latvia Lebanon Libya Lithuania Moldova Palestine Romania Russia Syria Ukraine "},
    new[] { "AST",  "Arabian Standard Time",            " AST Arabian Standard Time Abu Dhabi Muscart "},
    new[] { "TRT",  "Turkey Standard Time",             " TRT Turkey Time Istanbul "},
    new[] { "IST",  "India Standard Time",              " IST India Standard Time Chennai Kolkata Mumbai Delhi "},
    new[] { "CHT",  "China Standard Time",              " CST China Standard Time Macao Taiwan Bejing Shanghai Chongqing Hong Kong Urumqi "},
    new[] { "JST",  "Tokyo Standard Time",              " JST Japan Standard Time Osaka Sapporo Tokyo "},
    new[] { "AEST", "E. Australia Standard Time",       " AEST AUS Australian Eastern Time Brisbane Sydney Melbourne Hobart Canberra "},
    new[] { "ACST", "AUS Central Standard Time",        " ACST Australian Central Time Adelaide Darwin Broken Hill "},
    new[] { "NZST", "New Zealand Standard Time",        " NZST New Zealand Auckland Wellington "},
    new[] { "AST",  "Atlantic Standard Time",           " AST Atlantic Halifax "},
    new[] { "AWST", "W. Australia Standard Time",       " AWST Australian Western Time Perth "},
    new[] { "SGT",  "Singapore Standard Time",          " Singapore Kuala Lumpur "},
    new[] { "SAPST", "SA Pacific Standard Time",        " SAPST SA Bogota Lima Quito Rio Branco "},
    new[] { "BRT",   "E. South America Standard Time",  " BRT Brasilia Time Bahia Sao Paulo Recife "},
    new[] { "MRC",  "Morocco Standard Time",            " MRC Morocco Casablanca Rabat "}
  };

  /*


  [Command("timezone")]
  [Aliases("tz")]
  [Description("Get information about a users timezone and allow to set timezones for users.")]
  public async Task TimezoneCommand(CommandContext ctx) { // Basic version without parameters
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneG, ctx) && !Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneS, ctx)) return;
    await Utils.DeleteDelayed(10, await ctx.RespondAsync("Please specify the user to see its timezone, or an user and a timezone value to set it. Use `list` to show all known timezones."));
    await TimezoneCommand(ctx, "list");



  }

  [Command("timezones")]
  [Aliases("tzs")]
  [Description("Get the list of known time zones")]
  public async Task TimezonesCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneG, ctx)) return;
    await TimezoneCommand(ctx, "list");
  }


  [Command("timezone")]
  public async Task TimezoneCommand(CommandContext ctx, [Description("The user to get info from.")] DiscordMember member, [Description("The timezone to set (list to show all, remove to remove the timezone)")] string timezone) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneS, ctx)) return;
    await SetTimezone(ctx, member, timezone);
  }

  [Command("timezone")]
  public async Task TimezoneCommand(CommandContext ctx, [Description("The timezone to set (list to show all, remove to remove the timezone)")] string timezone) {
    if (timezone.Trim().Contains("list", StringComparison.InvariantCultureIgnoreCase)) {
      if (!Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneG, ctx)) return;
      await GetTimezone(ctx, null);
    } else {

      // We can set our own timezone if the command is not disabled
      if (Setup.Disabled(ctx.Guild.Id, Config.ParamType.TimezoneS) && !Setup.Permitted(ctx.Guild, Config.ParamType.TimezoneS, ctx)) return;
      await SetTimezone(ctx, ctx.Member, timezone);
    }
  }

  private Task GetTimezone(CommandContext ctx, DiscordMember m) {
    Utils.LogUserCommand(ctx);
    try {
      string msg;
      List<Timezone> ltz = Database.GetAll<Timezone>();
      if (m == null) { // Show all known timezones
        Dictionary<string, int> tzs = new Dictionary<string, int>();
        foreach (Timezone tz in ltz) {
          if (tzs.ContainsKey(tz.TimeZoneName)) tzs[tz.TimeZoneName]++;
          else tzs[tz.TimeZoneName] = 1;
        }

        if (tzs.Count == 0) {
          return Utils.DeleteDelayed(10, ctx.RespondAsync("I do not know any time zone yet.").Result);
        }

        msg = "I know " + tzs.Count + " timezones:\n```";
        bool first = true;
        foreach (string tzn in tzs.Keys) {
          msg += tzn + "\t(" + tzs[tzn];
          if (first) { msg += " user"; first = false; }
          if (tzs[tzn] != 1) msg += "s";
          msg += ")\n";
        }
        msg += "```";
        return Utils.DeleteDelayed(10, ctx.RespondAsync(msg).Result);
      }
      Timezone offset = null;
      foreach (Timezone tz in ltz) {
        if (tz.User == m.Id) {
          offset = tz;
          break;
        }
      }

      if (offset != null) {
          msg = "TimeZone for " + m.DisplayName + " is " + offset.TimeZoneName + ". It is " + CalculateLocalTime(offset.UtcOffset) + " for the user.";
      } else
        msg = "User " + m.DisplayName + " has no TimeZone defined.";
      return Utils.DeleteDelayed(10, ctx.RespondAsync(msg).Result);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "GetTimezone", ex));
    }
  }

  private Task SetTimezone(CommandContext ctx, DiscordMember m, string timezone) {
    Utils.LogUserCommand(ctx);
    try {
      if (timezone.Trim().Equals("remove", StringComparison.InvariantCultureIgnoreCase)) {
        Database.DeleteByKeys<Timezone>(m.Id);
        return Utils.DeleteDelayed(10, ctx.RespondAsync("Timezonefor user " + m.Username + " removed.").Result);
      }

      float offset = Timezone.GetOffset(timezone, out string name);
      if (offset == -999) return Utils.DeleteDelayed(10, ctx.RespondAsync("I do not understand the time zone: `" + timezone + "'.").Result);
      
      Timezone tz = new Timezone(m.Id, offset, name);
      Database.Add(tz);
      return Utils.DeleteDelayed(10, ctx.RespondAsync("TimeZone for user " + m.Mention + " set to " + name).Result);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "SetTimezone", ex));
    }
  }

  string CalculateLocalTime(float offset) { // Calculate the hour in local time
    return DateTime.UtcNow.AddHours(offset).ToString("HH:mm");
  }
  */
}