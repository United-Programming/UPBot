using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EmojisForRole : BaseCommandModule {
  internal static Task ReacionAdded(DiscordClient _, MessageReactionAddEventArgs a) {
    if (a.Guild == null) Task.FromResult(0); // No direct messages
    try {
      if (Setup.TempRoleSelected.ContainsKey(a.Guild.Id) && Setup.TempRoleSelected[a.Guild.Id] != null && Setup.TempRoleSelected[a.Guild.Id].user == a.User.Id) {
        TempSetRole tsr = Setup.TempRoleSelected[a.Guild.Id];
        tsr.channel = a.Channel.Id;
        tsr.message = a.Message.Id;
        tsr.emojiid = a.Emoji.Id;
        tsr.emojiname = a.Emoji.Name;
        tsr.cancel.Cancel();
        return Task.FromResult(0);
      }

      // Find if we have guild/channel/message/emoji, in case add the role to the user
      List<EmojiForRoleValue> em4rol = Setup.Em4Roles[a.Guild.Id];
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
      List<EmojiForRoleValue> em4rol = Setup.Em4Roles[a.Guild.Id];
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
    DiscordGuild guild = Setup.TryGetGuild(em.Guild);
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


}
