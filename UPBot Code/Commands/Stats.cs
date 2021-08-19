using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Provide some server stats
/// author: CPU
/// </summary>
public class Stats : BaseCommandModule {

  ulong[] defChannelIDs = {
      830904407540367441ul,
      830904726375628850ul,
      830921265648631878ul,
      830921315657449472ul
    };
  string[] defChannelNames = {
      "Unity",
      "CSharp",
      "Help1",
      "Help2",
      "Completed!"
    };



  [Command("stats")]
  [Description("Provides server stats for specific channels, with no parameters checks the most 4 commons channels")]
  [Cooldown(1, 60, CooldownBucketType.Channel | CooldownBucketType.User)]
  public async Task DoStats(CommandContext ctx) {
    await GenerateStats(ctx, null, 100);
  }
  [Command("stats")]
  [Description("Provides server stats for specific channels")]
  [Cooldown(1, 60, CooldownBucketType.Channel | CooldownBucketType.User)]
  public async Task DoStats(CommandContext ctx, [Description("Specific channel for the stats")] DiscordChannel channel) {
    await GenerateStats(ctx, channel, 100);
  }
  [Command("stats")]
  [Description("Provides server stats for specific channels")]
  [Cooldown(1, 60, CooldownBucketType.Channel | CooldownBucketType.User)]
  public async Task DoStats(CommandContext ctx, [Description("Specific channel for the stats")] DiscordChannel channel, [Description("Number of messages to check")] int numMessages) {
    await GenerateStats(ctx, channel, numMessages);
  }
  [Command("stats")]
  [Description("Provides server stats for specific channels")]
  [Cooldown(1, 60, CooldownBucketType.Channel | CooldownBucketType.User)]
  public async Task DoStats(CommandContext ctx, [Description("Number of messages to check")] int numMessages) {
    await GenerateStats(ctx, null, numMessages);
  }

  public async Task GenerateStats(CommandContext ctx, DiscordChannel channel, int numMessages) {
    Utils.LogUserCommand(ctx);
    try {
      DateTime start = DateTime.Now;
      if (numMessages > 2000) numMessages = 2000;

      ulong[] channelIDs;
      string[] channelNames;
      if (channel == null) {
        channelIDs = defChannelIDs;
        channelNames = defChannelNames;
      }
      else {
        channelIDs = new ulong[] { channel.Id };
        channelNames = new string[] { channel.Name };
      }

      DiscordGuild g = ctx.Guild;
      string title = Utils.GetEmojiSnowflakeID(Utils.GetEmoji(EmojiEnum.UnitedProgramming)) + " United Programming Statistics";
      string description = " ---- Fetching data 0/" + channelIDs.Length + " ---- Channel " + channelNames[0] + " ---- ";

      var e = Utils.BuildEmbed(title, description, DiscordColor.Black);
      int fieldPos = e.Fields.Count - 1;
      DiscordMessage m = await Utils.LogEmbed(e, ctx, true);


      int step = 0;
      await Task.Delay(120);
      Dictionary<ulong, int> mentioners = new Dictionary<ulong, int>();
      Dictionary<ulong, int> mentioneds = new Dictionary<ulong, int>();
      IReadOnlyDictionary<ulong, DiscordChannel> cs = g.Channels;
      int mentioned = 0;
      foreach (ulong cid in channelIDs) {
        DiscordChannel c = g.GetChannel(cid);
        if (c.Type != ChannelType.Text) continue;
        System.Console.WriteLine("Scanning channel " + c.Name + " for stats: " + numMessages + " messages");

        IReadOnlyList<DiscordMessage> msgs = await c.GetMessagesAsync(1500);
        await Task.Delay(200);

        foreach (DiscordMessage dm in msgs) {
          IReadOnlyList<DiscordRole> rolesMentioned = dm.MentionedRoles;
          foreach (DiscordRole r in rolesMentioned) {
            if (r.Id == 831050318171078718ul) {
              if (mentioners.ContainsKey(dm.Author.Id)) mentioners[dm.Author.Id]++;
              else mentioners[dm.Author.Id] = 1;
              mentioned++;
            }
          }
          IReadOnlyList<DiscordUser> usersMentioned = dm.MentionedUsers;
          foreach (DiscordUser r in usersMentioned) {
            if (mentioneds.ContainsKey(dm.Author.Id)) mentioneds[dm.Author.Id]++;
            else mentioneds[dm.Author.Id] = 1;
          }
        }

        step++;
        if (step < channelIDs.Length) {
          e.Description = " ---- Fetching data " + step + "/" + channelIDs.Length + " ---- Channel " + channelNames[step] + " ---- ";
          await m.ModifyAsync(e.Build());
          await Task.Delay(200);
        }
      }

      // -------- Construct final message

      e.Description = " ----  ----  ----  ---- ";
      int? m1 = g.ApproximateMemberCount;
      int m2 = g.MemberCount;
      string members = (m1 == null) ? m2.ToString() : (m1 + "/" + m2);
      e.AddField("Members", members, true);
      int? p1 = g.ApproximatePresenceCount;
      if (p1 != null) e.AddField("Presence", p1.ToString(), true);
      int? s1 = g.PremiumSubscriptionCount;
      if (s1 != null) e.AddField("Boosters", s1.ToString(), true);

      double days = (DateTime.Now - g.CreationTimestamp.UtcDateTime).TotalDays;
      e.AddField("Server created", (int)days + " days ago", true);
      double dailyms = m2 / days;
      e.AddField("Daily mebers", dailyms.ToString("N1") + " member per day", true);

      e.AddField(" ---- Helper was mentioned ---- ", mentioned + " times", false);

      List<Mentioner> ms = new List<Mentioner>();
      foreach (ulong id in mentioners.Keys)
        ms.Add(new Mentioner { member = id, num = mentioners[id] });
      ms.Sort((a, b) => { return b.num.CompareTo(a.num); });

      for (int i = 0; i < 8; i++) {
        if (i >= ms.Count) break;
        DiscordMember dm = await g.GetMemberAsync(ms[i].member);
        e.AddField(dm.DisplayName, "Called " + ms[i].num + " times", true);
      }


      e.AddField(" ---- Most mentioned people ---- ", mentioneds.Count + " total", false);
      List<Mentioner> md = new List<Mentioner>();
      foreach (ulong id in mentioneds.Keys)
        md.Add(new Mentioner { member = id, num = mentioneds[id] });
      md.Sort((a, b) => { return b.num.CompareTo(a.num); });

      for (int i = 0; i < 8; i++) {
        if (i >= md.Count) break;
        DiscordMember dm = await g.GetMemberAsync(md[i].member);
        e.AddField(dm.DisplayName, "Mentioned " + md[i].num + " times", true);
      }

      double time = (DateTime.Now - start).TotalMilliseconds / 1000;
      if (channelIDs.Length == 1)
        e.WithFooter("Statistics from channel " + channelNames[0] + " and " + numMessages + " messages per channel.\nGenerated in " + time.ToString("N1") + " seconds");
      else
        e.WithFooter("Statistics from " + channelIDs.Length + " channels and " + numMessages + " messages per channel.\nGenerated in " + time.ToString("N1") + " seconds");

      await m.ModifyAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Stats", ex));
    }
  }

  public class Mentioner {
    public ulong member;
    public int num;
  }


}
