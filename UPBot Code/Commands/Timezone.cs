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

    try {
      int best = -1;
      int dist = 0;
      string[] parts = tz.Split(' ');
      for (int i = 0; i < values.Length; i++) {
        int d = 0;
        foreach (var part in parts) {
          string withspaces = " " + part + " ";
          if (values[i][3].Contains(part, StringComparison.InvariantCulture)) d += 3;
          if (values[i][3].Contains(part, StringComparison.InvariantCultureIgnoreCase)) d++;
          if (values[i][3].Contains(withspaces, StringComparison.InvariantCultureIgnoreCase)) d += 2;
          if (values[i][3].Contains(withspaces, StringComparison.InvariantCulture)) d += 4;
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
    } catch (Exception ex) {
      if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "WhatTimeIs", ex), true);
    }
  }

  [SlashCommand("whattimeisfor", "Checks the current local time for an user")]
  public async Task TZTimeGetCommand(InteractionContext ctx, [Option("user", "The user for the time")] DiscordUser user) {
    Utils.LogUserCommand(ctx);

    try {
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
    } catch (Exception ex) {
      if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "WhatTimeIsFor", ex), true);
    }
  }

  [SlashCommand("set", "Set the timezone for a user")]
  public async Task TZSetCommand(InteractionContext ctx, [Option("user", "The user to set the timezone")] DiscordUser user, [Option("timezone", "Timezone to check the local time")] string tz) {
    try {
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
          if (values[i][3].Contains(part, StringComparison.InvariantCulture)) d += 3;
          if (values[i][3].Contains(part, StringComparison.InvariantCultureIgnoreCase)) d++;
          if (values[i][3].Contains(withspaces, StringComparison.InvariantCultureIgnoreCase)) d += 2;
          if (values[i][3].Contains(withspaces, StringComparison.InvariantCulture)) d += 4;
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
    } catch (Exception ex) {
      if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Set Timezone", ex), true);
    }
  }


  [SlashCommand("list", "List all timezones with how meny users are in each one")]
  public async Task TZListCommand(InteractionContext ctx, [Option("all", "Type all if you want to know all known names for toimezones")] string all = null) {
    try {
      string res = "```\n";

      if (all != null && all.Equals("all")) {
        res = "List of all known time zones:\n```\n";
        for (int i = 0; i < values.Length; i++) {
          //    new[] { "CET",  "Central European Standard Time",   "GMT+1:00", " CET Central Europe Germany berlin France Paris Belgium Brussels Amsterdam Nederland Spain Madrid Italy Rome Czekia Praha Switzerland Sweden Stockholm Romania Bucarest Austria Vienna Croatia Hungary Budapest Norway Oslo Poland Warsaw 	Albania Algeria Andorra Austria Belgium Bosnia and Herzegovina Bouvet Island Croatia Czechia Denmark France Germany Gibraltar Hungary Italy Kosovo Liechtenstein Luxembourg Malta Montenegro Netherlands North Macedonia Norway Poland Principality of Monaco San Marino Serbia Slovakia Slovenia Spain Svalbard Sweden Switzerland Tunisia Vatican "},
          res += values[i][2] + "  " + values[i][1] + "\n";
        }
        res += "```";
        await ctx.CreateResponseAsync(res);
        return;
      }

      var list = Database.GetAll<Timezone>();
      Dictionary<string, int> count = new Dictionary<string, int>();
      foreach (Timezone t in list) {
        if (!count.ContainsKey(t.TimeZoneName)) count[t.TimeZoneName] = 1;
        else count[t.TimeZoneName]++;
      }
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
          bool isDaylight = tzi.IsDaylightSavingTime(DateTime.Now);
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
    } catch (Exception ex) {
      if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "List Timezones", ex), true);
    }
  }



  readonly string[][] values = {
    new[] { "NZST", "New Zealand Standard Time",        "GMT+12:00", " NZST New Zealand Auckland Wellington "},
    new[] { "AEST", "E. Australia Standard Time",       "GMT+10:00", " AEST AUS Australian Eastern Time Brisbane Sydney Melbourne Hobart Canberra "},
    new[] { "JST",  "Tokyo Standard Time",              "GMT+09:00", " JST Japan Standard Time Osaka Sapporo Tokyo "},
    new[] { "KST",  "Korea Standard Time",              "GMT+09:00", " KST Korea Seoul "},
    new[] { "ACST", "AUS Central Standard Time",        "GMT+09:30", " ACST Australian Central Time Adelaide Darwin Broken Hill "},
    new[] { "CHT",  "China Standard Time",              "GMT+08:00", " CST China Standard Time Macao Taiwan Bejing Shanghai Chongqing Hong Kong Urumqi "},
    new[] { "AWST", "W. Australia Standard Time",       "GMT+08:00", " AWST Australian Western Time Perth "},
    new[] { "SGT",  "Singapore Standard Time",          "GMT+08:00", " Singapore Kuala Lumpur "},
    new[] { "IST",  "India Standard Time",              "GMT+05:30", " IST India Standard Time Chennai Kolkata Mumbai Delhi "},
    new[] { "TRT",  "Turkey Standard Time",             "GMT+03:00", " TRT Turkey Time Istanbul "},
    new[] { "AST",  "Arabian Standard Time",            "GMT+03:00", " AST Arabian Standard Time Abu Dhabi Muscart "},
    new[] { "MSK",  "Russian Standard Time",            "GMT+03:00", " MSK Moscow Standard Time Belarus Russia Moskow Petersburg "},
    new[] { "EET",  "FLE Standard Time",                "GMT+02:00", " EET Eastern European Time Bulgaria Cyprus Egypt Estonia Finland Greece Jordan Latvia Lebanon Libya Lithuania Moldova Palestine Romania Russia Syria Ukraine "},
    new[] { "CET",  "Central European Standard Time",   "GMT+01:00", " CET Central Europe Germany berlin France Paris Belgium Brussels Amsterdam Nederland Spain Madrid Italy Rome Czekia Praha Switzerland Sweden Stockholm Romania Bucarest Austria Vienna Croatia Hungary Budapest Norway Oslo Poland Warsaw 	Albania Algeria Andorra Austria Belgium Bosnia and Herzegovina Bouvet Island Croatia Czechia Denmark France Germany Gibraltar Hungary Italy Kosovo Liechtenstein Luxembourg Malta Montenegro Netherlands North Macedonia Norway Poland Principality of Monaco San Marino Serbia Slovakia Slovenia Spain Svalbard Sweden Switzerland Tunisia Vatican "},
    new[] { "MRC",  "Morocco Standard Time",            "GMT+01:00", " MRC Morocco Casablanca Rabat "},
    new[] { "GMT",  "GMT Standard Time",                "GMT+00:00", " GMT Greenwich Mean Time British UK United Kingdom Ireland London Dublin Island Dublin Edinburgh "},
    new[] { "WET",  "W. Europe Standard Time",          "GMT+00:00", " WET Western European Time Portugal Lisbon "},
    new[] { "ART",  "Argentina Standard Time",          "GMT-03:00", " ART Argentina Buenos Aires "},
    new[] { "BRT",  "E. South America Standard Time",   "GMT-03:00", " BRT Brasilia Time Bahia Sao Paulo Recife "},
    new[] { "ATS",  "Atlantic Standard Time",           "GMT-03:00", " ADT AST Atlantic Halifax "},
    new[] { "EST",  "Eastern Standard Time",            "GMT-05:00", " EST	EDT Eastern Toronto Montreal Quebec Boston Indianapolis Miami New York Philadelphia Orlando Ottawa "},
    new[] { "SAPST", "SA Pacific Standard Time",        "GMT-05:00", " SAPST SA Bogota Lima Quito Rio Branco "},
    new[] { "CST",  "Central Standard Time",            "GMT-06:00", " CST	CDT Central Mexico City Regina Winnipeg Cicago Austin "},
    new[] { "MST",  "Mountain Standard Time",           "GMT-07:00", " MST Mountain Whitehorse Edmonton Colorado Albuquerque Calgary Denver La Paz Phoenix Arizona Salt Lake "},
    new[] { "PST",  "Pacific Standard Time",            "GMT-08:00", " PDT PST Pacific Vancouver San Francisco Seattle "},
    new[] { "AKST", "Alaskan Standard Time",            "GMT-09:00", " AKDT AKST Alaska "},
    new[] { "HST",  "Hawaiian Standard Time",           "GMT-10:00", " HST Hawaii Hawaiian Honolulu "},
  };

// Keep the list of IDs from TimeZoneInfo
// Try to get if the timezone is in daylight savings and check the actual current time
// Show if it is daylight savings in the list
// Show GMT offsets in list
// Show list with all known ones
}