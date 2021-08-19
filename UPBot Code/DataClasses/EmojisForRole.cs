using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;

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
