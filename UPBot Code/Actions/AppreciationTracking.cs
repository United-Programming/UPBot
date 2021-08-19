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


  public class Reputation { // 16 bytes
    public ulong user;              // 8
    public ushort reputation;       // 2
    public ushort fun;              // 2
    public DateTime startTracking;  // 4
  }

  public class ReputationTracking {
    readonly DateTime trackingStarted;
    readonly Dictionary<ulong, Reputation> dic;
    readonly string path = null;

    public ReputationTracking(string path) {
      try {
        this.path = path;
        dic = new Dictionary<ulong, Reputation>();
        if (!File.Exists(path)) {
          trackingStarted = DateTime.Now;
          return;
        }
        byte[] data = new byte[20];
        using (FileStream f = new FileStream(path, FileMode.Open)) {
          // 32 bits for the date (ymd)
          if (f.Read(data, 0, 4) < 4) {
            Utils.Log("ERROR: wrong Reputation file: " + path);
            try {
              if (File.Exists(path)) File.Delete(path);
            } catch (Exception e) {
              Utils.Log("ERROR: cannot delete old Reputation file: " + path + "\nException: " + e.Message);
            }
            return;
          }
          trackingStarted = GetDateFromBytes(data, 0);
          while (f.Read(data, 0, 16) == 16) {
            ulong usrid = BitConverter.ToUInt64(data);
            ushort rep = BitConverter.ToUInt16(data, 8);
            ushort fun = BitConverter.ToUInt16(data, 10);
            DateTime start = GetDateFromBytes(data, 12);
            dic[usrid] = new Reputation { user = usrid, reputation = rep, fun = fun, startTracking = start };
          }
        }
        Utils.Log("ReputationTracking: Loaded " + dic.Count + " users");
      } catch (Exception e) {
        Utils.Log("ERROR: problems in loading the Reputation file: " + e.Message);
      }
    }

    public void Save() {
      try {
        lock (dic) {
          try {
            if (File.Exists(path)) File.Delete(path);
          } catch (Exception e) {
            Utils.Log("ERROR: cannot delete old Reputation file: " + path + "\nException: " + e.Message);
            return;
          }
          using (FileStream f = new FileStream(path, FileMode.CreateNew)) {
            byte[] data = new byte[16];
            SetDateToBytes(trackingStarted, data, 0);
            f.Write(data, 0, 4);
            foreach (Reputation r in dic.Values) {
              byte[] d = BitConverter.GetBytes(r.user);
              int pos = 0;
              for (int i = 0; i < d.Length; i++)
                data[pos++] = d[i];
              d = BitConverter.GetBytes(r.reputation);
              for (int i = 0; i < d.Length; i++)
                data[pos++] = d[i];
              d = BitConverter.GetBytes(r.fun);
              for (int i = 0; i < d.Length; i++)
                data[pos++] = d[i];
              SetDateToBytes(r.startTracking, data, pos);
              f.Write(data, 0, 16);
            }
            f.Flush();
          }
        }
        Utils.Log("ReputationTracking: Saved " + dic.Count + " users");
      } catch (Exception e) {
        Utils.Log("ERROR: problems in saving the Reputation file: " + e.Message);
      }
    }

    private void SetDateToBytes(DateTime d, byte[] data, int offset) {
      data[offset + 0] = (byte)((d.Year & 0xff00) >> 8);
      data[offset + 1] = (byte)(d.Year & 0xff);
      data[offset + 2] = (byte)(d.Month & 0xff);
      data[offset + 3] = (byte)(d.Day & 0xff);
    }

    private DateTime GetDateFromBytes(byte[] data, int offset) {
      try {
        return new DateTime((data[offset + 0] << 8) + data[offset + 1], data[offset + 2], data[offset + 3]);
      } catch (Exception) {
        return DateTime.Now;
      }
    }

    public bool AlterRep(ulong id, bool add) {
      if (add) {
        if (dic.ContainsKey(id)) dic[id].reputation++;
        else {
          dic.Add(id, new Reputation { user = id, reputation = 1, fun = 0, startTracking = DateTime.Now });
        }
        return true;
      }
      else {
        if (dic.ContainsKey(id) && dic[id].reputation > 0) {
          dic[id].reputation--;
          return true;
        }
      }
      return false;
    }
    public bool AlterFun(ulong id, bool add) {
      if (add) {
        if (dic.ContainsKey(id)) dic[id].fun++;
        else {
          dic.Add(id, new Reputation { user = id, reputation = 0, fun = 1, startTracking = DateTime.Now });
        }
        return true;
      }
      else {
        if (dic.ContainsKey(id) && dic[id].fun > 0) {
          dic[id].fun--;
          return true;
        }
      }
      return false;
    }

    internal string GetStartDate() {
      return trackingStarted.ToString("yyyy/MM/dd");
    }

    internal IEnumerable<Reputation> GetReputation() {
      return dic.Values;
    }
  }

  public class EmojisForRole {
    List<ReactionValue> values = null;
    string path = null;

    public EmojisForRole(string path) {
      try {
        values = new List<ReactionValue>();
        this.path = path;
        if (!File.Exists(path)) return;

        DiscordGuild g = Utils.GetGuild();
        byte[] data = new byte[36];
        using (FileStream f = new FileStream(path, FileMode.Open)) {
          while (f.Read(data, 0, 36) == 36) {
            ReactionValue v = new ReactionValue {
              channel = BitConverter.ToUInt64(data, 0),
              message = BitConverter.ToUInt64(data, 8),
              role = BitConverter.ToUInt64(data, 16),
              emojiId = BitConverter.ToUInt64(data, 24),
              emojiName = Char.ConvertFromUtf32(BitConverter.ToInt32(data, 32)),
              dRole = null
            };

            DiscordMessage m;
            try {
              // Does the message exists?
              DiscordChannel c = g.GetChannel(v.channel);
              if (c == null || c.Id == 0) continue; // Bad
              m = c.GetMessageAsync(v.message).Result;
              if (m == null || m.Id == 0) continue; // Bad;
                                                    // Does the role exists?
              DiscordRole r = g.GetRole(v.role);
              if (r == null || r.Id == 0) continue; // Bad
            } catch (Exception ex) {
              Utils.Log("Error while checking a ReactionValue: " + ex.Message);
              continue;
            }
            // Check that the message has the required emojis, if not add it
            DiscordEmoji e = null;
            if (v.emojiId != 0) {
              e = g.GetEmojiAsync(v.emojiId).Result;
            }
            else if (v.emojiName != "!") {
              e = DiscordEmoji.FromUnicode(v.emojiName);
            }
            if (e == null) continue; // Bad
            if (m.GetReactionsAsync(e, 1).Result.Count == 0) { // Need to add
              m.CreateReactionAsync(e).Wait();
            }

            values.Add(v);
          }
        }
        Utils.Log("Loaded " + values.Count + " tracked messages for emojis.");
      } catch (Exception e) {
        Utils.Log("ERROR: problems in loading the EmojiForRole file: " + e.Message);
      }
    }

    public void Save() {
      try {
        lock (values) {
          try {
            if (File.Exists(path)) File.Delete(path);
          } catch (Exception e) {
            Utils.Log("ERROR: cannot delete old EmojisForRole file: " + path + "\nException: " + e.Message);
            return;
          }
          using (FileStream f = new FileStream(path, FileMode.CreateNew)) {
            byte[] data = new byte[36];
            foreach (ReactionValue v in values) {
              int pos = 0;
              byte[] d = BitConverter.GetBytes(v.channel);
              for (int i = 0; i < d.Length; i++) data[pos++] = d[i];
              d = BitConverter.GetBytes(v.message);
              for (int i = 0; i < d.Length; i++) data[pos++] = d[i];
              d = BitConverter.GetBytes(v.role);
              for (int i = 0; i < d.Length; i++) data[pos++] = d[i];
              d = BitConverter.GetBytes(v.emojiId);
              for (int i = 0; i < d.Length; i++) data[pos++] = d[i];
              if (v.emojiId != 0 || v.emojiName == null || v.emojiName.Length == 0)
                for (int i = 0; i < 4; i++) data[pos++] = 0;
              else {
                d = BitConverter.GetBytes(Char.ConvertToUtf32(v.emojiName, 0));
                for (int i = 0; i < 4; i++) data[pos++] = d[i];
              }
              f.Write(data, 0, pos);
            }
            f.Flush();
          }
        }
        Utils.Log("EmojisForRole: Saved " + values.Count + " tracked messages and emojis");
      } catch (Exception e) {
        Utils.Log("ERROR: problems in saving the EmojiForRole file: " + e.Message);
      }
    }

    internal bool AddRemove(DiscordMessage msg, DiscordRole role, DiscordEmoji emoji) {
      bool here = false;
      ReactionValue toRemove = null;
      foreach (ReactionValue v in values) {
        if (v.channel == msg.ChannelId && v.message == msg.Id && v.role == role.Id) {
          if ((emoji == null && v.emojiId == 0 && v.emojiName == "!") ||
              (emoji != null && ((emoji.Id != 0 && emoji.Id == v.emojiId) || (emoji.Id == 0 && emoji.Name.Equals(v.emojiName))))) {
            toRemove = v; // Remove
            break;
          }
        }
      }
      if (toRemove != null) {
        values.Remove(toRemove);
      }
      else {
        here = true;
        ReactionValue v = new ReactionValue {
          channel = msg.ChannelId,
          message = msg.Id,
          role = role.Id,
          emojiId = emoji == null ? 0 : emoji.Id,
          emojiName = emoji == null ? "!" : emoji.Name,
          dRole = role
        };
        values.Add(v);

        if (emoji != null) {
          // Check if we have the emoji already, if not add it
          if (msg.GetReactionsAsync(emoji, 1).Result.Count == 0) {
            msg.CreateReactionAsync(emoji).Wait();
          }
        }

      }
      Save();
      return here;
    }

    internal void HandleAddingEmojiForRole(ulong cId, ulong eId, string eN, DiscordUser user, ulong msgId) {
      try {
        DiscordMember dm = (DiscordMember)user;
        if (dm == null) return; // Not a valid member for the Guild/Context
        if (values == null) return;
        foreach (ReactionValue v in values) {
          if (cId == v.channel && msgId == v.message && (v.emojiId == 0 && v.emojiName == "!") || (eId != 0 && v.emojiId == eId) || (eId == 0 && eN.Equals(v.emojiName))) {
            if (v.dRole == null) v.dRole = dm.Guild.GetRole(v.role);
            dm.GrantRoleAsync(v.dRole).Wait();
            Utils.Log("Role " + v.dRole.Name + " was granted to " + user.Username + " by emoji " + eId);
            return;
          }
        }
      } catch (Exception e) {
        Utils.Log("ERROR: problems in HandleAddingEmojiForRole: " + e.Message);
      }
    }

    internal void HandleRemovingEmojiForRole(ulong cId, ulong eId, string eN, DiscordUser user, ulong msgId) {
      try {
        DiscordMember dm = (DiscordMember)user;
        if (dm == null) return; // Not a valid member for the Guild/Context
        if (values == null) return;
        foreach (ReactionValue v in values) {
          if (cId == v.channel && msgId == v.message && (v.emojiId == 0 && v.emojiName == "!") || (eId != 0 && v.emojiId == eId) || (eId == 0 && eN.Equals(v.emojiName))) {
            if (v.dRole == null) v.dRole = dm.Guild.GetRole(v.role);
            dm.RevokeRoleAsync(v.dRole).Wait();
            Utils.Log("Role " + v.dRole.Name + " was removed from " + user.Username + " by emoji " + eId);
            return;
          }
        }
      } catch (Exception e) {
        Utils.Log("ERROR: problems in HandleRemovingEmojiForRole: " + e.Message);
      }
    }

    public class ReactionValue {
      public ulong channel;       // 8 bytes
      public ulong message;       // 8 bytes
      public ulong role;          // 8 bytes
      public ulong emojiId;       // 8 bytes
      public string emojiName;    // 4 bytes (utf32, 1 character)
      public DiscordRole dRole;
    }
  }
}
