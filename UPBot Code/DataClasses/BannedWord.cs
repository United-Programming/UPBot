using System;

public class BannedWord {
  public string word;
  public ulong creator = 0;
  public DateTime date = DateTime.MinValue;

  public BannedWord(string w, ulong id) {
    word = w;
    creator = id;
    date = DateTime.Now;
  }

  public BannedWord(string line) {
    string[] parts = line.Trim(' ', '\r', '\n').Split('\t');
    if (parts.Length != 3) return;
    word = parts[0].Trim(' ', '\r', '\n', '\t').ToLowerInvariant();
    ulong.TryParse(parts[1].Trim(' ', '\r', '\n', '\t'), out creator);
    DateTime.TryParse(parts[2].Trim(' ', '\r', '\n', '\t'), out date);
  }

  public override string ToString() {
    return word + "\t" + creator + "\t" + date + "\n";
  }
}