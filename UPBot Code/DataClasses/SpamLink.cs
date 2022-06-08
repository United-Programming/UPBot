
public class SpamLink : Entity {
  [Key] public ulong Guild;
  [Key] public string link;


  public SpamLink() { }

  public SpamLink(ulong guild, string l) {
    Guild = guild;
    link = l;
  }
}
