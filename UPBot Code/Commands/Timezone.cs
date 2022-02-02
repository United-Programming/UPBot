using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
/// <summary>
/// This command implements a table for the timezones of the users
/// Users can define their own timezone and can see the local time of other users that defined the timezone
/// author: CPU
/// </summary>
public class TimezoneCmd : BaseCommandModule {

  [Command("timezone")]
  [Aliases("tz")]
  [Description("Get information about a users timezone and allow to set timezones for users.")]
  public async Task TimezoneCommand(CommandContext ctx) { // Basic version without parameters
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneG, ctx) && !Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneS, ctx)) return;
    await Utils.DeleteDelayed(10, await ctx.RespondAsync("Please specify the user to see its timezone, or an user and a timezone value to set it. Use `list` to show all known timezones."));
  }

  [Command("timezone")]
  public async Task TimezoneCommand(CommandContext ctx, [Description("The user to get info from.")] DiscordMember member) { // Standard version with a user
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneG, ctx)) return;
    await GetTimezone(ctx, member);
  }

  [Command("timezone")]
  public async Task TimezoneCommand(CommandContext ctx, [Description("The user to get info from.")] DiscordMember member, [Description("The timezone to set (list to show all, remove to remove the timezone)")] string timezone) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneS, ctx)) return;
    await SetTimezone(ctx, member, timezone);
  }

  [Command("timezone")]
  public async Task TimezoneCommand(CommandContext ctx, [Description("The timezone to set (list to show all, remove to remove the timezone)")] string timezone) {
    if (timezone.Trim().Contains("list", StringComparison.InvariantCultureIgnoreCase)) {
      if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneG, ctx)) return;
      await GetTimezone(ctx, null);
    } else {
      if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.TimezoneS, ctx)) return;
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
          if (first) { msg += " users"; first = false; }
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
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("GetTimezone", ex));
    }
  }

  private Task SetTimezone(CommandContext ctx, DiscordMember m, string timezone) {
    Utils.LogUserCommand(ctx);
    try {
      if (timezone.Trim().Equals("remove", StringComparison.InvariantCultureIgnoreCase)) {
        Database.DeleteByKey<Timezone>(m.Id);
        return Utils.DeleteDelayed(10, ctx.RespondAsync("Timezonefor user " + m.Username + " removed.").Result);
      }

      float offset = Timezone.GetOffset(timezone, out string name);
      if (offset == -999) return Utils.DeleteDelayed(10, ctx.RespondAsync("I do not understand the time zone: `" + timezone + "'.").Result);
      
      Timezone tz = new Timezone(m.Id, offset, name);
      Database.Add(tz);
      return Utils.DeleteDelayed(10, ctx.RespondAsync("TimeZone for user " + m.Mention + " set to " + name).Result);

    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("SetTimezone", ex));
    }
  }

  string CalculateLocalTime(float offset) { // Calculate the hour in local time
    return DateTime.UtcNow.AddHours(offset).ToString("HH:mm");
  }

}