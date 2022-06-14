using System;

public class TagBase : Entity {
  [Key] public ulong Guild;
  [Key] public string Topic;
  public string Alias1;
  public string Alias2;
  public string Alias3;
  [Comment] public string Information;
  [Comment] public string Author;
  [Comment] public long ColorOfTheme;
  [Comment] public DateTime timeOfCreation;

  public TagBase() { }

  public TagBase(ulong guild, string topic, string info, string author, int colorOfTheme, DateTime time) {
    Guild = guild;
    Topic = topic;
    Alias1 = null;
    Alias2 = null;
    Alias3 = null;
    Information = info;
    Author = author;
    ColorOfTheme = colorOfTheme;
    timeOfCreation = time;
  }
}
