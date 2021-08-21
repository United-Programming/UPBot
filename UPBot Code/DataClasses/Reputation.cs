using System;

public class Reputation : Entity {
  [Key]
  public ulong User;
  public int Rep;
  public int Fun;
  public int Tnk;
  public DateTime DateAdded;
}


