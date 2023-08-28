using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using System.Collections.Generic;
using UPBot.UPBot_Code;

/// <summary>
/// Provide some server stats
/// author: CPU
/// </summary>
public class SlashStats : ApplicationCommandModule {

  /*
  Stats
  > Show global stats: 
      - global server stats (times and numbers)
      - number of roles with numebr of people for each role
      - interaction to check most mentioned people in current channel (or type a channel)
      - interaction to most used emojis (or type a channel)
      - interaction to most posting and mentioned roles (or type a channel)
      - button for all 3 stats together

  stats roles #channel
  stats mentions #channel
  stats emojis #channel
  stats all #channel  
  */

  public enum StatsTypes {
    [ChoiceName("Only server")] OnlyServer,
    [ChoiceName("Roles")] Roles,
    [ChoiceName("Mentions")] Mentions,
    [ChoiceName("Emojis")] Emojis,
    [ChoiceName("All stats")] AllStats
  }


  [SlashCommand("stats", "Provides server stats, including detailed stats for roles, mentions, and emojis when specified")]
  public async Task StatsCommand(InteractionContext ctx, [Option("what", "What type of stats to show")] StatsTypes? what) {
    Utils.LogUserCommand(ctx);

    try {
      if (what == null || what == StatsTypes.OnlyServer) {
        await ctx.CreateResponseAsync(GenerateStatsEmbed(ctx));

      }
      else if (what == StatsTypes.AllStats) {
        await ctx.CreateResponseAsync(GenerateStatsEmbed(ctx));

        DiscordMessage fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating emojis stats..."));
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateEmojis(ctx).Result));
        await ctx.DeleteFollowupAsync(fup.Id);

        fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating mentions stats..."));
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateUserMentions(ctx).Result));
        await ctx.DeleteFollowupAsync(fup.Id);

        fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating roles stats..."));
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateRoleMentions(ctx).Result));
        await ctx.DeleteFollowupAsync(fup.Id);

      }
      else if (what == StatsTypes.Emojis) {
        await ctx.CreateResponseAsync(GenerateStatsEmbed(ctx));
        DiscordMessage fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating emojis stats..."));
        await ctx.DeleteFollowupAsync(fup.Id);
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateEmojis(ctx).Result));

      }
      else if (what == StatsTypes.Mentions) {
        await ctx.CreateResponseAsync(GenerateStatsEmbed(ctx));
        DiscordMessage fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating mentions stats..."));
        await ctx.DeleteFollowupAsync(fup.Id);
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateUserMentions(ctx).Result));

      }
      else if (what == StatsTypes.Roles) {
        await ctx.CreateResponseAsync(GenerateStatsEmbed(ctx));
        DiscordMessage fup = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Calculating roles stats..."));
        await ctx.DeleteFollowupAsync(fup.Id);
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(CalculateRoleMentions(ctx).Result));
      }

    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Stats", ex));
    }
  }


  static DiscordEmbedBuilder GenerateStatsEmbed(InteractionContext ctx) {
    DiscordEmbedBuilder e = new();
    DiscordGuild g = ctx.Guild;

    e.Description = " ----  ---- Stats ----  ---- \n_" + g.Description + "_";

    int? m1 = g.ApproximateMemberCount;
    int m2 = g.MemberCount;
    int? m3 = g.MaxMembers;
    string members = (m1 == null) ? m2.ToString() : (m1 + "/" + m2 + "/" + m3 + "max");
    e.AddField("Members", members + (g.IsLarge ? " (large)" : ""), true);
    int? p1 = g.ApproximatePresenceCount;
    int? p2 = g.MaxPresences;
    if (p1 != null) e.AddField("Presence", p1.ToString() + (p2 != null ? "/" + p2 : ""), true);
    int? s1 = g.PremiumSubscriptionCount;
    if (s1 != null) e.AddField("Boosters", s1.ToString(), true);

    double days = (DateTime.Now - g.CreationTimestamp.UtcDateTime).TotalDays;
    e.AddField("Server created", (int)days + " days ago", true);
    double dailyms = m2 / days;
    e.AddField("Daily members", dailyms.ToString("N1") + " members per day", true);

    e.WithTitle("Stats for " + g.Name);
    e.WithThumbnail(g.IconUrl);
    e.WithImageUrl(g.BannerUrl);

    int numtc = 0, numvc = 0, numnc = 0;
    foreach (var c in g.Channels.Values) {
      if (c.Bitrate != null && c.Bitrate != 0) numvc++;
      else if (c.IsNSFW) numnc++;
      else numtc++;
    }

    if (g.IsNSFW) e.AddField("NSFW", "NSFW server\nFilter level: " + g.ExplicitContentFilter.ToString() + "\nNSFW restriction type: " + g.NsfwLevel.ToString(), true);

    e.AddField("Roles:", g.Roles.Count + " roles", true);

    e.AddField("Cannels", numtc + " text, " + numvc + " voice" + (numnc > 0 ? ", " + numnc + " nsfw" : "") +
      (g.SystemChannel == null ? "" : "\nSystem channel: " + g.SystemChannel.Mention) +
      (g.RulesChannel == null ? "" : "\nRules channel: " + g.RulesChannel.Mention), false);

    string emojis;
    if (g.Emojis.Count > 0) {
      emojis = g.Emojis.Count + " custom emojis: ";
      foreach (var emj in g.Emojis.Values) emojis += Utils.GetEmojiSnowflakeID(emj) + " ";
      e.AddField("Emojis:", emojis, true);
    }
    return e;
  }

  static async Task<string> CalculateEmojis(InteractionContext ctx) {
    Dictionary<string, int> count = new();

    var msgs = await ctx.Channel.GetMessagesAsync(1000);
    foreach (var m in msgs) {
      var emjs = m.Reactions;
      foreach (var r in emjs) {
        string snowflake = Utils.GetEmojiSnowflakeID(r.Emoji);
        if (snowflake == null) continue;
        if (count.ContainsKey(snowflake)) count[snowflake] += r.Count;
        else count[snowflake] = r.Count;
      }
    }
    List<KeyValuePair<string, int>> list = new();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "\n_Used emojis_: used " + list.Count + " different emojis(as reactions):\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += $"**{list[i].Key}**({list[i].Value}) ";
    }
    if (list.Count >= 25) res += " _showing only the first, most used, 25._";
    return res;
  }

  static async Task<string> CalculateUserMentions(InteractionContext ctx) {
    Dictionary<string, int> count = new();
    Dictionary<ulong, int> askers = new();

    var msgs = await ctx.Channel.GetMessagesAsync(1000);
    foreach (var m in msgs) {
      var mens = m.MentionedUsers;
      foreach (var r in mens) {
        string snowflake = r.Username;
        if (snowflake == null) continue;
        snowflake = snowflake.Replace("_", "\\_");
        if (count.ContainsKey(snowflake)) count[snowflake]++;
        else count[snowflake] = 1;

        if (!askers.ContainsKey(m.Author.Id)) askers[m.Author.Id] = 1;
        else askers[m.Author.Id]++;
      }
    }
    List<KeyValuePair<string, int>> list = new();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    List<KeyValuePair<string, int>> listask = new();
    foreach (var k in askers.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askers[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "\n_Mentioned users_: " + list.Count + " users have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += $"**{list[i].Key}**({list[i].Value}) ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned other users:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += $"**{listask[i].Key}**({listask[i].Value}) ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    return res;
  }

  static async Task<string> CalculateRoleMentions(InteractionContext ctx) {
    Dictionary<string, int> count = new();
    Dictionary<ulong, int> askers = new();

    var msgs = await ctx.Channel.GetMessagesAsync(1000);
    foreach (var m in msgs) {
      var mens = m.MentionedRoles;
      foreach (var r in mens) {
        string snowflake = r.Name;
        if (snowflake == null) continue;
        snowflake = snowflake.Replace("_", "\\_");
        if (count.ContainsKey(snowflake)) count[snowflake]++;
        else count[snowflake] = 1;

        if (!askers.ContainsKey(m.Author.Id)) askers[m.Author.Id] = 1;
        else askers[m.Author.Id]++;
      }
    }
    List<KeyValuePair<string, int>> list = new();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    List<KeyValuePair<string, int>> listask = new();
    foreach (var k in askers.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askers[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "\n_Mentioned roles_: " + list.Count + " roles have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += $"**{list[i].Key}**({list[i].Value}) ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned the roles:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += $"**{listask[i].Key}**({listask[i].Value}) ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    return res;
  }

}
