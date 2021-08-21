using DSharpPlus.Entities;

public class EmojiForRoleValue : Entity {
  public ulong Channel;
  [Key]
  public ulong Message;
  public ulong Role;
  public ulong EmojiId;
  public string EmojiName;

  [NotPersistent]
  public DiscordRole dRole; // Not in DB

  internal byte GetId() {
    byte res = 0;

    ulong v = Channel;
    for (int i = 0; i < 8; i++) {
      res ^= (byte)(v & 0xff);
      v >>= 8;
    }
    v = Message;
    for (int i = 0; i < 8; i++) {
      res ^= (byte)(v & 0xff);
      v >>= 8;
    }
    v = Role;
    for (int i = 0; i < 8; i++) {
      res ^= (byte)(v & 0xff);
      v >>= 8;
    }
    v = EmojiId;
    for (int i = 0; i < 8; i++) {
      res ^= (byte)(v & 0xff);
      v >>= 8;
    }
    return res;
  }
}
