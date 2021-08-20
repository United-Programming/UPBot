using DSharpPlus.Entities;
using System.ComponentModel.DataAnnotations;

public class EmojiForRoleValue {
  [Required]
  public ulong Channel { get; set; }
  [Key]
  [Required]
  public ulong Message { get; set; }
  [Required]
  public ulong Role { get; set; }
  [Required]
  public ulong EmojiId { get; set; }
  [Required]
  public string EmojiName { get; set; }

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
