
public class SpamLink : Entity {
  [Key] public ulong Guild;
  [Key] public string link;
  public bool whitelist;


  public SpamLink() { }

  public SpamLink(ulong guild, string l, bool wl) {
    Guild = guild;
    link = l;
    whitelist = wl;
  }
}
