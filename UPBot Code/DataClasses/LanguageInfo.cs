using DSharpPlus.Entities;

public class LanguageInfo {
  public LanguageInfo(string videoLink, string courseLink, string colorHex) {
    this.VideoLink = videoLink;
    this.CourseLink = courseLink;
    this.Color = new DiscordColor(colorHex);
  }

  internal readonly string VideoLink;
  internal readonly string CourseLink;
  internal readonly DiscordColor Color;
}