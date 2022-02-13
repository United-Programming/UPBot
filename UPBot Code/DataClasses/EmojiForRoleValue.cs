using DSharpPlus.Entities;

public class EmojiForRoleValue : Entity {
  [Key] public ulong Guild;
  [Key] public ulong Channel;
  [Key] public ulong Message;
  [Key] public ulong Role;
  public ulong EmojiId;
  public string EmojiName;
}
