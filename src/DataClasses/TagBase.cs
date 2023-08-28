using System;

public class TagBase : Entity {
  [Key] public ulong Guild;
  [Key] public string Topic;
  public string Alias1;
  public string Alias2;
  public string Alias3;
  [Comment] public string Information;
  public string Author;
  public long ColorOfTheme;
  public DateTime timeOfCreation;
  public string thumbnailLink;
  public string AuthorIcon;
  public string imageLink;

  public TagBase() { }

  public TagBase(ulong guild, string topic, string info, string author, string authorIcon, int colorOfTheme, DateTime time, string thumbnailLink, string imageLink) {
    Guild = guild;
    Topic = topic;
    Alias1 = null;
    Alias2 = null;
    Alias3 = null;
    Information = info;
    Author = author;
    AuthorIcon = authorIcon;
    ColorOfTheme = colorOfTheme;
    timeOfCreation = time;
    this.thumbnailLink = thumbnailLink;
    this.imageLink = imageLink;
  }
}
