using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Provide some server stats
/// author: CPU
/// </summary>
public class Stats : BaseCommandModule {

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

  [Command("stats")]
  [Description("Provides server stats, including detailed stats for roles, mentions, and emojis when specified")]
  public async Task DoStats(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Stats, ctx)) return;
    await GenerateStatsInteractive(ctx);
  }

  [Command("stats")]
  [Description("Provides server stats, including detailed stats for roles, mentions, and emojis when specified")]
  public async Task DoStats(CommandContext ctx, [Description("Specific type of stats to calculate (server, emoji, users, roles, all, full)")] string cmd) {
    await GenerateStats(ctx, cmd, ctx.Channel);
  }
  public async Task DoStats(CommandContext ctx, [Description("Specific type of stats to calculate (server, emoji, users, roles, all, full)")] string cmd, [Description("Specific channel for the stats")] DiscordChannel channel) {
    await GenerateStats(ctx, cmd, channel);
  }

  async Task GenerateStats(CommandContext ctx, string cmd, DiscordChannel ch) { 
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Stats, ctx)) return;
    Utils.LogUserCommand(ctx);
    try { 
    cmd = cmd.Trim().ToLowerInvariant();
      if (cmd == "server") {
        DiscordEmbedBuilder e = GenerateStatsEmbed(ctx);
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(e.Build()));

      } else if (cmd == "full") {
        var msg = await ctx.Channel.SendMessageAsync("Calculating server stats, counting emojis usage, mentioned users and roles for the channel...");
        DiscordEmbedBuilder e = GenerateStatsEmbed(ctx);
        string res = await CalculateAll(ctx, ch);
        _ = msg.DeleteAsync();
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(e.Build() + "\n" + res));

      } else if (cmd == "all") {
        var msg = await ctx.Channel.SendMessageAsync("Counting emojis usage, mentioned users and roles for the channel...");
        string res = await CalculateAll(ctx, ch);
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (cmd == "emojis") {
        var msg = await ctx.Channel.SendMessageAsync("Counting emojis usage, mentioned users and roles for the channel...");
        string res = await CalculateEmojis(ctx, ch);
        _ = msg.DeleteAsync();
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (cmd == "users") {
        var msg = await ctx.Channel.SendMessageAsync("Counting mentioned people...");
        string res = await CalculateUserMentions(ctx, ch);
        _ = msg.DeleteAsync();
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (cmd == "roles") {
        var msg = await ctx.Channel.SendMessageAsync("Counting mentioned roles...");
        string res = await CalculateRoleMentions(ctx, ch);
        _ = msg.DeleteAsync();
        await Utils.DeleteDelayed(60, ctx.Message);
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Stats", ex));
    }
  }

  DiscordEmbedBuilder GenerateStatsEmbed(CommandContext ctx) {
    DiscordEmbedBuilder e = new DiscordEmbedBuilder();
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
    e.AddField("Daily mebers", dailyms.ToString("N1") + " members per day", true);

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

    string emojis = "";
    if (g.Emojis.Count > 0) {
      emojis = g.Emojis.Count + " custom emojis: ";
      foreach (var emj in g.Emojis.Values) emojis += Utils.GetEmojiSnowflakeID(emj) + " ";
      e.AddField("Emojis:", emojis, true);
    }
    return e;
  }

  public async Task GenerateStatsInteractive(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      var inter = ctx.Client.GetInteractivity();

      var b = new DiscordMessageBuilder();
      b.AddEmbed(GenerateStatsEmbed(ctx).Build());

      var actions = new List<DiscordButtonComponent>();
      actions.Add(new DiscordButtonComponent(ButtonStyle.Primary, "idusedemojis", "Used emojis", false));
      actions.Add(new DiscordButtonComponent(ButtonStyle.Primary, "idmentionedusers", "Mentioned people", false));
      actions.Add(new DiscordButtonComponent(ButtonStyle.Primary, "idmentionedroles", "Mentioned roles", false));
      actions.Add(new DiscordButtonComponent(ButtonStyle.Success, "idall", "All 3", false));
      b.AddComponents(actions);

      var msg = await b.SendAsync(ctx.Channel);

      var result = await inter.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));

      var ir = result.Result;
      if (ir == null) {
        await ctx.Message.DeleteAsync();
        await msg.DeleteAsync();
        return;
      }

      if (ir.Id == "idall") {
        msg.DeleteAsync().Wait();
        msg = await ctx.Channel.SendMessageAsync("Counting emojis usage, mentioned users and roles for the channel...");
        string res = await CalculateAll(ctx, ctx.Channel);
        msg.DeleteAsync().Wait();
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (ir.Id == "idusedemojis") {
        msg.DeleteAsync().Wait();
        msg = await ctx.Channel.SendMessageAsync("Counting emojis usage for the channel...");
        string res = await CalculateEmojis(ctx, ctx.Channel);
        msg.DeleteAsync().Wait();
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (ir.Id == "idmentionedusers") {
        msg.DeleteAsync().Wait();
        msg = await ctx.Channel.SendMessageAsync("Counting mentioned people...");
        string res = await CalculateUserMentions(ctx, ctx.Channel);
        msg.DeleteAsync().Wait();
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else if (ir.Id == "idmentionedroles") {
        msg.DeleteAsync().Wait();
        msg = await ctx.Channel.SendMessageAsync("Counting mentioned roles...");
        string res = await CalculateRoleMentions(ctx, ctx.Channel);
        msg.DeleteAsync().Wait();
        await Utils.DeleteDelayed(120, ctx.Channel.SendMessageAsync(res));

      } else {
        await Utils.DeleteDelayed(120, ctx.Message);
        msg.DeleteAsync().Wait();
      }

    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Stats", ex));
    }
  }

  async Task<string> CalculateEmojis(CommandContext ctx, DiscordChannel c) {
    Dictionary<string, int> count = new Dictionary<string, int>();

    var msgs = await c.GetMessagesAsync(1000);
    foreach (var m in msgs) {
      var emjs = m.Reactions;
      foreach (var r in emjs) {
        string snowflake = Utils.GetEmojiSnowflakeID(r.Emoji);
        if (snowflake == null) continue;
        if (count.ContainsKey(snowflake)) count[snowflake] += r.Count;
        else count[snowflake] = r.Count;
      }
    }
    List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "**Stats** (last 1000 messages)" + (c==ctx.Channel ? "" : " for channel " + c.Mention) + "\n_Used emojis_: used " + list.Count + " different emojis(as reactions):\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + "(" + list[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most used, 25._";
    return res;
  }

  async Task<string> CalculateUserMentions(CommandContext ctx, DiscordChannel c) {
    Dictionary<string, int> count = new Dictionary<string, int>();
    Dictionary<ulong, int> askers = new Dictionary<ulong, int>();

    var msgs = await c.GetMessagesAsync(1000);
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
    List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    List<KeyValuePair<string, int>> listask = new List<KeyValuePair<string, int>>();
    foreach (var k in askers.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askers[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "**Stats** (last 1000 messages)" + (c==ctx.Channel ? "" : " for channel " + c.Mention) + "\n_Mentioned users_: " + list.Count + " users have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + "(" + list[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned other users:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += listask[i].Key + "(" + listask[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    return res;
  }

  async Task<string> CalculateRoleMentions(CommandContext ctx, DiscordChannel c) {
    Dictionary<string, int> count = new Dictionary<string, int>();
    Dictionary<ulong, int> askers = new Dictionary<ulong, int>();

    var msgs = await c.GetMessagesAsync(1000);
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
    List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
    foreach (var k in count.Keys) list.Add(new KeyValuePair<string, int>(k, count[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    List<KeyValuePair<string, int>> listask = new List<KeyValuePair<string, int>>();
    foreach (var k in askers.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askers[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "**Stats** (last 1000 messages)" + (c==ctx.Channel ? "" : " for channel " + c.Mention) + "\n_Mentioned roles_: " + list.Count + " roles have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + "(" + list[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned the roles:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += listask[i].Key + "(" + listask[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    return res;
  }

  async Task<string> CalculateAll(CommandContext ctx, DiscordChannel c) {
    Dictionary<string, int> countE = new Dictionary<string, int>();
    Dictionary<string, int> countU = new Dictionary<string, int>();
    Dictionary<string, int> countR = new Dictionary<string, int>();
    Dictionary<ulong, int> askersU = new Dictionary<ulong, int>();
    Dictionary<ulong, int> askersR = new Dictionary<ulong, int>();

    var msgs = await c.GetMessagesAsync(1000);
    foreach (var m in msgs) {
      var emjs = m.Reactions;
      foreach (var r in emjs) {
        string snowflake = Utils.GetEmojiSnowflakeID(r.Emoji);
        if (snowflake == null) continue;
        if (countE.ContainsKey(snowflake)) countE[snowflake]++;
        else countE[snowflake] = 1;
      }

      var usrs = m.MentionedUsers;
      foreach (var r in usrs) {
        string snowflake = r.Username;
        if (snowflake == null) continue;
        snowflake = snowflake.Replace("_", "\\_");
        if (countU.ContainsKey(snowflake)) countU[snowflake]++;
        else countU[snowflake] = 1;
        if (!askersU.ContainsKey(m.Author.Id)) askersU[m.Author.Id] = 1;
        else askersU[m.Author.Id]++;
      }

      var rols = m.MentionedRoles;
      foreach (var r in rols) {
        string snowflake = r.Name;
        if (snowflake == null) continue;
        snowflake = snowflake.Replace("_", "\\_");
        if (countR.ContainsKey(snowflake)) countR[snowflake]++;
        else countR[snowflake] = 1;
        if (!askersR.ContainsKey(m.Author.Id)) askersR[m.Author.Id] = 1;
        else askersR[m.Author.Id]++;
      }
    }
    List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
    foreach (var k in countE.Keys) list.Add(new KeyValuePair<string, int>(k, countE[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    string res = "**Stats** (last 1000 messages)" + (c==ctx.Channel ? "" : " for channel " + c.Mention) + "\n_Used emojis_: used " + list.Count + " different emojis (as reactions):\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + " (" + list[i].Value + ")  ";
    }
    if (list.Count >= 25) res += " _showing only the first, most used, 25._";

    list.Clear();
    foreach (var k in countU.Keys) list.Add(new KeyValuePair<string, int>(k, countU[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });
    List<KeyValuePair<string, int>> listask = new List<KeyValuePair<string, int>>();
    foreach (var k in askersU.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askersU[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    res += "\n_Mentioned users_: " + list.Count + " users have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + " (" + list[i].Value + ")  ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned other users:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += listask[i].Key + "(" + listask[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    list.Clear();
    foreach (var k in countR.Keys) list.Add(new KeyValuePair<string, int>(k, countR[k]));
    list.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    listask.Clear();
    foreach (var k in askersU.Keys) {
      DiscordUser u = await ctx.Channel.Guild.GetMemberAsync(k);
      if (u == null) continue;
      listask.Add(new KeyValuePair<string, int>(u.Username, askersU[k]));
    }
    listask.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

    res += "\n_Mentioned roles_: " + list.Count + " roles have been mentioned:\n  ";
    for (int i = 0; i < 25 && i < list.Count; i++) {
      res += list[i].Key + " (" + list[i].Value + ")  ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    res += "\n_Users mentioning_: " + listask.Count + " users have mentioned the roles:\n  ";
    for (int i = 0; i < 25 && i < listask.Count; i++) {
      res += listask[i].Key + "(" + listask[i].Value + ") ";
    }
    if (list.Count >= 25) res += " _showing only the first, most mentioned, 25._";

    return res;
  }

}
