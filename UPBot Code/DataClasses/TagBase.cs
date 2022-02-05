public class TagBase : Entity {
  [Key] public ulong Guild;
  [Key] public string Topic;
  public string Information;

  public TagBase() { }

  public TagBase(ulong guild, string topic, string info) {
    Guild = guild;
    Topic = topic;
    Information = info;
  }
}
