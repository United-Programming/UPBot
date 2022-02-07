using System;

public class Reputation : Entity {
  [Key] public ulong Guild;
  [Key] public ulong User;
  public int Rep;
  public int Fun;
  public int Tnk;
  public int Ran;
  public int Men;
  public DateTime LastUpdate;

  public Reputation() { }
  public Reputation(ulong gid, ulong usr) {
    Guild = gid;
    User = usr;
    Rep = 0;
    Fun = 0;
    Tnk = 0;
    Ran = 0;
    Men = 0;
    LastUpdate = DateTime.Now;
  }
}


