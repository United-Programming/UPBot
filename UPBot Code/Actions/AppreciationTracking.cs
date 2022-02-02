using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Used to keep track on specific emojis, thanks, and global number of stuff posted.
/// It is also the base for the ranking system.
/// Each of the 4 features can be enabled individually
/// </summary>
public class AppreciationTracking : BaseCommandModule {
  readonly static Regex thanks = new Regex("(^|[^a-z0-9_])thanks($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thankyou = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank2you = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}to\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank4n = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}for\\s{0,1}nothing($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

  [Command("Appreciation")]
  [Aliases("Ranking")]
  [Description("It shows the statistics for users")]
  public async Task ShowAppreciationCommand(CommandContext ctx) {
    try {
      ulong gid = ctx.Guild.Id;
      WhatToTrack wtt = Setup.WhatToTracks[gid];

      if (Setup.WhatToTracks[gid] == WhatToTrack.None) return;
      Utils.LogUserCommand(ctx);

      DiscordEmbedBuilder e = Utils.BuildEmbed("Appreciation", "These are the most appreciated people of this server", DiscordColor.Azure);

      List<Reputation> vals = Setup.GetReputations(gid).ToList();

      if (wtt.HasFlag(WhatToTrack.Thanks)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        e.AddField("Thanks --------------------", "For receving a message with _Thanks_ or _Thank you_", false);

        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Tnk == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            Setup.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Thanks: _" + r.Tnk + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        vals.Sort((a, b) => { return b.Rep.CompareTo(a.Rep); });
        string emjs = "";
        foreach (ReputationEmoji emj in Setup.RepEmojis[gid].Values) {
          if (emj.HasFlag(WhatToTrack.Reputation))
            emjs += emj.GetEmoji(ctx.Guild);
        }
        e.AddField("Reputation ----------------", "For receving these emojis: " + emjs, false);


        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Rep == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            Setup.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Reputation: _" + r.Rep + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Fun)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        string emjs = "";
        foreach (ReputationEmoji emj in Setup.RepEmojis[gid].Values) {
          if (emj.HasFlag(WhatToTrack.Fun))
            emjs += emj.GetEmoji(ctx.Guild);
        }
        e.AddField("Fun --------------------", "For receving these emojis: " + emjs, false);

        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Tnk == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            Setup.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Fun: _" + r.Tnk + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Rank)) {
        // Calculate the rank of every user, and show the top 10
        List<UserRank> ranks = CalculateRanks(ctx.Guild);
        e.AddField("Ranking --------------------", "For global activity on the server", false);

        for (int i = 0; i < 10; i++) {
          if (i >= ranks.Count) break;
          UserRank r = ranks[i];
          if (r.Score == 0) break;
          e.AddField(r.Name, "Rank: #_" + r.Score + "_");
        }
      }


      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Appreciation", ex));
    }
  }

  [Command("Rank")]
  [Description("Shows your own rank")]
  public async Task ShowRankCommand(CommandContext ctx) {
    await ShowRankCommand(ctx, ctx.Member);
  }

  [Command("Rank")]
  [Description("Shows your own rank")]
  public async Task ShowRankCommand(CommandContext ctx, [Description("Member for the rank")] DiscordMember user) {
    try {
      ulong gid = ctx.Guild.Id;
      if (!Setup.WhatToTracks[gid].HasFlag(WhatToTrack.Rank)) return;
      Utils.LogUserCommand(ctx);

      List<UserRank> ranks = CalculateRanks(ctx.Guild, user.Id);

      if (ranks.Count == 0) await ctx.Message.RespondAsync("No rank for user: " + user.DisplayName);
      else await ctx.Message.RespondAsync("rank: " + ranks[0].Score);

    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Rank", ex));
    }
  }

  private static Dictionary<ulong, Dictionary<ulong, LastPosters>> LastMemberPerGuildPerChannels = new Dictionary<ulong, Dictionary<ulong, LastPosters>>();

  internal static Task ThanksAdded(DiscordClient sender, MessageCreateEventArgs args) {
    try {
      if (args.Author.IsBot) Task.FromResult(0);
      ulong gid = args.Guild.Id;
      // Are we tracking this guild and is the tracking active?
      WhatToTrack wtt = Setup.WhatToTracks[gid];
      if (wtt == WhatToTrack.None) return Task.FromResult(0);

      if (wtt.HasFlag(WhatToTrack.Thanks)) CheckThanks(args.Message);
      if (wtt.HasFlag(WhatToTrack.Rank)) CheckRanks(args.Guild.Id, args.Message);

    } catch (Exception ex) {
      Utils.Log("Error in ThanksAdded: " + ex.Message);
    }
    return Task.FromResult(0);
  }

  static void CheckThanks(DiscordMessage msg) {
    string text = msg.Content.Trim().ToLowerInvariant();
    if (thanks.IsMatch(text) || thankyou.IsMatch(text) || thank2you.IsMatch(text)) { // Add thanks
      if (thank4n.IsMatch(text)) return; // Not what we want

      ulong authorId = msg.Author.Id;
      ulong cid = msg.Channel.Id;
      ulong gid = msg.Channel.Guild.Id;
      if (!LastMemberPerGuildPerChannels.ContainsKey(gid)) LastMemberPerGuildPerChannels[gid] = new Dictionary<ulong, LastPosters>();
      LastPosters lp;
      if (!LastMemberPerGuildPerChannels[gid].ContainsKey(cid)) {
        lp = new LastPosters();
        LastMemberPerGuildPerChannels[gid][cid] = lp;
      } else lp = LastMemberPerGuildPerChannels[gid][cid];
      lp.Add(authorId);

      if (msg.Reference == null && (msg.MentionedUsers == null || msg.MentionedUsers.Count == 0)) { // Get the previous poster
        IReadOnlyList<DiscordMessage> msgs = msg.Channel.GetMessagesBeforeAsync(msg.Id, 3).Result;
        msg = null;
        foreach (DiscordMessage m in msgs) {
          ulong oid = m.Author.Id;
          if (oid != authorId) {
            Reputation r = Setup.GetReputation(gid, oid);
            r.Tnk++;
            Database.Update(r);
            return;
          }
        }
      } else if (msg.Reference != null) { // By reference
        ulong oid = msg.Reference.Message.Author.Id;
        if (oid != authorId) {
          Reputation r = Setup.GetReputation(gid, oid);
          r.Tnk++;
          Database.Update(r);
          return;
        }
      } else { // Mentioned
        foreach (var usr in msg.MentionedUsers) {
          ulong oid = usr.Id;
          if (oid != authorId) {
            Reputation r = Setup.GetReputation(gid, oid);
            r.Tnk++;
            Database.Update(r);
          }
        }
      }
    }
  }

  readonly static TimeSpan aMinute = TimeSpan.FromSeconds(60);

  static void CheckRanks(ulong gid, DiscordMessage msg) {
    try {
      ulong oid = msg.Author.Id;
      Reputation r = Setup.GetReputation(gid, oid);
      if (DateTime.Now - r.LastUpdate < aMinute) return;

      string txt = msg.Content.Trim().Replace("\t", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
      int num = 0;
      foreach (char c in txt)
        if (c == ' ') num++;
      num = (int)Math.Sqrt(num);
      r.Ran += num;
      r.LastUpdate = DateTime.Now;
      Database.Update(r);

    } catch (Exception ex) {
      Utils.Log("Error in CheckRanks: " + ex.Message);
    }
  }


  internal static Task ReactionAdded(DiscordClient sender, MessageReactionAddEventArgs mr) {
    try {
      if (mr.User.IsBot) return Task.FromResult(0);
      ulong gid = mr.Guild.Id;
      // Are we tracking this guild and is the tracking active?
      WhatToTrack wtt = Setup.WhatToTracks[gid];
      if (wtt == WhatToTrack.None || (!wtt.HasFlag(WhatToTrack.Reputation) && !wtt.HasFlag(WhatToTrack.Fun))) return Task.FromResult(0);

      ulong emojiId = mr.Emoji.Id;
      string emojiName = mr.Emoji.Name;

      DiscordUser author = mr.Message.Author;
      if (author == null) {
        ulong msgId = mr.Message.Id;
        ulong chId = mr.Message.ChannelId;
        DiscordChannel c = mr.Guild.GetChannel(chId);
        DiscordMessage m = c.GetMessageAsync(msgId).Result;
        author = m.Author;
      }
      ulong authorId = author.Id;
      if (authorId == mr.User.Id) return Task.Delay(0); // If member is equal to author ignore (no self emojis)


      ulong key = ReputationEmoji.GetKeyValue(gid, emojiId, emojiName);
      if (!Setup.RepEmojis[gid].ContainsKey(key)) return Task.FromResult(0);
      ReputationEmoji rem = Setup.RepEmojis[gid][key];

      if (wtt.HasFlag(WhatToTrack.Reputation) && rem.HasFlag(WhatToTrack.Reputation)) {
        Reputation r = Setup.GetReputation(gid, authorId);
        r.Rep++;
        Database.Update(r);
      }

      if (wtt.HasFlag(WhatToTrack.Fun) && rem.HasFlag(WhatToTrack.Fun)) {
        Reputation r = Setup.GetReputation(gid, authorId);
        r.Fun++;
        Database.Update(r);
      }

    } catch (Exception ex) {
      Utils.Log("Error in ReactionAdded: " + ex.Message);
    }
    return Task.FromResult(0);
  }


  List<UserRank> CalculateRanks(DiscordGuild guild, ulong user = 0) {
    List<UserRank> ranks = new List<UserRank>();

    IReadOnlyCollection<Reputation> reps = Setup.Reputations[guild.Id].Values;
    foreach (Reputation r in reps) {
      double lev = Math.Floor(
                      1.25 * Math.Pow(r.Ran, 0.25) +
                      2.5 * Math.Pow(r.Rep, 0.27) +
                      2.5 * Math.Pow(r.Fun, 0.27) +
                      3.5 * Math.Pow(r.Tnk, 0.27)) - 20;
      if (lev < 0) lev = 0;
      if (r.User == user) {
        DiscordUser usr = guild.GetMemberAsync(r.User).Result;
        if (usr == null) return null;
        ranks.Add(new UserRank() { Name = usr.Username, Id = r.User, Score = (int)lev });
        return ranks;
      }
      DiscordUser du = guild.GetMemberAsync(r.User).Result;
      if (du == null) continue;
      ranks.Add(new UserRank() { Name = du.Username, Id = r.User, Score = (int)lev });
    }
    ranks.Sort((a,b) => {  return b.Score.CompareTo(a.Score); });
    return ranks;
  }

  internal class LastPosters {
    public ulong thirdLast;
    public ulong secondLast;
    public ulong last;

    internal void Add(ulong memberid) {
      if (last == memberid) return;
      thirdLast = secondLast;
      secondLast = last;
      last = memberid;
    }
  }


}

/*
public class RepEmoji {
  public ulong lid;
  public string sid;

  public RepEmoji(ulong id) {
    lid = id;
    sid = null;
  }

  public RepEmoji(string id) {
    sid = id;
    lid = 0;
  }

  public RepEmoji(ReputationEmoji r) {
    lid = r.Lid;
    sid = r.Sid;
  }

  public RepEmoji(ulong id1, string id2) {
    lid = id1;
    sid = id2;
  }


  public override int GetHashCode() {
    if (sid == null) return lid.GetHashCode();
    else return sid.GetHashCode();
  }
}
*/

public enum WhatToTrack {
  None = 0,
  Thanks = 1,
  Reputation = 2,
  Fun = 4,
  Rank = 8,
}

public class UserRank {
  public string Name;
  public ulong Id;
  public int Score;
}
