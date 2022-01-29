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

public class AppreciationTracking : BaseCommandModule {
  readonly static Regex thanks = new Regex("(^|[^a-z0-9_])thanks($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thankyou = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank2you = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}to\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank4n = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}for\\s{0,1}nothing($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

  [Command("Appreciation")]
  [Aliases("Ranking")]
  [Description("It shows the statistics for users")]
  public async Task ShowAppreciationCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      ulong gid = ctx.Guild.Id;
      WhatToTrack wtt = SetupModule.WhatToTracks[gid];

      if (SetupModule.WhatToTracks[gid] == WhatToTrack.None) return;

      DiscordEmbedBuilder e = Utils.BuildEmbed("Appreciation", "These are the most appreciated people of this server", DiscordColor.Azure);

      List<Reputation> vals = SetupModule.GetReputations(gid).ToList();

      if (wtt.HasFlag(WhatToTrack.Thanks)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        e.AddField("Thanks --------------------", "For receving a message with _Thanks_ or _Thank you_", false);
        
        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Tnk == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            SetupModule.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Thanks: _" + r.Tnk + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        vals.Sort((a, b) => { return b.Rep.CompareTo(a.Rep); });
        string emjs = "";
        foreach (RepEmoji emj in SetupModule.RepEmojis[gid]) {
          if (emj.sid != null) emjs += emj;
          else emjs += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj.lid));
        }       
        e.AddField("Reputation ----------------", "For receving these emojis: " + emjs, false);


        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Rep == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            SetupModule.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Reputation: _" + r.Rep + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Fun)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        string emjs = "";
        foreach (RepEmoji emj in SetupModule.FunEmojis[gid]) {
          if (emj.sid != null) emjs += emj;
          else emjs += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj.lid));
        }
        e.AddField("Fun --------------------", "For receving these emojis: " + emjs, false);

        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Tnk == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            SetupModule.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Fun: _" + r.Tnk + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Rank)) {
        // Calculate the rank of every user, and show the top 10
        // FIXME
        /*
lev = floor(
    1.25 * numword ^ 0.25 + 
    1.5 * numappr ^ 0.27
    1.5 * numfun ^ 0.27
    1.5 * numthanks ^ 0.27
)        
        */
      }


      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Appreciation", ex));
    }
  }

  private static Dictionary<ulong, Dictionary<ulong, LastPosters>> LastMemberPerGuildPerChannels = new Dictionary<ulong, Dictionary<ulong, LastPosters>>();


  internal static Task ThanksAdded(DiscordClient sender, MessageCreateEventArgs args) {
    try {
      if (args.Author.IsBot) Task.FromResult(0);
      ulong gid = args.Guild.Id;
      // Are we tracking this guild and is the tracking active?
      WhatToTrack wtt = SetupModule.WhatToTracks[gid];
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
            Reputation r = SetupModule.GetReputation(gid, oid);
            r.Tnk++;
            Database.Update(r);
            return;
          }
        }
      } else if (msg.Reference != null) { // By reference
        ulong oid = msg.Reference.Message.Author.Id;
        if (oid != authorId) {
          Reputation r = SetupModule.GetReputation(gid, oid);
          r.Tnk++;
          Database.Update(r);
          return;
        }
      } else { // Mentioned
        foreach (var usr in msg.MentionedUsers) {
          ulong oid = usr.Id;
          if (oid != authorId) {
            Reputation r = SetupModule.GetReputation(gid, oid);
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
      Reputation r = SetupModule.GetReputation(gid, oid);
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
      WhatToTrack wtt = SetupModule.WhatToTracks[gid];
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


      RepEmoji rem = new RepEmoji(emojiId, emojiName);
      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        // Do we have this emoji in the Reputation list for the server?
        if (SetupModule.RepEmojis[gid].Contains(rem)) {
          Reputation r = SetupModule.GetReputation(gid, authorId);
          r.Rep++;
          Database.Update(r);
        }
      }

      if (wtt.HasFlag(WhatToTrack.Fun)) {
        // Do we have this emoji in the Reputation list for the server?
        if (SetupModule.RepEmojis[gid].Contains(rem)) {
          Reputation r = SetupModule.GetReputation(gid, authorId);
          r.Fun++;
          Database.Update(r);
        }
      }

    } catch (Exception ex) {
      Utils.Log("Error in ReactionAdded: " + ex.Message);
    }
    return Task.FromResult(0);
  }

  static void CheckFun(MessageReactionAddEventArgs mr) {
  }

  static void CheckReputation(MessageReactionAddEventArgs mr) {

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

  public RepEmoji(ulong id1, string id2) {
    lid = id1;
    sid = id2;
  }


  public override int GetHashCode() {
    if (sid == null) return lid.GetHashCode();
    else return sid.GetHashCode();
  }
}

public enum WhatToTrack {
  None = 0,
  Thanks = 1,
  Reputation = 2,
  Fun = 4,
  Rank = 8,
}