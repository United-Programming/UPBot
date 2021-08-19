using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class AppreciationTracking : BaseCommandModule {
  static ReputationTracking tracking = null;
  static EmojisForRole emojisForRole = null;
  static bool savingRequested = false;

  public static void Init() {
    tracking = new ReputationTracking(Utils.ConstructPath("Tracking", "Tracking", ".Tracking"));
    emojisForRole = new EmojisForRole(Utils.ConstructPath("Tracking", "EmojisForRole", ".dat"));
  }


  [Command("Appreciation")]
  [Description("It shows the statistics for users")]
  public async Task ShowAppreciationCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      if (tracking == null) {
        tracking = new ReputationTracking(Utils.ConstructPath("Tracking", "Tracking", ".Tracking"));
        if (tracking == null) return;
      }
      DiscordEmbedBuilder e = Utils.BuildEmbed("Appreciation", "These are the most appreciated people of this server (tracking started at " + tracking.GetStartDate() + ")", DiscordColor.Azure);

      List<Reputation> vals = new List<Reputation>(tracking.GetReputation());
      Dictionary<ulong, string> users = new Dictionary<ulong, string>();
      e.AddField("Reputation ----------------", "For receving these emojis in the posts: <:OK:830907665869570088>👍❤️🥰😍🤩😘💯<:whatthisguysaid:840702597216337990>", false);
      vals.Sort((a, b) => { return b.reputation.CompareTo(a.reputation); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.reputation == 0) break;
        if (!users.ContainsKey(r.user)) {
          users[r.user] = ctx.Guild.GetMemberAsync(r.user).Result.DisplayName;
        }
        e.AddField(users[r.user], "Reputation: _" + r.reputation + "_", true);
      }

      e.AddField("Fun -----------------------", "For receving these emojis in the posts: 😀😃😄😁😆😅🤣😂🙂🙃😉😊😇<:StrongSmile:830907626928996454>", false);
      vals.Sort((a, b) => { return b.fun.CompareTo(a.fun); });
      for (int i = 0; i < 6; i++) {
        if (i >= vals.Count) break;
        Reputation r = vals[i];
        if (r.fun == 0) break;
        if (!users.ContainsKey(r.user)) {
          users[r.user] = ctx.Guild.GetMemberAsync(r.user).Result.DisplayName;
        }
        e.AddField(users[r.user], "Fun: _" + r.fun + "_", (i < vals.Count - 1 && i < 5));
      }

      await ctx.Message.RespondAsync(e.Build());
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Appreciation", ex));
    }
  }


  [Command("EmojiForRole")]
  [Aliases("RoleForEmoji")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  [Description("Reply to a message what should add and remove a role when an emoji (any or specific) is added")]
  public async Task EmojiForRoleCommand(CommandContext ctx, [Description("The role to add")] DiscordRole role, [Description("The emoji to watch")] DiscordEmoji emoji) {
    Utils.LogUserCommand(ctx);
    try {
      if (ctx.Message.ReferencedMessage == null) {
        await Utils.BuildEmbedAndExecute("EmojiForRole - Bad parameters", "You have to reply to the message that should be watched!", Utils.Red, ctx, true);
      }
      else {
        string msg = ctx.Message.ReferencedMessage.Content;
        if (msg.Length > 20) msg = msg.Substring(0, 20) + "...";
        if (emojisForRole.AddRemove(ctx.Message.ReferencedMessage, role, emoji)) {
          msg = "The referenced message (_" + msg + "_) will grant/remove the role *" + role.Name + "* when adding/removing the emoji: " + emoji.GetDiscordName();
        }
        else {
          msg = "The message referenced (_" + msg + "_) will not grant anymore the role *" + role.Name + "* when adding the emoji: " + emoji.GetDiscordName();
        }
        Utils.Log(msg);
        await ctx.RespondAsync(msg);
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("EmojiForRole", ex));
    }
  }


  [Command("EmojiForRole")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  public async Task EmojiForRoleCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    await Utils.BuildEmbedAndExecute("EmojiForRole - Bad parameters", "You have to reply to the message that should be watched,\nyou have to specify the Role to add/remove, and the emoji to watch.", Utils.Red, ctx, true);
  }

  [Command("EmojiForRole")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  public async Task EmojiForRoleCommand(CommandContext ctx, [Description("The role to add")] DiscordRole role) {
    Utils.LogUserCommand(ctx);
    try {
      if (ctx.Message.ReferencedMessage == null) {
        await Utils.BuildEmbedAndExecute("EmojiForRole - Bad parameters", "You have to reply to the message that should be watched,\n and you have to specify the emoji to watch.", Utils.Red, ctx, true);
      }
      else {
        string msg = ctx.Message.ReferencedMessage.Content;
        if (msg.Length > 20) msg = msg.Substring(0, 20) + "...";
        if (emojisForRole.AddRemove(ctx.Message.ReferencedMessage, role, null)) {
          msg = "The referenced message (_" + msg + "_) will grant/remove the role *" + role.Name + "* when adding/removing any emoji";
        }
        else {
          msg = "The message referenced (_" + msg + "_) will not grant anymore the role *" + role.Name + "* when adding an emoji";
        }
        Utils.Log(msg);
        await ctx.RespondAsync(msg);
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("EmojiForRole", ex));
    }
  }

  [Command("EmojiForRole")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  public async Task EmojiForRoleCommand(CommandContext ctx, [Description("The emoji to watch")] DiscordEmoji emoji) {
    Utils.LogUserCommand(ctx);
    if (ctx.Message.ReferencedMessage == null) {
      await Utils.BuildEmbedAndExecute("EmojiForRole - Bad parameters", "You have to reply to the message that should be watched,\nand y ou have to specify the Role to add/remove.", Utils.Red, ctx, true);
    }
    else {
      await Utils.BuildEmbedAndExecute("EmojiForRole - Bad parameters", "You have to specify the Role to add/remove.", Utils.Red, ctx, true);
    }
  }




  internal static Task ReacionAdded(DiscordClient sender, MessageReactionAddEventArgs a) {
    try {
      ulong emojiId = a.Emoji.Id;
      string emojiName = a.Emoji.Name;
      emojisForRole.HandleAddingEmojiForRole(a.Message.ChannelId, emojiId, emojiName, a.User, a.Message.Id);

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
      emojisForRole.HandleRemovingEmojiForRole(a.Message.ChannelId, emojiId, emojiName, a.User, a.Message.Id);

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
    bool save = false;
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
      if (tracking == null) {
        tracking = new ReputationTracking(Utils.ConstructPath("Tracking", "Tracking", ".Tracking"));
        if (tracking == null) return Task.Delay(10);
      }
      save = tracking.AlterFun(authorId, added);
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
      if (tracking == null) {
        tracking = new ReputationTracking(Utils.ConstructPath("Tracking", "Tracking", ".Tracking"));
        if (tracking == null) return Task.Delay(10);
      }
      save = tracking.AlterRep(authorId, added);
    }

    // Start a delayed saving if one is not yet present
    if (save && !savingRequested) {
      savingRequested = true;
      _ = SaveDelayedAsync();
    }

    return Task.Delay(10);
  }



  static async Task SaveDelayedAsync() {
    await Task.Delay(30000);
    try {
      tracking.Save();
    } catch (Exception e) {
      Utils.Log("ERROR: problems in saving the Reputation file: " + e.Message);
    }
    savingRequested = false;
  }


}
