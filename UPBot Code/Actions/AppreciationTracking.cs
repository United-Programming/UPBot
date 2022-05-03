using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
  [Aliases("Ranking", "Ranks")]
  [Description("It shows the statistics for users")]
  public async Task ShowAppreciationCommand(CommandContext ctx) {
    if (ctx.Guild == null) return;
    try {
      ulong gid = ctx.Guild.Id;
      WhatToTrack wtt = Configs.WhatToTracks[gid];

      if (Configs.WhatToTracks[gid] == WhatToTrack.None) return;
      Utils.LogUserCommand(ctx);

      DiscordEmbedBuilder e = Utils.BuildEmbed("Appreciation", "These are the most appreciated people of this server", DiscordColor.Azure);

      List<Reputation> vals = Configs.GetReputations(gid).ToList();

      if (wtt.HasFlag(WhatToTrack.Thanks)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        e.AddField("Thanks --------------------", "For receving a message with _Thanks_ or _Thank you_", false);

        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Tnk == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            Configs.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Thanks: _" + r.Tnk + "_");
        }
      }

      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        vals.Sort((a, b) => { return b.Rep.CompareTo(a.Rep); });
        string emjs = "";
        foreach (ReputationEmoji emj in Configs.RepEmojis[gid].Values) {
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
            Configs.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Reputation: _" + r.Rep + "_", true);
        }
      }

      if (wtt.HasFlag(WhatToTrack.Fun)) {
        vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
        string emjs = "";
        foreach (ReputationEmoji emj in Configs.RepEmojis[gid].Values) {
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
            Configs.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Fun: _" + r.Tnk + "_", true);
        }
      }

      if (wtt.HasFlag(WhatToTrack.Rank)) {
        // Calculate the rank of every user, and show the top 10
        List<UserRank> ranks = CalculateRanks(ctx.Guild);
        e.AddField("Ranking --------------------", "For global activity on the server", false);

        for (int i = 0; i < 10; i++) {
          if (i >= ranks.Count) break;
          UserRank r = ranks[i];
          if (r.Lev == 0) break;
          e.AddField(r.Name, $"Rank: #_{i+1}_ xp_{r.Exp}_", true);
        }
      }

      if (wtt.HasFlag(WhatToTrack.Mention)) {
        vals.Sort((a, b) => { return b.Men.CompareTo(a.Men); });
        e.AddField("Mentions --------------------", "For being mentioned (not in replys)", false);

        for (int i = 0; i < 10; i++) {
          if (i >= vals.Count) break;
          Reputation r = vals[i];
          if (r.Men == 0) break;
          string u = Utils.GetSafeMemberName(ctx, r.User);
          if (u == null) { // Remove
            Configs.Reputations[gid].Remove(r.User);
            Database.Delete(r);
            continue;
          }
          e.AddField(u, "Mentions: _" + r.Men + "_", true);
        }
      }


      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Appreciation", ex));
    }
  }

  [Command("Rank")]
  [Description("Shows your own rank")]
  public async Task ShowRankCommand(CommandContext ctx) {
    if (ctx.Guild == null) return;
    await ShowRankCommand(ctx, ctx.Member);
  }

  [Command("Rank")]
  [Description("Shows your own rank")]
  public async Task ShowRankCommand(CommandContext ctx, [Description("Member for the rank")] DiscordMember user) {
    try {
      ulong gid = ctx.Guild.Id;
      if (!Configs.WhatToTracks[gid].HasFlag(WhatToTrack.Rank)) return;
      Utils.LogUserCommand(ctx);

      List<UserRank> ranks = CalculateRanks(ctx.Guild);
      // Let find ourself and our position
      int pos = 0;
      UserRank rank = null;
      foreach (UserRank r in ranks) {
        if (r.Id == ctx.Member.Id) {
          rank = r;
          break;
        }
        pos++;
      }

      if (rank == null) await ctx.Message.RespondAsync("No rank for user: " + user.DisplayName);
      else {
        long exp = ranks[pos].Exp;
        long lev = ranks[pos].Lev;
        long nextlev = lev + 1;
        long minExp4Lev = ((lev * lev * lev * lev) + 9);
        long maxExp4Lev = ((nextlev * nextlev * nextlev * nextlev) + 9);
        if (minExp4Lev < 10) minExp4Lev = 0;

        string file = GenerateRankImage(user.DisplayName, lev, exp, minExp4Lev, maxExp4Lev, pos + 1);
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        await Utils.DeleteFileDelayed(10, file);
        await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithFiles(new Dictionary<string, Stream>() { { file, fs } }));
      }


    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Rank", ex));
    }
  }

  private static readonly Dictionary<ulong, Dictionary<ulong, LastPosters>> LastMemberPerGuildPerChannels = new Dictionary<ulong, Dictionary<ulong, LastPosters>>();

  internal static Task ThanksAdded(DiscordClient _, MessageCreateEventArgs args) {
    if (args.Guild == null) return Task.FromResult(0);
    try {
      if (args.Author.IsBot) Task.FromResult(0);
      ulong gid = args.Guild.Id;
      // Are we tracking this guild and is the tracking active?
      WhatToTrack wtt = Configs.WhatToTracks[gid];
      if (wtt == WhatToTrack.None) return Task.FromResult(0);

      if (wtt.HasFlag(WhatToTrack.Thanks)) CheckThanks(args.Message);
      if (wtt.HasFlag(WhatToTrack.Rank) && !args.Author.IsBot) CheckRanks(args.Guild.Id, args.Message);
      if (wtt.HasFlag(WhatToTrack.Mention) && !args.Author.IsBot) CheckMentions(args.Guild.Id, args.Message);

    } catch (Exception ex) {
      Utils.Log("Error in ThanksAdded: " + ex.Message, args.Guild.Name);
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
        foreach (DiscordMessage m in msgs) {
          if (m.Author.IsBot) continue;
          ulong oid = m.Author.Id;
          if (oid != authorId) {
            Reputation r = Configs.GetReputation(gid, oid);
            r.Tnk++;
            Database.Update(r);
            return;
          }
        }
      } else if (msg.Reference != null && !msg.Reference.Message.Author.IsBot) { // By reference
        ulong oid = msg.Reference.Message.Author.Id;
        if (oid != authorId) {
          Reputation r = Configs.GetReputation(gid, oid);
          r.Tnk++;
          Database.Update(r);
          return;
        }
      } else { // Mentioned
        foreach (var usr in msg.MentionedUsers) {
          if (usr.IsBot) continue;
          ulong oid = usr.Id;
          if (oid != authorId) {
            Reputation r = Configs.GetReputation(gid, oid);
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
      Reputation r = Configs.GetReputation(gid, oid);
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
      Utils.Log("Error in CheckRanks: " + ex.Message, msg.Channel.Guild.Name);
    }
  }

  static void CheckMentions(ulong gid, DiscordMessage msg) {
    try {
      ulong oid = msg.Author.Id;
      ulong rid = 0;
      if (msg.Reference != null) rid = msg.Reference.Message.Author.Id;
      foreach (var m in msg.MentionedUsers) {
        if (m.Id == oid || m.IsBot || m.Id == rid) continue;
        Reputation r = Configs.GetReputation(gid, m.Id);
        r.Men++;
        Database.Add(r);
      }

    } catch (Exception ex) {
      Utils.Log("Error in CheckMentions: " + ex.Message, msg.Channel.Guild.Name);
    }
  }




  internal static Task ReactionAdded(DiscordClient _, MessageReactionAddEventArgs mr) {
    try {
      if (mr.User.IsBot) return Task.FromResult(0);
      ulong gid = mr.Guild.Id;
      // Are we tracking this guild and is the tracking active?
      WhatToTrack wtt = Configs.WhatToTracks[gid];
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
      if (!Configs.RepEmojis[gid].ContainsKey(key)) return Task.FromResult(0);
      ReputationEmoji rem = Configs.RepEmojis[gid][key];

      if (wtt.HasFlag(WhatToTrack.Reputation) && rem.HasFlag(WhatToTrack.Reputation)) {
        Reputation r = Configs.GetReputation(gid, authorId);
        r.Rep++;
        Database.Update(r);
      }

      if (wtt.HasFlag(WhatToTrack.Fun) && rem.HasFlag(WhatToTrack.Fun)) {
        Reputation r = Configs.GetReputation(gid, authorId);
        r.Fun++;
        Database.Update(r);
      }

    } catch (Exception ex) {
      Utils.Log("Error in ReactionAdded: " + ex.Message, mr.Guild.Name);
    }
    return Task.FromResult(0);
  }


  internal static List<UserRank> CalculateRanks(DiscordGuild guild) {
    List<UserRank> ranks = new List<UserRank>();

    IReadOnlyCollection<Reputation> reps = Configs.Reputations[guild.Id].Values;
    foreach (Reputation r in reps) {
      int exp = (int)Math.Round(1.25 * r.Ran + 2.5 * r.Rep + 2.5 * r.Fun + 3.5 * r.Tnk + 3 * r.Men);
      int lev = (int)Math.Floor(Math.Pow(exp - 9.0, .25));
      if (lev < 0) lev = 0;
      DiscordUser du = guild.GetMemberAsync(r.User).Result;
      if (du == null) continue;
      ranks.Add(new UserRank() { Name = du.Username, Id = r.User, Exp = exp, Lev = lev });
    }
    ranks.Sort((a,b) => {  return b.Exp.CompareTo(a.Exp); });
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







  string GenerateRankImage(string user, long lev, long exp, long minlev, long maxlev, int pos) {
    try {
      using Bitmap b = new Bitmap(400, 150);
      using (Graphics g = Graphics.FromImage(b)) {
        Font f = new Font("Times New Roman", 20, FontStyle.Bold, GraphicsUnit.Pixel);
        Font f2 = new Font("Times New Roman", 16, FontStyle.Bold, GraphicsUnit.Pixel);

        g.Clear(Color.Transparent);
        Pen back = new Pen(Color.FromArgb(30, 30, 28));
        Pen lines = new Pen(Color.FromArgb(70, 70, 72));
        Brush fill = new SolidBrush(back.Color);
        Brush txtl = new SolidBrush(Color.FromArgb(150, 150, 102));
        Brush txte = new SolidBrush(Color.FromArgb(170, 182, 150));
        Brush txty = new SolidBrush(Color.FromArgb(204, 212, 170));
        Brush txtb = new SolidBrush(Color.FromArgb(50, 50, 107));

        Brush expfillg = new SolidBrush(Color.FromArgb(106, 188, 96));
        Pen explineg = new Pen(Color.FromArgb(106, 248, 169));
        Brush expfillb = new SolidBrush(Color.FromArgb(100, 96, 180));
        Pen explineb = new Pen(Color.FromArgb(106, 169, 248));

        /*
             ----------------------------------------
            | Rank for <NAME>                        |
            |   Level: <rank>                        |
            |   Experience <abcd>/<defg>             |
            |   #<position>/<total counted>          |
             ----------------------------------------

        <rank> is the calculated level
        <abcd> is the number we calculate
        <defg> is the number for the next level
        */


        DrawBox(g, fill, lines, 0, 0, 400, 150);

        g.DrawString("Rank for", f, txtl, 16, 16);
        g.DrawString(user, f, txty, 105, 16);

        g.DrawString("Level:", f, txtl, 32, 48);
        g.DrawString(lev.ToString(), f, txte, 160, 48);

        int perc = (int)(222 * (1.0 * exp - minlev) / (maxlev - minlev));


        g.FillRectangle(expfillg, new Rectangle(160, 80, 224, 24));
        g.DrawRectangle(explineg, new Rectangle(160, 80, 224, 24));
        g.FillRectangle(expfillb, new Rectangle(161, 81, perc, 22));
        g.DrawRectangle(explineb, new Rectangle(161, 81, perc, 22));

        g.DrawString("Experience:", f, txtl, 32, 80);
        g.DrawString(exp + " / " + maxlev, f2, txtb, 192, 84);

        g.DrawString("Position:", f, txtl, 32, 112);
        g.DrawString("#" + pos, f, txte, 160, 112);
      }
      string rndName = "RankImage" + DateTime.Now.Second + "Tmp" + DateTime.Now.Millisecond + ".png";
      b.Save(rndName, System.Drawing.Imaging.ImageFormat.Png);
      return rndName;
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
      throw ex;
    }
  }

  void DrawBox(Graphics g, Brush fill, Pen border, int t, int l, int r, int b) {
    int w = r - l;
    int h = b - t;
    int sw = w / 12;
    int sh = h / 10;
    Rectangle box = new Rectangle(t, l, t + sw, l + sh);
    GraphicsPath path = new GraphicsPath();
    box.X = l; box.Y = t;
    path.AddArc(box, 180, 90); // top left arc  

    box.X = w - sw - 1; box.Y = t;
    path.AddArc(box, 270, 90); // top right arc  

    box.X = w - sw - 1; box.Y = h - sh - 1;
    path.AddArc(box, 0, 90); // bottom right arc

    box.X = t; box.Y = h - sh - 1;
    path.AddArc(box, 90, 90); // bottom left arc 

    path.CloseFigure();
    g.FillPath(fill, path);
    g.DrawPath(border, path);
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
  Mention = 16
}

public class UserRank {
  public string Name;
  public ulong Id;
  public int Exp;
  public int Lev;
}
