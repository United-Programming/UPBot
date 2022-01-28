using System;


public class BannedWord : Entity {
  [Key]
  public long WordKey;
  public ulong Guild;
  public string Word;

  public BannedWord() { }

  public BannedWord(ulong guild, string w) {
    Guild = guild;
    Word = w;
    WordKey = TheKey();
  }

  public override string ToString() {
    return Word + "\t" + Guild + "\n";
  }

  public long TheKey() {
    return (long)Guild ^ Word.GetHashCode();
  }

  public static long GetTheKey(ulong gid, string w) {
    return (long)gid ^ w.GetHashCode();
  }

}

