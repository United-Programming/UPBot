public class TagBase : Entity {
  [Key] public ulong Guild;
  [Key] public string Topic;
  public string Alias1;
  public string Alias2;
  public string Alias3;
  [Comment]public string Information;

  public TagBase() { }

  public TagBase(ulong guild, string topic, string info) {
    Guild = guild;
    Topic = topic;
    Alias1 = null;
    Alias2 = null;
    Alias3 = null;
    Information = info;
  }
}
