using System;
using System.ComponentModel.DataAnnotations;


public class BannedWord {
  [Key]
  [Required]
  public string Word { get; set; }
  [Required]
  public ulong Creator { get; set; }
  [Required]
  public DateTime DateAdded { get; set; }

  public BannedWord(string w, ulong id) {
    Word = w;
    Creator = id;
    DateAdded = DateTime.Now;
  }
  public BannedWord() {
    Word = "";
    Creator = 0;
    DateAdded = DateTime.Now;
  }

  public BannedWord(string w, ulong id, DateTime d) {
    Word = w;
    Creator = id;
    DateAdded = d;
  }

  public override string ToString() {
    return Word + "\t" + Creator + "\t" + DateAdded + "\n";
  }
}

