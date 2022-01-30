using DSharpPlus.Entities;
using System;

public class ReputationEmoji : Entity {
  [Key]
  public long EmKey;
  public ulong Guild;
  public ulong Lid;
  public string Sid;
  public int For = 0;

  public ReputationEmoji() { }

  public ReputationEmoji(ulong gid, ulong lid, string sid, WhatToTrack f) {
    Guild = gid;
    Lid = lid;
    Sid = sid;
    For = (int)f;
    EmKey = GetTheKey(gid, lid, sid);
  }

  public static long GetTheKey(ulong gid, ulong lid, string sid) {
    return (long)gid ^ (long)lid ^ (long)(sid == null ? 0 : sid.GetHashCode());
  }


  public long GetTheKey() {
    return (long)Guild ^ (long)Lid ^ (long)(Sid == null ? 0 : Sid.GetHashCode());
  }

  internal string GetEmoji(DiscordGuild guild) {
    try {
      if (Lid == 0) return Sid;
      else return Utils.GetEmojiSnowflakeID(guild.GetEmojiAsync(Lid).Result);
    } catch (Exception ex) {
      Utils.Log("Error in getting an emoji: " + ex);
      return "";
    }
  }


  internal bool HasFlag(WhatToTrack uf) {
    return ((WhatToTrack)For).HasFlag(uf);
  }
}


