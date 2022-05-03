using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EmojisForRole : BaseCommandModule {
  internal static Task ReacionAdded(DiscordClient _, MessageReactionAddEventArgs a) {
    if (a.Guild == null) Task.FromResult(0); // No direct messages
    try {
      if (Configs.TempRoleSelected.ContainsKey(a.Guild.Id) && Configs.TempRoleSelected[a.Guild.Id] != null && Configs.TempRoleSelected[a.Guild.Id].user == a.User.Id) {
        TempSetRole tsr = Configs.TempRoleSelected[a.Guild.Id];
        tsr.channel = a.Channel.Id;
        tsr.message = a.Message.Id;
        tsr.emojiid = a.Emoji.Id;
        tsr.emojiname = a.Emoji.Name;
        tsr.cancel.Cancel();
        return Task.FromResult(0);
      }

      // Find if we have guild/channel/message/emoji, in case add the role to the user
      List<EmojiForRoleValue> em4rol = Configs.Em4Roles[a.Guild.Id];
      foreach (var e in em4rol) {
        if (e.Guild == a.Guild.Id && e.Channel == a.Channel.Id && e.Message == a.Message.Id && (e.EmojiId == a.Emoji.Id || e.EmojiName == a.Emoji.Name))
          HandleRoleForEmoji(e, a.User, true);
      }
    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.ReactionAdded: " + ex.Message, a.Guild.Name);
    }
    return Task.FromResult(0);
  }

  internal static Task ReactionRemoved(DiscordClient _, MessageReactionRemoveEventArgs a) {
    if (a.Guild == null) Task.FromResult(0); // No direct messages
    try {
      // Find if we have guild/channel/message/emoji, in case add the role to the user
      List<EmojiForRoleValue> em4rol = Configs.Em4Roles[a.Guild.Id];
      foreach (var e in em4rol) {
        if (e.Guild == a.Guild.Id && e.Channel == a.Channel.Id && e.Message == a.Message.Id && (e.EmojiId == a.Emoji.Id || e.EmojiName == a.Emoji.Name))
          HandleRoleForEmoji(e, a.User, false);
      }
    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.ReactionRemoved: " + ex.Message, a.Guild.Name);
    }
    return Task.FromResult(0);
  }

  static void HandleRoleForEmoji(EmojiForRoleValue em, DiscordUser usr, bool add) {
    DiscordGuild guild = Configs.TryGetGuild(em.Guild);
    try {
      bool here = false;
      DiscordMember dm = guild.GetMemberAsync(usr.Id).Result;
      DiscordRole r = null;
      foreach (DiscordRole ur in dm.Roles)
        if (ur.Id == em.Role) {
          here = true;
          r = ur;
          break;
        }

      if (add && !here) {
        r = guild.GetRole(em.Role);
        if (r == null) {
          Utils.Log("The role with ID " + em.Role + " does not exist anymore!", guild.Name);
          return;
        }
        dm.GrantRoleAsync(r).Wait();
      }
      else if (here && !add) {
        dm.RevokeRoleAsync(r).Wait();
      }
    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.HandleRoleForEmoji: " + ex.Message, guild.Name);
    }
  }

  [Command("emojiforroles")]
  [Aliases("emojiforrole", "emojisforroles", "emforrole", "emoji4roles", "emoji4role", "emojis4roles", "em4role")]
  [Description("List the possible emoji for role and a link to the actual message")]
  public async Task Em4RoleCommand(CommandContext ctx) {
    if (ctx.Guild == null) return;
    try {
      if (!Configs.Permitted(ctx.Guild, Config.ParamType.Emoji4RoleList, ctx.Member) || !Configs.Permitted(ctx.Guild, Config.ParamType.Emoji4Role, ctx.Member)) return;
      ulong gid = ctx.Guild.Id;
      DiscordGuild g = ctx.Guild;

      if (Configs.Em4Roles[gid].Count == 0) {
        await Utils.DeleteDelayed(15, ctx.RespondAsync("No emoji for roles are defined"));
        return;
      }
      DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
        Title = "Emojis for Roles"
      };

      string ems = "";
      foreach (var em4r in Configs.Em4Roles[gid]) {
        DiscordRole r = g.GetRole(em4r.Role);
        DiscordChannel ch = g.GetChannel(em4r.Channel);
        DiscordMessage m = null;
        try {
          m = ch?.GetMessageAsync(em4r.Message).Result; // This may fail
        } catch (Exception) { }
        if (r == null || ch == null || m == null) continue;

        ems += Utils.GetEmojiSnowflakeID(em4r.EmojiId, em4r.EmojiName, g) + " grants  **_@" + r.Name + "_**  here: [" +
          (m.Content.Length > 16 ? m.Content[0..16] + "..." : m.Content) + "](https://discord.com/channels/" +
          gid + "/" + ch.Id + "/" + m.Id + ")\n";
      }
      eb.WithDescription(ems);
      await Utils.DeleteDelayed(15, ctx.RespondAsync(eb.Build()));

    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.Em4RoleCommand: " + ex.Message, ctx.Guild.Name);
    }
  }

}
