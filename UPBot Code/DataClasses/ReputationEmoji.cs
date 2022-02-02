using System;
using DSharpPlus.Entities;

public class ReputationEmoji : Entity {
  [Key] public ulong Guild;
  [Key] public ulong Lid;
  [Key] public string Sid;
  public int For = 0;

  public ReputationEmoji() { }

  public ReputationEmoji(ulong gid, ulong lid, string sid, WhatToTrack f) {
    Guild = gid;
    Lid = lid;
    Sid = sid;
    For = (int)f;
  }

  internal string GetEmoji(DiscordGuild guild) {
    try {
      if (Lid == 0) return Sid;
      else return Utils.GetEmojiSnowflakeID(guild.GetEmojiAsync(Lid).Result);
    } catch (Exception ex) {
      Utils.Log("Error in getting an emoji: " + ex, guild.Name);
      return "";
    }
  }

  internal ulong GetKeyValue() {
    if (Sid == null) return Guild ^ Lid;
    else return Guild ^ (ulong)Sid.GetHashCode();
  }
  internal static ulong GetKeyValue(ulong gid, ulong lid, string sid) {
    if (sid == null) return gid ^ lid;
    else return gid ^ (ulong)sid.GetHashCode();
  }

  internal bool HasFlag(WhatToTrack uf) {
    return ((WhatToTrack)For).HasFlag(uf);
  }
}


