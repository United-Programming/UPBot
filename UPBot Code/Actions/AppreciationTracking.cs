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
      e.AddField("Reputation ----------------", "For receving these emojis in the posts: <:OK:830907665869570088>👍❤️🥰😍🤩😘💯<:whatthisguysaid:840702597216337990>", false);
      vals.Sort((a, b) => { return b.Rep.CompareTo(a.Rep); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Rep == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = ctx.Guild.GetMemberAsync(r.User).Result.DisplayName;
        }
        e.AddField(users[r.User], "Reputation: _" + r.Rep + "_", true);
      }

      e.AddField("Fun -----------------------", "For receving these emojis in the posts: 😀😃😄😁😆😅🤣😂🙂🙃😉😊😇<:StrongSmile:830907626928996454>", false);
      vals.Sort((a, b) => { return b.Fun.CompareTo(a.Fun); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Fun == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = ctx.Guild.GetMemberAsync(r.User).Result.DisplayName;
        }
        e.AddField(users[r.User], "Fun: _" + r.Fun + "_", (i < vals.Count - 1 && i < 5));
      }

      e.AddField("Thanks --------------------", "For receving a message with _Thanks_ or _Thank you_", false);
      vals.Sort((a, b) => { return b.Tnk.CompareTo(a.Tnk); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.Tnk == 0) break;
        if (!users.ContainsKey(r.User)) {
          users[r.User] = ctx.Guild.GetMemberAsync(r.User).Result.DisplayName;
        }
        e.AddField(users[r.User], "Thanks: _" + r.Tnk + "_", (i < vals.Count - 1 && i < 5));
      }

      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Appreciation", ex));
    }
  }

  internal static Task ThanksAdded(DiscordClient sender, MessageCreateEventArgs args) {
    try {
      if (args.Message.Reference == null && (args.Message.MentionedUsers == null || args.Message.MentionedUsers.Count == 0)) return Task.FromResult(0);

      string msg = args.Message.Content.ToLowerInvariant();
      if (thanks.IsMatch(msg) || thankyou.IsMatch(msg) || thank2you.IsMatch(msg)) { // Add thanks
        if (thank4n.IsMatch(msg)) return Task.FromResult(0);
        if (GetTracking()) return Task.FromResult(0);

        IReadOnlyList<DiscordUser> mentions = args.Message.MentionedUsers;
        ulong authorId = args.Message.Author.Id;
        ulong refAuthorId = args.Message.Reference != null ? args.Message.Reference.Message.Author.Id : 0;
        if (mentions != null)
          foreach (DiscordUser u in mentions)
            if (u.Id != authorId && u.Id != refAuthorId) tracking.AlterThankYou(u.Id);
        if (args.Message.Reference != null)
          if (args.Message.Reference.Message.Author.Id != authorId) tracking.AlterThankYou(args.Message.Reference.Message.Author.Id);
      }

      return Task.FromResult(0);
    } catch (Exception ex) {
      Utils.Log("Error in ThanksAdded: " + ex.Message);
      return Task.FromResult(0);
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
      Utils.Log("Error in ReacionAdded: " + ex.Message);
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
      Utils.Log("Error in ReacionAdded: " + ex.Message);
      return Task.FromResult(0);
    }
  }

  static Task HandleReactions(ulong emojiId, string emojiName, ulong authorId, bool added) {
    // check if emoji is :smile: :rolf: :strongsmil: (find valid emojis -> increase fun level of user
    if (emojiName == "😀" ||
        emojiName == "😃" ||
        emojiName == "😄" ||
        emojiName == "😁" ||
        emojiName == "😆" ||
        emojiName == "😅" ||
        emojiName == "🤣" ||
        emojiName == "😂" ||
        emojiName == "🙂" ||
        emojiName == "🙃" ||
        emojiName == "😉" ||
        emojiName == "😊" ||
        emojiName == "😇" ||
        emojiId == 830907626928996454ul || // :StrongSmile: 
        false) { // just to keep the other lines aligned
      if (GetTracking()) return Task.FromResult(0);
      tracking.AlterFun(authorId, added);
    }

    // check if emoji is :OK: or :ThumbsUp: -> Increase reputation for user
    if (emojiId == 830907665869570088ul || // :OK:
        emojiName == "👍" || // :thumbsup:
        emojiName == "❤️" || // :hearth:
        emojiName == "🥰" || // :hearth:
        emojiName == "😍" || // :hearth:
        emojiName == "🤩" || // :hearth:
        emojiName == "😘" || // :hearth:
        emojiName == "💯" || // :100:
        emojiId == 840702597216337990ul || // :whatthisguysaid:
        emojiId == 552147917876625419ul || // :thoose:
        false) { // just to keep the other lines aligned
      if (GetTracking()) return Task.FromResult(0);
      tracking.AlterRep(authorId, added);
    }

    return Task.Delay(10);
  }
}
