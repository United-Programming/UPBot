using System;
using DSharpPlus.Entities;

public class ReputationEmoji : Entity {
  [Key] public long EmKey;
  [KeyGen] public ulong Guild;
  [KeyGen] public ulong Lid;
  [KeyGen] public string Sid;
  public int For = 0;

  public ReputationEmoji() { }

  public ReputationEmoji(ulong gid, ulong lid, string sid, WhatToTrack f) {
    Guild = gid;
    Lid = lid;
    Sid = sid;
    For = (int)f;
    EmKey = GetKeyValue(gid, lid, sid);
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


