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
}
