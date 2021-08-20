using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EmojisForRole : BaseCommandModule {
  static List<EmojiForRoleValue> values = null;

  static void GetValues() {
    if (values != null) return;
    values = new List<EmojiForRoleValue>();
    if (Utils.db.EmojiForRoles != null) {
      int num = 0;
      foreach (EmojiForRoleValue v in Utils.db.EmojiForRoles) {
        num++;
        values.Add(v);
      }
      Utils.Log("Found " + num + " EmojiForRoles entries");
    }
  }

  [Command("WhatRole")]
  [Aliases("WhatRoles", "WhatRoleCanIGet", "WhatRolesCanIGet")]
  [Description("List all roles that can be got via an emoji, and a link to the message")]
  public async Task WhatRoleCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      GetValues();
      if (values.Count == 0) {
        DiscordMessage msg = await Utils.BuildEmbedAndExecute("EmojiForRoles List", "No messages are available to provide a role with an emoji.", Utils.LightBlue, ctx, true);
        await Utils.DeleteDelayed(30, msg);
        await Task.FromResult(0);
      }
      else {
        DiscordEmbedBuilder e = new DiscordEmbedBuilder();
        e.Title = values.Count + (values.Count != 1 ? " messages are" : " message is") + " known to give a Role by Emoji\n";
        DiscordGuild guild = Utils.GetGuild();
        string msg = "";
        foreach (EmojiForRoleValue val in values) {
          if (val.dRole == null) val.dRole = guild.GetRole(val.Role);
          msg += "- " + val.dRole.Mention + " from the message: [Link to message](https://discord.com/channels/" + guild.Id + "/" + val.Channel + "/" + val.Message + ")\n";
        }
        e.Description = msg;
        e.Color = Utils.LightBlue;
        DiscordMessage answer = await ctx.RespondAsync(e.Build());
        await Utils.DeleteDelayed(30, answer);
        await Task.FromResult(0);
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("WhatRole", ex));
    }
  }

  [Command("ListAndHandleEmojisForRoles")]
  [Description("List all roles that can be got via an emoji, with a link to the message, a button to check if the message is still valid, and and a button to remove it")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  public async Task ListAndHandleEmojisForRolesCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      GetValues();
      if (values.Count == 0) {
        DiscordMessage msg = await Utils.BuildEmbedAndExecute("EmojiForRoles List", "No messages are available to provide a role with an emoji.", Utils.Red, ctx, true);
        await Utils.DeleteDelayed(30, msg);
        await Task.FromResult(0);
      }
      else {
        DiscordEmbedBuilder e = new DiscordEmbedBuilder();
        e.Title = values.Count + (values.Count != 1 ? " messages are" : " message is") + " known to give a Role by Emoji\n";
        DiscordGuild guild = Utils.GetGuild();
        string msg = "";
        foreach (EmojiForRoleValue val in values) {
          if (val.dRole == null) val.dRole = guild.GetRole(val.Role);
          msg += "- [Jump to message](https://discord.com/channels/" + guild.Id + "/" + val.Channel + "/" + val.Message + ") ❌Remove command: `e4rremove " + val.GetId() + "`  Role " + val.dRole.Mention + "\n";
        }
        e.Description = msg;
        e.Color = Utils.Red;
        DiscordMessage answer = await ctx.RespondAsync(e.Build());
        await Utils.DeleteDelayed(30, answer);
        await Task.FromResult(0);
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("WhatRole", ex));
    }
  }

  [Command("e4rremove")]
  [Description("used to quickly remove an EmojiForRole command")]
  [RequireRoles(RoleCheckMode.Any, "Mod", "Owner", "Helper")] // Restrict access to users with the "Mod" or "Owner" role only
  public async Task E4rRemoveCommand(CommandContext ctx, int code) {
    Utils.LogUserCommand(ctx);
    try {
      GetValues();
      EmojiForRoleValue toRemove = null;
      foreach (EmojiForRoleValue val in values) {
        if (val.GetId() == code) {
          toRemove = val;
          break;
        }
      }
      if (toRemove == null) {
        DiscordMessage msg = await Utils.BuildEmbedAndExecute("EmojiForRoles Removal error", "No entry with code " + code + " found!", Utils.Red, ctx, true);
        await Utils.DeleteDelayed(30, msg);
        await Task.FromResult(0);
      }
      else {
        values.Remove(toRemove);
        Utils.db.EmojiForRoles.Remove(toRemove);
        Utils.db.SaveChanges();
        Utils.Log("Memeber " + ctx.Member.DisplayName + " removed EmojiForRoles with code " + code + " https://discord.com/channels/" + Utils.GetGuild().Id + "/" + toRemove.Channel + "/" + toRemove.Message);
        DiscordMessage answer = await Utils.BuildEmbedAndExecute("EmojiForRoles removal", "Entry with code " + code + " has been removed", Utils.Red, ctx, true);
        await Utils.DeleteDelayed(30, answer);
        await Task.FromResult(0);
      }
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("WhatRole", ex));
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
        if (AddRemove(ctx.Message.ReferencedMessage, role, emoji)) {
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
        if (AddRemove(ctx.Message.ReferencedMessage, role, null)) {
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
      HandleAddingEmojiForRole(a.Message.ChannelId, emojiId, emojiName, a.User, a.Message.Id);
    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.ReactionAdded: " + ex.Message);
    }
    return Task.FromResult(0);
  }

  internal static Task ReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs a) {
    try {
      ulong emojiId = a.Emoji.Id;
      string emojiName = a.Emoji.Name;
      HandleRemovingEmojiForRole(a.Message.ChannelId, emojiId, emojiName, a.User, a.Message.Id);
    } catch (Exception ex) {
      Utils.Log("Error in EmojisForRole.ReactionRemoved: " + ex.Message);
    }
    return Task.FromResult(0);
  }



  internal bool AddRemove(DiscordMessage msg, DiscordRole role, DiscordEmoji emoji) {
    GetValues();
    bool here = false;
    EmojiForRoleValue toRemove = null;
    foreach (EmojiForRoleValue v in values) {
      if (v.Channel == msg.ChannelId && v.Message == msg.Id && v.Role == role.Id) {
        if ((emoji == null && v.EmojiId == 0 && v.EmojiName == "!") ||
            (emoji != null && ((emoji.Id != 0 && emoji.Id == v.EmojiId) || (emoji.Id == 0 && emoji.Name.Equals(v.EmojiName))))) {
          toRemove = v; // Remove
          break;
        }
      }
    }
    if (toRemove != null) {
      values.Remove(toRemove);
      Utils.db.EmojiForRoles.Remove(toRemove);
      Utils.db.SaveChanges();
    }
    else {
      here = true;
      EmojiForRoleValue v = new EmojiForRoleValue {
        Channel = msg.ChannelId,
        Message = msg.Id,
        Role = role.Id,
        EmojiId = emoji == null ? 0 : emoji.Id,
        EmojiName = emoji == null ? "!" : emoji.Name,
        dRole = role
      };
      values.Add(v);
      Utils.db.EmojiForRoles.Add(v);
      Utils.db.SaveChanges();

      if (emoji != null) {
        // Check if we have the emoji already, if not add it
        if (msg.GetReactionsAsync(emoji, 1).Result.Count == 0) {
          msg.CreateReactionAsync(emoji).Wait();
        }
      }
    }
    return here;
  }

  internal static void HandleAddingEmojiForRole(ulong cId, ulong eId, string eN, DiscordUser user, ulong msgId) {
    try {
      DiscordMember dm = (DiscordMember)user;
      if (dm == null) return; // Not a valid member for the Guild/Context
      GetValues();
      foreach (EmojiForRoleValue v in values) {
        if (cId == v.Channel && msgId == v.Message && (v.EmojiId == 0 && v.EmojiName == "!") || (eId != 0 && v.EmojiId == eId) || (eId == 0 && eN.Equals(v.EmojiName))) {
          if (v.dRole == null) v.dRole = dm.Guild.GetRole(v.Role);
          dm.GrantRoleAsync(v.dRole).Wait();
          Utils.Log("Role " + v.dRole.Name + " was granted to " + user.Username + " by emoji " + eId);
          return;
        }
      }
    } catch (Exception e) {
      Utils.Log("ERROR: problems in HandleAddingEmojiForRole: " + e.Message);
    }
  }

  internal static void HandleRemovingEmojiForRole(ulong cId, ulong eId, string eN, DiscordUser user, ulong msgId) {
    try {
      DiscordMember dm = (DiscordMember)user;
      if (dm == null) return; // Not a valid member for the Guild/Context
      GetValues();
      foreach (EmojiForRoleValue v in values) {
        if (cId == v.Channel && msgId == v.Message && (v.EmojiId == 0 && v.EmojiName == "!") || (eId != 0 && v.EmojiId == eId) || (eId == 0 && eN.Equals(v.EmojiName))) {
          if (v.dRole == null) v.dRole = dm.Guild.GetRole(v.Role);
          dm.RevokeRoleAsync(v.dRole).Wait();
          Utils.Log("Role " + v.dRole.Name + " was removed from " + user.Username + " by emoji " + eId);
          return;
        }
      }
    } catch (Exception e) {
      Utils.Log("ERROR: problems in HandleRemovingEmojiForRole: " + e.Message);
    }
  }

}
