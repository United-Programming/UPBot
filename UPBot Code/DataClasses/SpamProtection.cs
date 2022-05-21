using DSharpPlus.Entities;

public class SpamProtection : Entity {
  [Key] public ulong Guild;
  public bool protectDiscord;
  public bool protectSteam;
  public bool protectEpic;

  public SpamProtection() { }
  public SpamProtection(ulong gid) {
    Guild = gid;
  }
}
