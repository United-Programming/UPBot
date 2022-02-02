public class BannedWord : Entity {
  [Key] public ulong Guild;
  [Key] public string Word;

  public BannedWord() { }

  public BannedWord(ulong guild, string w) {
    Guild = guild;
    Word = w;
  }
}

