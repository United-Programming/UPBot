using System;


public class BannedWord : Entity {
  [Key]
  public string Word;
  public ulong Creator;
  public DateTime DateAdded;

  public BannedWord() { }

  public BannedWord(string w, ulong id) {
    Word = w;
    Creator = id;
    DateAdded = DateTime.Now;
  }

  public override string ToString() {
    return Word + "\t" + Creator + "\t" + DateAdded + "\n";
  }
}

