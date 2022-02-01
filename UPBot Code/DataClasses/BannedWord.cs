public class BannedWord : Entity {
  [Key] public long WordKey;
  [KeyGen] public ulong Guild;
  [KeyGen] public string Word;

  public BannedWord() { }

  public BannedWord(ulong guild, string w) {
    Guild = guild;
    Word = w;
    WordKey = GetKeyValue(guild, w);
  }
}

