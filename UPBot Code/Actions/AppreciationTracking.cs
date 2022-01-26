using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class AppreciationTracking : BaseCommandModule {
  readonly static Regex thanks = new Regex("(^|[^a-z0-9_])thanks($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thankyou = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank2you = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}to\\s{0,1}you($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  readonly static Regex thank4n = new Regex("(^|[^a-z0-9_])thanks{0,1}\\s{0,1}for\\s{0,1}nothing($|[^a-z0-9_])", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
  public static ReputationTracking tracking = null;

  private static bool GetTracking() {
    if (tracking != null) return false;
    tracking = new ReputationTracking();
    return (tracking == null);
  }

  [Command("Appreciation")]
  [Description("It shows the statistics for users")]
  public async Task ShowAppreciationCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      if (GetTracking()) return;
      DiscordEmbedBuilder e = Utils.BuildEmbed("Appreciation", "These are the most appreciated people of this server", DiscordColor.Azure);

      List<Reputation> vals = new List<Reputation>(tracking.GetReputation());
      Dictionary<ulong, string> users = new Dictionary<ulong, string>();
      string emjs = "";
      foreach (string emj in SetupModule.RepSEmojis) emjs += emj;
      foreach (ulong emj in SetupModule.RepIEmojis) emjs += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj));
      e.AddField("Reputation ----------------", "For receving these emojis in the posts: " + emjs, false);
      vals.Sort((a, b) => { return b.Rep.CompareTo(a.Rep); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Rep == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = Utils.GetSafeMemberName(ctx, r.User);
          if (users[r.User] == null) {
            tracking.RemoveEntry(r.User); // Remove the entry
            users.Remove(r.User);
          }
        }
        if (users.ContainsKey(r.User)) e.AddField(users[r.User], "Reputation: _" + r.Rep + "_", true);
      }

      emjs = "";
      foreach (string emj in SetupModule.FunSEmojis) emjs += emj;
      foreach (ulong emj in SetupModule.FunIEmojis) emjs += Utils.GetEmojiSnowflakeID(Utils.GetEmoji(emj));
      e.AddField("Fun -----------------------", "For receving these emojis in the posts: " + emjs, false);
      vals.Sort((a, b) => { return b.Fun.CompareTo(a.Fun); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Fun == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = Utils.GetSafeMemberName(ctx, r.User);
          users[r.User] = Utils.GetSafeMemberName(ctx, r.User);
          if (users[r.User] == null) {
            tracking.RemoveEntry(r.User); // Remove the entry
            users.Remove(r.User);
          }
        }
        if (users.ContainsKey(r.User)) e.AddField(users[r.User], "Fun: _" + r.Fun + "_", (i < vals.Count - 1 && i < 5));
      }

      e.AddField("Thanks --------------------", "For receving a message with _Thanks_ or _Thank you_", false);
      vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Tnk == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = Utils.GetSafeMemberName(ctx, r.User);
          if (users[r.User] == null) {
            tracking.RemoveEntry(r.User); // Remove the entry
            users.Remove(r.User);
          }
        }
        if (users.ContainsKey(r.User)) e.AddField(users[r.User], "Thanks: _" + r.Tnk + "_", (i < vals.Count - 1 && i < 5));
      }

      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Appreciation", ex));
    }
  }


  private static Dictionary<ulong, Dictionary<ulong, LastPosters>> LastMemberPerGuildPerChannels = null;

  internal static void InitChannelList() {
    Dictionary<ulong, Dictionary<ulong, DiscordChannel>> channels = Utils.GetAllChannelsFromGuilds(); // FIXME this should be specific for each server
    LastMemberPerGuildPerChannels = new Dictionary<ulong, Dictionary<ulong, LastPosters>>();
    foreach (ulong gid in channels.Keys) {
      LastMemberPerGuildPerChannels[gid] = new Dictionary<ulong, LastPosters>();
      foreach (ulong cid in channels[gid].Keys) LastMemberPerGuildPerChannels[gid][cid] = new LastPosters();
    }
  }

  internal static Task ThanksAdded(DiscordClient sender, MessageCreateEventArgs args) {
    try {
      string msg = args.Message.Content.ToLowerInvariant();
      ulong memberid = args.Message.Author.Id;
      ulong channelid = args.Message.ChannelId;
      ulong guildid = args.Guild.Id;
      if (LastMemberPerGuildPerChannels == null) InitChannelList();
      LastPosters lp = LastMemberPerGuildPerChannels[guildid][channelid];
      lp.Add(memberid);

      if (thanks.IsMatch(msg) || thankyou.IsMatch(msg) || thank2you.IsMatch(msg)) { // Add thanks
        if (thank4n.IsMatch(msg)) return Task.FromResult(0);
        if (GetTracking()) return Task.FromResult(0);

        DiscordMessage theMsg = args.Message;
        ulong authorId = theMsg.Author.Id;
        if (theMsg.Reference == null && (theMsg.MentionedUsers == null || theMsg.MentionedUsers.Count == 0)) {
          if (lp.secondLast != 0 || lp.secondLast != 875701548301299743ul)
            tracking.AlterThankYou(lp.secondLast);
          else {
            // Unrelated thank you, get the previous message and check /*
            IReadOnlyList<DiscordMessage> msgs = theMsg.Channel.GetMessagesBeforeAsync(theMsg.Id, 2).Result;
            theMsg = null;
            foreach (DiscordMessage m in msgs)
              if (m.Author.Id != authorId) {
                theMsg = m;
                break;
              }
          }
          if (theMsg == null) return Task.FromResult(0);
        }

        IReadOnlyList<DiscordUser> mentions = theMsg.MentionedUsers;
        ulong refAuthorId = theMsg.Reference != null ? theMsg.Reference.Message.Author.Id : 0;
        if (mentions != null)
          foreach (DiscordUser u in mentions)
            if (u.Id != authorId && u.Id != refAuthorId) tracking.AlterThankYou(u.Id);
        if (theMsg.Reference != null)
          if (theMsg.Reference.Message.Author.Id != authorId) tracking.AlterThankYou(theMsg.Reference.Message.Author.Id);
      }

      return Task.FromResult(0);
    } catch (Exception ex) {
      Utils.Log("Error in ThanksAdded: " + ex.Message);
      return Task.FromResult(0);
    }
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



  internal static Task ReacionAdded(DiscordClient sender, MessageReactionAddEventArgs a) {
    try {
      ulong emojiId = a.Emoji.Id;
      string emojiName = a.Emoji.Name;

      DiscordUser author = a.Message.Author;
      if (author == null) {
        ulong msgId = a.Message.Id;
        ulong chId = a.Message.ChannelId;
        DiscordChannel c = a.Guild.GetChannel(chId);
        DiscordMessage m = c.GetMessageAsync(msgId).Result;
        author = m.Author;
      }
      ulong authorId = author.Id;
      if (authorId == a.User.Id) return Task.Delay(10); // If member is equal to author ignore (no self emojis)
      return HandleReactions(emojiId, emojiName, authorId, true);
    } catch (Exception ex) {
      Utils.Log("Error in AppreciationTracking.ReatcionAdded: " + ex.Message);
      return Task.FromResult(0);
    }
  }

  internal static Task ReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs a) {
    try {
      ulong emojiId = a.Emoji.Id;
      string emojiName = a.Emoji.Name;

      DiscordUser author = a.Message.Author;
      if (author == null) {
        ulong msgId = a.Message.Id;
        ulong chId = a.Message.ChannelId;
        DiscordChannel c = a.Guild.GetChannel(chId);
        DiscordMessage m = c.GetMessageAsync(msgId).Result;
        author = m.Author;
      }
      ulong authorId = author.Id;
      if (authorId == a.User.Id) return Task.Delay(10); // If member is equal to author ignore (no self emojis)
      return HandleReactions(emojiId, emojiName, authorId, false);
    } catch (Exception ex) {
      Utils.Log("Error in AppreciationTracking.ReactionAdded: " + ex.Message);
      return Task.FromResult(0);
    }
  }

  static Task HandleReactions(ulong emojiId, string emojiName, ulong authorId, bool added) {
    if (SetupModule.FunIEmojis == null) return Task.Delay(10);
    // check if emoji is :smile: :rolf: :strongsmil: (find valid emojis -> increase fun level of user
    if ((emojiId != 0 && SetupModule.FunIEmojis.Contains(emojiId)) || (!string.IsNullOrEmpty(emojiName) && SetupModule.FunSEmojis.Contains(emojiName))) {
      if (GetTracking()) return Task.FromResult(0);
      tracking.AlterFun(authorId, added);
    }

    // check if emoji is :OK: or :ThumbsUp: -> Increase reputation for user
    if ((emojiId != 0 && SetupModule.RepIEmojis.Contains(emojiId)) || (!string.IsNullOrEmpty(emojiName) && SetupModule.RepSEmojis.Contains(emojiName))) {
      if (GetTracking()) return Task.FromResult(0);
      tracking.AlterRep(authorId, added);
    }

    return Task.Delay(10);
  }

}
