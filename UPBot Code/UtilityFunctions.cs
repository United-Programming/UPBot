using DSharpPlus.Entities;

public static class UtilityFunctions
{
    public static string PluralFormatter(int count, string singular, string plural)
    {
        return count > 1 ? plural : singular;
    }

  public static DiscordEmoji GetEmoji(EmojiEnum emoji) {
    return null;
  }
}

public enum EmojiEnum {
  Cpp,
  CSharp,
  Java,
  Javascript,
  Python,
  UnitedProgramming,
  Unity,
  Godot,
  AutoMeged,
}